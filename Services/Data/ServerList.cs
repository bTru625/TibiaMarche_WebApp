using TibiaMarche_WebApp.Models;

namespace TibiaMarche_WebApp.Services.Data
{
    /// <summary>
    /// The list of servers (worlds) for dropdowns.
    ///
    /// TODO(J - Foundation card): the real list is J's fixed Server enum (and the Tibia API check in
    /// sprint 9). The names and types below are PLACEHOLDERS so the Request form works today.
    /// Replace the body of <see cref="All"/> with J's enum when it is merged.
    /// </summary>
    public static class ServerList
    {
        public static IReadOnlyList<Server> All { get; } = new List<Server>
        {
            new("Antica", ServerType.OPEN_PVP),
            new("Secura", ServerType.OPEN_PVP),
            new("Bona", ServerType.OPEN_PVP),
        };

        public static bool Exists(string? name)
        {
            return All.Any(s => string.Equals(s.Name, name, StringComparison.Ordinal));
        }
    }
}
