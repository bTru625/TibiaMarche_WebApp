using System.ComponentModel.DataAnnotations;

namespace TibiaMarche_WebApp.Models.ViewModels
{
    /// <summary>What the buyer's Request Item form posts.</summary>
    public class RequestFormInput : IValidatableObject
    {
        [Required(ErrorMessage = "Enter the item you are looking for."), StringLength(80)]
        [Display(Name = "Item")]
        public string? ItemName { get; set; }

        [Required(ErrorMessage = "Choose a server.")]
        [Display(Name = "Server")]
        public string? ServerName { get; set; }

        /// <summary>false = notify the account email, true = notify AltEmail.</summary>
        public bool UseAltEmail { get; set; }

        [Display(Name = "Alternative email")]
        public string? AltEmail { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (UseAltEmail && (string.IsNullOrWhiteSpace(AltEmail) || !new EmailAddressAttribute().IsValid(AltEmail)))
            {
                yield return new ValidationResult("Enter a valid email address.", new[] { nameof(AltEmail) });
            }
        }
    }

    public class KnownItemViewModel
    {
        public string Name { get; set; } = string.Empty;
        public string Img { get; set; } = string.Empty;
    }

    public class RequestFormViewModel
    {
        public RequestFormInput Input { get; set; } = new();
        public List<string> Servers { get; set; } = new();
        public List<KnownItemViewModel> KnownItems { get; set; } = new();
        public string? AccountEmail { get; set; }
        public bool Submitted { get; set; }
    }

    public class AccountViewModel
    {
        public string Email { get; set; } = string.Empty;
        public bool IsAdmin { get; set; }
        public Currency Currency { get; set; } = Currency.CAD;
        public string ExamplePrice { get; set; } = string.Empty;
    }
}
