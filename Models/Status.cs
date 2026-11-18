namespace TibiaMarche_WebApp.Models
{
    public enum Status
    {
        Available,
        In_Cart,
        Maintenance,
        Sold,
        Delivered,
        On_Hold
    }

    public static class StatusExtensions
    {
        public static string DisplayName(this Status status)
        {
            return status switch
            {
                Status.Available => "Available",
                Status.In_Cart => "In Cart",
                Status.Maintenance => "Maintenance",
                Status.Sold => "Sold",
                Status.Delivered => "Delivered",
                Status.On_Hold => "On Hold",
                _ => status.ToString()
            };
        }

        public static bool IsAvailable(this Status status)
        {
            return status == Status.Available;
        }

        // Sold and Delivered items are never touched by maintenance or holds
        public static bool IsFinal(this Status status)
        {
            return status == Status.Sold || status == Status.Delivered;
        }
    }

}