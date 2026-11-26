using Microsoft.AspNetCore.Mvc;
using TibiaMarche_WebApp.Models;
using TibiaMarche_WebApp.Models.ViewModels;
using TibiaMarche_WebApp.Services.Auth;
using TibiaMarche_WebApp.Services.ExchangeRates;

namespace TibiaMarche_WebApp.Controllers
{
    /// <summary>
    /// S3 [Web] Account page (/Account): currency (buyers only; admins work in CAD), change password,
    /// change email. Log out is on this page.
    ///
    /// Any signed-in user may open it ([RequireRole] with no role). The uid always comes from the
    /// signed-in user (ICurrentUser), never from a form field, so nobody can change another account.
    ///
    /// TODO(B - S5 "Swap Account stubs..."): password/email use StubFirebaseAuthService until then.
    /// TODO(J - S2): the cookie keeps the old email after an email change; rebuild the principal with
    /// J's helper, or ask the user to sign in again (a confirmation email must be clicked first anyway).
    /// </summary>
    [RequireRole]
    public class AccountController : Controller
    {
        private readonly ICurrentUser _user;
        private readonly IUserProfileService _profiles;
        private readonly IFirebaseAuthService _auth;
        private readonly ExchangeRateService _rates;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            ICurrentUser user,
            IUserProfileService profiles,
            IFirebaseAuthService auth,
            ExchangeRateService rates,
            ILogger<AccountController> logger)
        {
            _user = user;
            _profiles = profiles;
            _auth = auth;
            _rates = rates;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            return View(await BuildModelAsync(null));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> SetCurrency(Currency currency)
        {
            if (_user.IsAdmin)
            {
                // Admins work in CAD only.
                return RedirectToAction(nameof(Index));
            }
            if (!Enum.IsDefined(currency))
            {
                TempData["Error"] = "Choose one of the listed currencies.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                await _profiles.SetCurrencyAsync(_user.Uid!, currency);
                TempData["Message"] = $"Currency saved: {currency.DisplayName()}.";
            }
            catch (Exception ex) when (ex is HttpRequestException or InvalidOperationException)
            {
                _logger.LogError(ex, "Saving the currency failed");
                TempData["Error"] = "Couldn't save your currency. Try again.";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(string? oldPassword, string? newPassword, string? confirmPassword)
        {
            if (string.IsNullOrEmpty(oldPassword) || string.IsNullOrEmpty(newPassword))
            {
                TempData["Error"] = "Enter your current password and a new password.";
            }
            else if (newPassword.Length < 6)
            {
                TempData["Error"] = "The new password must be at least 6 characters.";
            }
            else if (newPassword != confirmPassword)
            {
                TempData["Error"] = "The new password and its confirmation do not match.";
            }
            else
            {
                AuthResult result = await _auth.ChangePasswordAsync(_user.Email!, oldPassword, newPassword);
                TempData[result.Success ? "Message" : "Error"] = result.Message;
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeEmail(string? password, string? newEmail)
        {
            if (string.IsNullOrEmpty(password) || string.IsNullOrWhiteSpace(newEmail))
            {
                TempData["Error"] = "Enter your password and the new email address.";
            }
            else if (!new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(newEmail))
            {
                TempData["Error"] = "Enter a valid email address.";
            }
            else
            {
                AuthResult result = await _auth.ChangeEmailAsync(_user.Email!, password, newEmail.Trim());
                TempData[result.Success ? "Message" : "Error"] = result.Message;
            }
            return RedirectToAction(nameof(Index));
        }

        private async Task<AccountViewModel> BuildModelAsync(Currency? overrideCurrency)
        {
            ViewData["Title"] = "Account";
            // Admins see the admin navbar here too, with "Account" highlighted.
            if (_user.IsAdmin)
            {
                ViewData["AdminSection"] = "Account";
            }

            Currency currency = Currency.CAD;
            if (!_user.IsAdmin)
            {
                try
                {
                    currency = overrideCurrency ?? await _profiles.GetCurrencyAsync(_user.Uid!);
                }
                catch (Exception ex) when (ex is HttpRequestException or InvalidOperationException)
                {
                    _logger.LogWarning(ex, "Could not read the saved currency; showing CAD");
                }
            }

            return new AccountViewModel
            {
                Email = _user.Email ?? string.Empty,
                IsAdmin = _user.IsAdmin,
                Currency = currency,
                ExamplePrice = ExamplePrice(currency)
            };
        }

        // "Example: 10.00 CAD = 7.20 USD". Falls back to CAD text when no rates are loaded yet.
        private string ExamplePrice(Currency currency)
        {
            const double exampleCad = 10.00;
            if (currency == Currency.CAD)
            {
                return $"Example: {exampleCad:0.00} CAD";
            }
            if (!_rates.IsReady)
            {
                return $"Example: {exampleCad:0.00} CAD (exchange rates are not available right now, so prices show in CAD)";
            }
            return $"Example: {exampleCad:0.00} CAD = {_rates.Convert(exampleCad, Currency.CAD, currency):0.00} {currency}";
        }
    }
}
