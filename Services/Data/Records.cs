using System.Text.Json;
using System.Text.Json.Serialization;
using TibiaMarche_WebApp.Models;

namespace TibiaMarche_WebApp.Services.Data
{
    /// <summary>
    /// One Item as stored at items/{id} in the Realtime Database.
    ///
    /// A flat record (server is just its name) so the same JSON can be read by the Android app.
    /// Enums are stored by name. Fields this class does not know about (for example ones J adds in
    /// the Add Item card) are kept in <see cref="Extra"/>, so an edit made here never erases them.
    ///
    /// TODO(J): check these field names against docs/data-model.md and change them here if they differ.
    /// </summary>
    public class ItemRecord
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Img { get; set; } = string.Empty;
        public double Price { get; set; }                 // CAD
        public string Server { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int Level { get; set; }
        public int GoldPaid { get; set; }                 // Admin-only
        public string? DeliverTo { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public Vocation Vocation { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public Status Status { get; set; }

        /// <summary>Everything else stored on the node; written back unchanged.</summary>
        [JsonExtensionData]
        public Dictionary<string, JsonElement>? Extra { get; set; }
    }

    /// <summary>
    /// One buyer request as stored at requests/{pushKey}. CreatedAt is Unix milliseconds (UTC) so
    /// Android can read it without date parsing.
    /// TODO(J): check these field names against docs/data-model.md.
    /// </summary>
    public class RequestRecord
    {
        /// <summary>The push key. Not stored inside the node; filled in when read.</summary>
        [JsonIgnore]
        public string? Id { get; set; }

        public string ItemName { get; set; } = string.Empty;
        public string ServerName { get; set; } = string.Empty;
        public string? BuyerId { get; set; }
        public string? NotifyEmail { get; set; }
        public long CreatedAt { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public Request.RequestStatus Status { get; set; } = Request.RequestStatus.OPEN;

        [JsonExtensionData]
        public Dictionary<string, JsonElement>? Extra { get; set; }
    }

    /// <summary>Outcome of a repository change: a message the page can show.</summary>
    public record RepoResult(bool Ok, string Message);
}
