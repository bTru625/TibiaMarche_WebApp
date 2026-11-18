namespace TibiaMarche_WebApp.Models
{
    public enum Vocation
    {
        ANY,
        KNIGHT,
        PALADIN,
        SORCERER,
        DRUID,
        MONK

    }


    public static class VocationExtensions
    {
        public static string DisplayName(this Vocation vocation)
        {
            return vocation switch
            {
                Vocation.ANY => "Any",
                Vocation.KNIGHT => "Knight",
                Vocation.PALADIN => "Paladin",
                Vocation.SORCERER => "Sorcerer",
                Vocation.DRUID => "Druid",
                Vocation.MONK => "Monk",
                _ => vocation.ToString()
            };
        }
    }

}