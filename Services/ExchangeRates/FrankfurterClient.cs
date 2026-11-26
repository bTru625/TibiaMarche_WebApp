using System.Net.Http.Json;

namespace TibiaMarche_WebApp.Services.ExchangeRates
{
    /// <summary>
    /// Fetches the latest CAD-based rates from the Frankfurter API (v1):
    ///   GET {FrankfurterBaseUrl}latest?base=CAD&amp;symbols=USD,MXN,BRL,EUR
    /// No API key is needed. Rates are published once per working day (around 16:00 CET).
    /// Singleton; each call takes a fresh HttpClient from IHttpClientFactory.
    /// </summary>
    public class FrankfurterClient
    {
        /// <summary>Name of the HttpClient registered in Program.cs (its BaseAddress is the Frankfurter root).</summary>
        public const string HttpClientName = "Frankfurter";

        private readonly IHttpClientFactory _httpClientFactory;

        public FrankfurterClient(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        /// <summary>
        /// Returns a validated snapshot stamped with the current time.
        /// Throws if the request fails or the answer is incomplete, so bad data is never saved.
        /// </summary>
        public async Task<ExchangeRateSnapshot> FetchLatestAsync(CancellationToken cancellationToken = default)
        {
            string symbols = string.Join(",", ExchangeRateSnapshot.TargetCurrencies);
            HttpClient http = _httpClientFactory.CreateClient(HttpClientName);

            // Relative URL without a leading slash so it stays under /v1/.
            FrankfurterLatestResponse response =
                await http.GetFromJsonAsync<FrankfurterLatestResponse>(
                    $"latest?base={ExchangeRateSnapshot.BaseCurrency}&symbols={symbols}", cancellationToken)
                ?? throw new InvalidDataException("Frankfurter returned an empty response.");

            var snapshot = new ExchangeRateSnapshot
            {
                Base = response.Base ?? string.Empty,
                Date = response.Date ?? string.Empty,
                Rates = response.Rates ?? new Dictionary<string, double>(),
                FetchedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };
            snapshot.Validate();
            return snapshot;
        }

        /// <summary>Frankfurter v1 "latest" body: { "amount": 1.0, "base": "CAD", "date": "...", "rates": { ... } }</summary>
        private sealed class FrankfurterLatestResponse
        {
            public double Amount { get; set; }
            public string? Base { get; set; }
            public string? Date { get; set; }
            public Dictionary<string, double>? Rates { get; set; }
        }
    }
}
