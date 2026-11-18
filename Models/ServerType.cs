namespace TibiaMarche_WebApp.Models
{

    public enum ServerType
    {
        OPEN_PVP,
        OPTIONAL_PVP,
        RETRO_OPEN_PVP,
        HARDCORE_PVP,
        RETRO_HARDCORE_PVP

    }

    public static class ServerTypeExtensions
    {
        public static string DisplayName(this ServerType type)
        {
            return type switch
            {
                ServerType.OPEN_PVP => "Open PvP",
                ServerType.OPTIONAL_PVP => "Optional PvP",
                ServerType.RETRO_OPEN_PVP => "Retro Open PvP",
                ServerType.HARDCORE_PVP => "Hardcore PvP",
                ServerType.RETRO_HARDCORE_PVP => "Retro Hardcore PvP",
                _ => type.ToString()
            };
        }
    }

}