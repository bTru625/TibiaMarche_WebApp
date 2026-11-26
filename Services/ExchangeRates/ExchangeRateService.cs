using TibiaMarche_WebApp.Models;

namespace TibiaMarche_WebApp.Services.ExchangeRates
{
    /// <summary>
    /// UML ExchangeRateService (web). Holds the current rates in memory so pages never wait on
    /// the database, and records the refresh status for the Admin hub.
    ///
    /// Rates arrive from ExchangeRateRefreshService (on startup from Realtime Database, then after
    /// every refresh). Each update is also pushed into CurrencyExtensions.SetRates, so existing code
    /// such as Buyer.ConvertPrice uses the real rates.
    ///
    /// Until the first rates arrive, IsReady is false and converting to or from any currency other
    /// than CAD throws, instead of silently using 1.0. Callers should show CAD in that case.
    ///
    /// Singleton; safe to call from any request thread.
    /// </summary>
    public class ExchangeRateService
    {
        private readonly object _lock = new();
        private ExchangeRateSnapshot? _current;
        private DateTimeOffset? _lastAttemptAt;
        private DateTimeOffset? _nextRefreshAt;
        private string? _lastError;

        /// <summary>True once real rates have been loaded.</summary>
        public bool IsReady
        {
            get { lock (_lock) { return _current != null; } }
        }

        /// <summary>When the current rates were fetched from Frankfurter, or null before the first load.</summary>
        public DateTimeOffset? GetLastUpdated()
        {
            lock (_lock) { return _current?.FetchedAtUtc; }
        }

        /// <summary>How many units of <paramref name="to"/> equal 1 unit of <paramref name="from"/>.</summary>
        public double GetRate(Currency from, Currency to)
        {
            if (from == to)
            {
                return 1.0;
            }
            if (!IsReady)
            {
                throw new InvalidOperationException("Exchange rates have not been loaded yet.");
            }
            // Stored rates are "1 CAD = x units", so convert through CAD.
            return to.GetRate() / from.GetRate();
        }

        /// <summary>Converts an amount between currencies (display and checkout).</summary>
        public double Convert(double amount, Currency from, Currency to) => amount * GetRate(from, to);

        /// <summary>Validates and installs new rates (called by the refresh service).</summary>
        public void Apply(ExchangeRateSnapshot snapshot)
        {
            snapshot.Validate();
            lock (_lock)
            {
                CurrencyExtensions.SetRates(snapshot.ToCurrencyMap());
                _current = snapshot;
            }
        }

        public void RecordSuccess(DateTimeOffset attemptedAt)
        {
            lock (_lock)
            {
                _lastAttemptAt = attemptedAt;
                _lastError = null;
            }
        }

        public void RecordFailure(DateTimeOffset attemptedAt, string error)
        {
            lock (_lock)
            {
                _lastAttemptAt = attemptedAt;
                _lastError = error;
            }
        }

        public void SetNextRefresh(DateTimeOffset? nextRefreshAt)
        {
            lock (_lock) { _nextRefreshAt = nextRefreshAt; }
        }

        /// <summary>Everything the Admin hub needs to show (S4 hub card).</summary>
        public ExchangeRateStatus GetStatus()
        {
            lock (_lock)
            {
                return new ExchangeRateStatus(
                    IsReady: _current != null,
                    RatesDate: _current?.Date,
                    LastUpdated: _current?.FetchedAtUtc,
                    LastAttempt: _lastAttemptAt,
                    NextRefresh: _nextRefreshAt,
                    LastError: _lastError);
            }
        }
    }

    /// <summary>Read-only refresh status for the Admin hub. Times are UTC.</summary>
    public record ExchangeRateStatus(
        bool IsReady,
        string? RatesDate,
        DateTimeOffset? LastUpdated,
        DateTimeOffset? LastAttempt,
        DateTimeOffset? NextRefresh,
        string? LastError);
}
