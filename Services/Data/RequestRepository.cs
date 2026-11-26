using System.Text.RegularExpressions;
using TibiaMarche_WebApp.Models;
using TibiaMarche_WebApp.Services.Firebase;

namespace TibiaMarche_WebApp.Services.Data
{
    /// <summary>Buyer item requests at requests/{pushKey}. Same fields as the Android app.</summary>
    public class RequestRepository
    {
        private const string Root = "requests";
        private readonly RealtimeDatabaseClient _db;

        public RequestRepository(RealtimeDatabaseClient db)
        {
            _db = db;
        }

        public bool IsConfigured => _db.IsConfigured;

        public async Task AddAsync(RequestRecord record, CancellationToken ct = default)
        {
            await _db.PushAsync(Root, record, ct);
        }

        public async Task<List<RequestRecord>> GetAllAsync(CancellationToken ct = default)
        {
            Dictionary<string, RequestRecord>? all = await _db.GetAsync<Dictionary<string, RequestRecord>>(Root, ct);
            var list = new List<RequestRecord>();
            if (all == null)
            {
                return list;
            }
            foreach (KeyValuePair<string, RequestRecord> entry in all)
            {
                entry.Value.Id = entry.Key;
                list.Add(entry.Value);
            }
            return list;
        }

        /// <summary>
        /// Marks every OPEN request for this item + server as FULFILLED. Returns how many were changed.
        /// Item names are compared with <see cref="Normalise"/>, the same rule the admin table groups by.
        /// </summary>
        public async Task<int> MarkGroupFulfilledAsync(string itemName, string serverName, CancellationToken ct = default)
        {
            string wantedName = Normalise(itemName);
            string wantedServer = Normalise(serverName);
            int changed = 0;

            foreach (RequestRecord request in await GetAllAsync(ct))
            {
                bool sameGroup = Normalise(request.ItemName) == wantedName && Normalise(request.ServerName) == wantedServer;
                if (sameGroup && request.Status == Request.RequestStatus.OPEN && request.Id != null)
                {
                    await _db.PatchAsync($"{Root}/{request.Id}", new { status = Request.RequestStatus.FULFILLED.ToString() }, ct);
                    changed++;
                }
            }
            return changed;
        }

        /// <summary>Grouping key: trimmed, lower case, inner spaces collapsed ("  Magic  Sword" == "magic sword").</summary>
        public static string Normalise(string? text)
        {
            return Regex.Replace((text ?? string.Empty).Trim(), @"\s+", " ").ToLowerInvariant();
        }
    }
}
