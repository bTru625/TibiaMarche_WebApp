using System.ComponentModel.DataAnnotations;
using TibiaMarche_WebApp.Services.Data;

namespace TibiaMarche_WebApp.Models.ViewModels
{
    public class ItemRowViewModel
    {
        public ItemRecord Item { get; set; } = null!;
        public double? BreakEven { get; set; }

        /// <summary>In Cart, Sold or Delivered: cannot be edited, held or deleted.</summary>
        public bool Locked { get; set; }
        public string LockReason { get; set; } = string.Empty;
    }

    public class ItemListViewModel
    {
        public List<ItemRowViewModel> Rows { get; set; } = new();
        public int Page { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
        public int TotalItems { get; set; }
        public bool DatabaseConfigured { get; set; }
        public bool GoldRateMissing { get; set; }
    }

    /// <summary>What the Edit Item form posts. Only these three fields are editable (see ItemEdit in AdminController).</summary>
    public class ItemEditInput
    {
        public int Id { get; set; }

        [Required, Range(1, 5000, ErrorMessage = "Level must be between 1 and 5000.")]
        public int? Level { get; set; }

        [Required, Range(0.01, 1_000_000, ErrorMessage = "Price must be greater than 0.")]
        public double? Price { get; set; }

        [Required, Range(0, int.MaxValue, ErrorMessage = "Gold paid cannot be negative.")]
        public int? GoldPaid { get; set; }
    }

    public class ItemEditViewModel
    {
        public ItemRecord Item { get; set; } = null!;
        public ItemEditInput Input { get; set; } = new();
        public double? GoldRate { get; set; }
        public bool Locked { get; set; }
        public string LockReason { get; set; } = string.Empty;
    }

    public class RequestGroupViewModel
    {
        public string ItemName { get; set; } = string.Empty;
        public string ServerName { get; set; } = string.Empty;
        public int Count { get; set; }
        public DateTimeOffset Latest { get; set; }
    }

    public class RequestListViewModel
    {
        public List<RequestGroupViewModel> Groups { get; set; } = new();
        public bool DatabaseConfigured { get; set; }
    }
}
