using System.Text.Json;
using TibiaMarche_WebApp.Services.Firebase;

namespace TibiaMarche_WebApp.Services.Data
{
    /// <summary>
    /// Reads and changes Items at items/{id} in the Realtime Database.
    ///
    /// Every change goes through <see cref="UpdateAsync"/> / <see cref="DeleteAsync"/>: read the Item with
    /// its ETag, check its current status, then write only if nobody changed it in between (the
    /// REST equivalent of a transaction). This is what stops an Item from being edited or deleted
    /// in the instant a buyer adds it to a cart.
    ///
    /// TODO(J): J's Catalog/Cart cards must use the same conditional write (PutIfMatchAsync) when they
    /// change an Item's status, otherwise this protection only works one way.
    /// </summary>
    public class ItemRepository
    {
        private const string Root = "items";
        private const int MaxAttempts = 5;
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        private readonly RealtimeDatabaseClient _db;

        public ItemRepository(RealtimeDatabaseClient db)
        {
            _db = db;
        }

        public bool IsConfigured => _db.IsConfigured;

        /// <summary>All Items. Firebase answers an object keyed by id, or an array when the ids are 0,1,2...; both are handled.</summary>
        public async Task<List<ItemRecord>> GetAllAsync(CancellationToken ct = default)
        {
            JsonElement root = await _db.GetAsync<JsonElement>(Root, ct);
            var items = new List<ItemRecord>();

            if (root.ValueKind == JsonValueKind.Object)
            {
                foreach (JsonProperty property in root.EnumerateObject())
                {
                    AddItem(items, property.Value, property.Name);
                }
            }
            else if (root.ValueKind == JsonValueKind.Array)
            {
                int index = 0;
                foreach (JsonElement element in root.EnumerateArray())
                {
                    AddItem(items, element, index.ToString());
                    index++;
                }
            }
            return items;
        }

        public async Task<ItemRecord?> GetAsync(int id, CancellationToken ct = default)
        {
            return await _db.GetAsync<ItemRecord>($"{Root}/{id}", ct);
        }

        /// <summary>
        /// Changes one Item. <paramref name="change"/> receives the current Item and returns an error message
        /// (nothing is written) or null after modifying it. Retries when the Item changed during the call.
        /// </summary>
        public async Task<RepoResult> UpdateAsync(int id, Func<ItemRecord, string?> change, string okMessage, CancellationToken ct = default)
        {
            string path = $"{Root}/{id}";
            for (int attempt = 0; attempt < MaxAttempts; attempt++)
            {
                (ItemRecord? item, string? etag) = await _db.GetWithETagAsync<ItemRecord>(path, ct);
                if (item == null || etag == null)
                {
                    return new RepoResult(false, "That item no longer exists.");
                }

                string? error = change(item);
                if (error != null)
                {
                    return new RepoResult(false, error);
                }
                if (await _db.PutIfMatchAsync(path, item, etag, ct))
                {
                    return new RepoResult(true, okMessage);
                }
                // Someone changed it between the read and the write: loop and check again.
            }
            return new RepoResult(false, "The item kept changing while saving. Please try again.");
        }

        /// <summary>Deletes one Item if <paramref name="check"/> (run on the current Item) returns null.</summary>
        public async Task<RepoResult> DeleteAsync(int id, Func<ItemRecord, string?> check, string okMessage, CancellationToken ct = default)
        {
            string path = $"{Root}/{id}";
            for (int attempt = 0; attempt < MaxAttempts; attempt++)
            {
                (ItemRecord? item, string? etag) = await _db.GetWithETagAsync<ItemRecord>(path, ct);
                if (item == null || etag == null)
                {
                    return new RepoResult(false, "That item no longer exists.");
                }

                string? error = check(item);
                if (error != null)
                {
                    return new RepoResult(false, error);
                }
                if (await _db.DeleteIfMatchAsync(path, etag, ct))
                {
                    return new RepoResult(true, okMessage);
                }
            }
            return new RepoResult(false, "The item kept changing while deleting. Please try again.");
        }

        private static void AddItem(List<ItemRecord> items, JsonElement element, string key)
        {
            if (element.ValueKind != JsonValueKind.Object)
            {
                return;   // the null hole at index 0 of an array
            }
            ItemRecord? item = element.Deserialize<ItemRecord>(JsonOptions);
            if (item == null)
            {
                return;
            }
            if (item.Id == 0 && int.TryParse(key, out int parsed))
            {
                item.Id = parsed;
            }
            items.Add(item);
        }
    }
}
