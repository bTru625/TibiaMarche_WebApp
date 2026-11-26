using Microsoft.Extensions.Options;
using TibiaMarche_WebApp.Services.Firebase;

namespace TibiaMarche_WebApp.Services.ExchangeRates
{
    /// <summary>
    /// Background job that keeps the shared exchange rates fresh (decided 2026-11-08: automatic,
    /// no manual refresh button).
    ///
    /// 1. At startup, read exchangeRates/latest from Realtime Database and load it into memory.
    ///    If nothing is stored, or it is older than RefreshInterval, refresh right away (catch-up).
    ///    Otherwise wait only for the time left.
    /// 2. Refresh = fetch from Frankfurter, save to Realtime Database, then load into memory.
    ///    Next refresh after RefreshInterval.
    /// 3. On failure, log it, keep the old rates and try again after RetryDelay.
    ///    Errors never escape, so the website keeps running.
    ///
    /// Rates only refresh while the web app is running. A scheduled Cloud Function can replace
    /// this later (needs the paid Blaze plan).
    /// </summary>
    public class ExchangeRateRefreshService : BackgroundService
    {
        private readonly FrankfurterClient _frankfurter;
        private readonly RealtimeDatabaseClient _database;
        private readonly ExchangeRateService _rates;
        private readonly ExchangeRateOptions _options;
        private readonly ILogger<ExchangeRateRefreshService> _logger;

        public ExchangeRateRefreshService(
            FrankfurterClient frankfurter,
            RealtimeDatabaseClient database,
            ExchangeRateService rates,
            IOptions<ExchangeRateOptions> options,
            ILogger<ExchangeRateRefreshService> logger)
        {
            _frankfurter = frankfurter;
            _database = database;
            _rates = rates;
            _options = options.Value;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_database.IsConfigured)
            {
                const string message =
                    "Exchange rate refresh is OFF: Realtime Database is not configured. Set " +
                    "Firebase:ServiceAccountKeyPath with dotnet user-secrets to an existing key file, then restart.";
                _logger.LogWarning(message);
                _rates.RecordFailure(DateTimeOffset.UtcNow, message);
                return;
            }

            TimeSpan delay = await LoadStoredRatesAsync(stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                if (delay > TimeSpan.Zero)
                {
                    _rates.SetNextRefresh(DateTimeOffset.UtcNow + delay);
                    _logger.LogInformation("Next exchange rate refresh in {Delay}.", delay);
                    try
                    {
                        await Task.Delay(delay, stoppingToken);
                    }
                    catch (OperationCanceledException)
                    {
                        return; // app is shutting down
                    }
                }

                delay = await RefreshAsync(stoppingToken);
            }
        }

        /// <summary>Loads the stored rates (if any) and returns how long to wait before the first refresh.</summary>
        private async Task<TimeSpan> LoadStoredRatesAsync(CancellationToken stoppingToken)
        {
            try
            {
                ExchangeRateSnapshot? stored =
                    await _database.GetAsync<ExchangeRateSnapshot>(_options.DatabasePath, stoppingToken);
                if (stored == null)
                {
                    _logger.LogInformation("No stored exchange rates at '{Path}'. Fetching now.", _options.DatabasePath);
                    return TimeSpan.Zero;
                }

                _rates.Apply(stored);
                TimeSpan age = DateTimeOffset.UtcNow - stored.FetchedAtUtc;
                _logger.LogInformation(
                    "Loaded stored exchange rates for {Date} (saved {Age} ago).", stored.Date, age);

                return age >= _options.RefreshInterval ? TimeSpan.Zero : _options.RefreshInterval - age;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                // Unreadable or invalid document: fetch fresh rates, which overwrites it.
                _logger.LogWarning(ex, "Could not load stored exchange rates. Fetching now.");
                return TimeSpan.Zero;
            }
        }

        /// <summary>Fetches, saves and applies new rates. Returns the delay until the next attempt.</summary>
        private async Task<TimeSpan> RefreshAsync(CancellationToken stoppingToken)
        {
            DateTimeOffset attemptedAt = DateTimeOffset.UtcNow;
            try
            {
                ExchangeRateSnapshot snapshot = await _frankfurter.FetchLatestAsync(stoppingToken);
                // Save first, so the web app and Android always show the same rates.
                await _database.PutAsync(_options.DatabasePath, snapshot, stoppingToken);
                _rates.Apply(snapshot);
                _rates.RecordSuccess(attemptedAt);

                _logger.LogInformation(
                    "Exchange rates refreshed for {Date}: {Rates}.",
                    snapshot.Date,
                    string.Join(", ", snapshot.Rates.Select(r => $"{r.Key} {r.Value}")));
                return _options.RefreshInterval;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exchange rate refresh failed. Keeping the previous rates.");
                _rates.RecordFailure(attemptedAt, ex.Message);
                return _options.RetryDelay;
            }
        }
    }
}
