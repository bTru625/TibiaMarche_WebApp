using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using TibiaMarche_WebApp.Models;
using TibiaMarche_WebApp.Services;
using TibiaMarche_WebApp.ViewModels;

namespace TibiaMarche_WebApp.Controllers
{

    // Login, Register, Forgot password and Logout, with the same Firebase as the Android app.
    // After a good login we save uid, email and role in the login cookie (see Program.cs).
    //   Admin -> /Admin         Buyer -> back to the page they came from (returnUrl) or Home
  
    public class AccountController : Controller
    {
        private readonly UserRepository _users;

        public AccountController(UserRepository users)
        {
            _users = users;
        }

        // ---------------------------------------------------------------- Login

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }
            ViewData["ReturnUrl"] = returnUrl;
            return View(new LoginViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            AuthResult result = await _users.LoginAsync(model.Email, model.Password);
            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.Error!);
                return View(model);
            }

            await SignInAsync(result.User!);

            if (result.User!.Role == Roles.Admin)
            {
                return RedirectToAction("Index", "Admin");
            }
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))   // only our own pages
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction("Index", "Home");
        }

        // ---------------------------------------------------------------- Register (buyers only)

        [HttpGet]
        public IActionResult Register()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }
            return View(new RegisterViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            AuthResult result = await _users.RegisterBuyerAsync(model.Email, model.Password);
            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.Error!);
                return View(model);
            }

            await SignInAsync(result.User!);
            TempData["Message"] = "Account created";
            return RedirectToAction("Index", "Home");
        }

        // ---------------------------------------------------------------- Forgot password

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View(new ForgotPasswordViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            string? error = await _users.SendPasswordResetAsync(model.Email);
            if (error != null)
            {
                ModelState.AddModelError(string.Empty, error);
                return View(model);
            }

            TempData["Message"] = "If this email has an account, a reset link was sent";
            return RedirectToAction(nameof(Login));
        }

        // ---------------------------------------------------------------- Logout (POST only)

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }

        // ---------------------------------------------------------------- helper

        /// <summary>Creates the login cookie with the user's uid, email and role.</summary>
        private async Task SignInAsync(Models.User user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),   // uid
                new Claim(ClaimTypes.Name, user.Email),          // User.Identity.Name in the views
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role)            // "ADMIN" or "BUYER"
            };
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
        }
    }
}