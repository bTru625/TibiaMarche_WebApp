using TibiaMarche_WebApp.Models;
using TibiaMarche_WebApp.Services.Firebase;

namespace TibiaMarche_WebApp.Services.Auth
{
    /// <summary>Profile data kept at users/{uid} in the Realtime Database (currently only the buyer's currency).</summary>
    public interface IUserProfileService
    {
        Task<Currency> GetCurrencyAsync(string uid);
        Task SetCurrencyAsync(string uid, Currency currency);
    }

    /// <summary>
    /// Reads and writes users/{uid}/currency. Writes only that one field (a PATCH), so the user's
    /// role and other fields are never touched. A missing or unreadable value means CAD.
    /// </summary>
    public class UserProfileService : IUserProfileService
    {
        private readonly RealtimeDatabaseClient _db;

        public UserProfileService(RealtimeDatabaseClient db)
        {
            _db = db;
        }

        public async Task<Currency> GetCurrencyAsync(string uid)
        {
            string? stored = await _db.GetAsync<string>($"users/{uid}/currency");
            return Enum.TryParse(stored, ignoreCase: true, out Currency currency) && Enum.IsDefined(currency)
                ? currency
                : Currency.CAD;
        }

        public async Task SetCurrencyAsync(string uid, Currency currency)
        {
            await _db.PatchAsync($"users/{uid}", new { currency = currency.ToString() });
        }
    }
}
