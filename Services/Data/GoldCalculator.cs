namespace TibiaMarche_WebApp.Services.Data
{
    /// <summary>
    /// The Add/Edit Item calculator: gold paid -> break-even price (CAD) -> price with a profit.
    ///
    /// ASSUMPTION (confirm with the project owner): settings/goldRate is CAD per 1,000,000 gold
    /// ("1 kk"). If the rate is per 1 gold piece instead, change GoldPerRateUnit to 1.
    /// admin-item-edit.js uses the same unit (data-gold-unit), so change them together.
    /// </summary>
    public static class GoldCalculator
    {
        public const double GoldPerRateUnit = 1_000_000;

        public static double? BreakEven(int goldPaid, double? ratePerUnit)
        {
            if (ratePerUnit == null || ratePerUnit <= 0)
            {
                return null;
            }
            return Math.Round(goldPaid / GoldPerRateUnit * ratePerUnit.Value, 2);
        }
    }
}
