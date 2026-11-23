using System.ComponentModel.DataAnnotations;

namespace TibiaMarche_WebApp.ViewModels
{
    //What the Register form sends (same checks as RegisterActivity in Android)
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Enter your email")]
        [EmailAddress(ErrorMessage = "This is not a valid email")]
        [Display(Name = "Email")]
        public string Email { get; set; } = "";

        [Required(ErrorMessage = "Enter a password")]
        [MinLength(6, ErrorMessage = "At least 6 characters")]   // Firebase minimum
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = "";

        [Required(ErrorMessage = "Confirm your password")]
        [Compare(nameof(Password), ErrorMessage = "Passwords don't match")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        public string ConfirmPassword { get; set; } = "";
    }
}