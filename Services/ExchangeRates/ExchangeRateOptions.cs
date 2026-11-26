namespace TibiaMarche_WebApp.Services.ExchangeRates
{
    /// <summary>
    /// Settings for the automatic exchange rate refresh (bound from the "ExchangeRates" config section).
    /// Timings use TimeSpan strings: "1.00:00:00" = 1 day, "01:00:00" = 1 hour, "00:02:00" = 2 minutes.
    /// </summary>
    public class ExchangeRateOptions
    {
        public const string SectionName = "ExchangeRates";

        /// <summary>Frankfurter API root (v1). The client calls "latest?base=CAD&amp;symbols=..." under it.</summary>
        public string FrankfurterBaseUrl { get; set; } = "https://api.frankfurter.dev/v1/";

        /// <summary>How often to fetch new rates after a successful refresh. Default 24 hours.</summary>
        public TimeSpan RefreshInterval { get; set; } = TimeSpan.FromHours(24);

        /// <summary>How long to wait before trying again after a failed refresh. Default 1 hour.</summary>
        public TimeSpan RetryDelay { get; set; } = TimeSpan.FromHours(1);

        /// <summary>Realtime Database path of the shared rates document (Android reads the same path).</summary>
        public string DatabasePath { get; set; } = "exchangeRates/latest";
    }
}
