using System.Collections.Concurrent;

namespace TibiaMarche_WebApp.Models
{
    public enum Currency
    {
        CAD,
        USD,
        MXN,
        BRL,
        EUR
    }

    public static class CurrencyExtensions
    {
        private static readonly Dictionary<Currency, string> DisplayNames = new()
    {
        { Currency.CAD, "Canadian Dollars" },
        { Currency.USD, "US Dollars" },
        { Currency.MXN, "Mexican Pesos" },
        { Currency.BRL, "Brazilian Reais" },
        { Currency.EUR, "Euros" }
    };

        // 1 CAD = rate units of this currency. CAD itself is always 1.0.
        // Concurrent: rates may be set from a background/Firestore callback and read by requests.
        private static readonly ConcurrentDictionary<Currency, double> Rates =
            new(Enum.GetValues<Currency>().Select(c => new KeyValuePair<Currency, double>(c, 1.0)));



        // Replaces Java's toString(): the readable version of each currency
        public static string DisplayName(this Currency currency)
        {
            return DisplayNames[currency];
        }

        // Returns how much of this currency equals $1 CAD
        public static double GetRate(this Currency currency)
        {
            return Rates[currency];
        }

        // Sets how much of this currency equals 1 CAD (Admin). CAD is fixed at 1.0
        public static void SetRate(this Currency currency, double rateFromCAD)
        {
            if (currency == Currency.CAD)
            {
                throw new ArgumentException("The CAD rate is fixed at 1.0.");
            }
            if (rateFromCAD <= 0 || double.IsNaN(rateFromCAD) || double.IsInfinity(rateFromCAD))
            {
                throw new ArgumentException("Rate must be a positive number.");
            }
            Rates[currency] = rateFromCAD;
        }

        // Sets several rates at once, e.g. the map read from Firestore. CAD entries are ignored
        // (Java's static Currency.setRates)
        public static void SetRates(IDictionary<Currency, double?> rates)
        {
            foreach (KeyValuePair<Currency, double?> entry in rates)
            {
                if (entry.Key != Currency.CAD && entry.Value != null)
                {
                    entry.Key.SetRate(entry.Value.Value);
                }
            }
        }

        // Converts an amount in CAD into this currency (display and checkout only)
        public static double ConvertFromCAD(this Currency currency, double amountCAD)
        {
            return amountCAD * currency.GetRate();
        }
    }
}