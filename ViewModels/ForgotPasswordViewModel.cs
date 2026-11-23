using System.ComponentModel.DataAnnotations;

namespace TibiaMarche_WebApp.ViewModels
{
    //What the Forgot password form sends.
    public class ForgotPasswordViewModel
    {
        [Required(ErrorMessage = "Enter your email")]
        [EmailAddress(ErrorMessage = "This is not a valid email")]
        [Display(Name = "Email")]
        public string Email { get; set; } = "";
    }
}