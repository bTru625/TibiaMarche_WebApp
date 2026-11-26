using TibiaMarche_WebApp.Services.Firebase;

namespace TibiaMarche_WebApp.Services.Data
{
    /// <summary>
    /// Shared settings. For now only settings/goldRate: the gold-to-CAD rate the Admin updates (about
    /// once a day) and the Add/Edit Item calculators read. Stored as a plain number.
    /// TODO(J): J's Add Item card also reads settings/goldRate. Keep this one path and format.
    /// </summary>
    public class SettingsRepository
    {
        private const string GoldRatePath = "settings/goldRate";
        private readonly RealtimeDatabaseClient _db;

        public SettingsRepository(RealtimeDatabaseClient db)
        {
            _db = db;
        }

        public async Task<double?> GetGoldRateAsync(CancellationToken ct = default)
        {
            return await _db.GetAsync<double?>(GoldRatePath, ct);
        }

        public async Task SetGoldRateAsync(double rate, CancellationToken ct = default)
        {
            await _db.PutAsync(GoldRatePath, rate, ct);
        }
    }
}
