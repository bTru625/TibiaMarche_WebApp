using System.Text.Json.Serialization;
using TibiaMarche_WebApp.Models;

namespace TibiaMarche_WebApp.Services.ExchangeRates
{
    /// <summary>
    /// The shared rates document stored at exchangeRates/latest in Realtime Database.
    /// The web app writes it; the Android app only reads it. Keep the JSON names stable.
    ///
    /// {
    ///   "base": "CAD",
    ///   "date": "2026-11-20",          // the day the rates are for (Frankfurter publishes on working days only)
    ///   "rates": { "USD": 0.71, "MXN": 13.1, "BRL": 3.9, "EUR": 0.62 },   // 1 CAD = rate units
    ///   "fetchedAt": 1795304933894     // when the web app saved them, Unix time in milliseconds (UTC)
    /// }
    /// </summary>
    public class ExchangeRateSnapshot
    {
        public const string BaseCurrency = "CAD";

        [JsonPropertyName("base")]
        public string Base { get; set; } = BaseCurrency;

        [JsonPropertyName("date")]
        public string Date { get; set; } = string.Empty;

        [JsonPropertyName("rates")]
        public Dictionary<string, double> Rates { get; set; } = new();

        [JsonPropertyName("fetchedAt")]
        public long FetchedAt { get; set; }

        [JsonIgnore]
        public DateTimeOffset FetchedAtUtc => DateTimeOffset.FromUnixTimeMilliseconds(FetchedAt);

        /// <summary>Every currency except CAD needs a rate.</summary>
        public static IReadOnlyList<Currency> TargetCurrencies { get; } =
            Enum.GetValues<Currency>().Where(c => c != Currency.CAD).ToArray();

        /// <summary>
        /// Throws InvalidDataException unless the base is CAD and every target currency has a
        /// positive, finite rate. Used on Frankfurter answers and on documents read back from the database.
        /// </summary>
        public void Validate()
        {
            if (!string.Equals(Base, BaseCurrency, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException($"Expected base {BaseCurrency} but got '{Base}'.");
            }
            foreach (Currency currency in TargetCurrencies)
            {
                if (!Rates.TryGetValue(currency.ToString(), out double rate))
                {
                    throw new InvalidDataException($"Missing rate for {currency}.");
                }
                if (rate <= 0 || double.IsNaN(rate) || double.IsInfinity(rate))
                {
                    throw new InvalidDataException($"Invalid rate for {currency}: {rate}.");
                }
            }
        }

        /// <summary>The rates keyed by the Currency enum, in the shape CurrencyExtensions.SetRates expects.</summary>
        public Dictionary<Currency, double?> ToCurrencyMap() =>
            TargetCurrencies.ToDictionary(c => c, c => (double?)Rates[c.ToString()]);
    }
}
