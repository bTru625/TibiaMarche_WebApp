using Microsoft.AspNetCore.Mvc;
using TibiaMarche_WebApp.Models;
using TibiaMarche_WebApp.Services.Auth;

namespace TibiaMarche_WebApp.Controllers
{
    /// <summary>
    /// DEVELOPER FEATURE ONLY: lets us switch between guest, user (buyer) and admin from the navbar
    /// while real login does not exist. It only stores a cookie that CurrentUser reads, and it only works
    /// in the Development environment: anywhere else every action answers 404.
    ///
    /// TODO(DEV ONLY - DELETE BEFORE RELEASE): delete this controller, Views/Shared/_DevRoleSwitcher.cshtml,
    /// wwwroot/js/dev-role.js, the partial call in _Layout.cshtml, the .tm-dev-switch CSS and the
    /// "tm_dev_role" cookie branch in Services/Auth/CurrentUser.cs.
    /// </summary>
    public class DevRoleController : Controller
    {
        public const string CookieName = "tm_dev_role";

        private readonly IWebHostEnvironment _environment;

        public DevRoleController(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult Set(string role)
        {
            if (!_environment.IsDevelopment())
            {
                return NotFound();
            }
            if (role != "guest" && role != Roles.Buyer && role != Roles.Admin)
            {
                return BadRequest();
            }

            Response.Cookies.Append(CookieName, role, new CookieOptions
            {
                HttpOnly = true,
                SameSite = SameSiteMode.Strict,
                IsEssential = true
            });

            // Go to the new role's start page: All Items (/Admin) for admins, Home (catalogue) for buyers and guests.
            // The next request reads the new cookie, so the navbar and page access switch with it.
            string label = role == Roles.Admin ? "Admin" : role == Roles.Buyer ? "User" : "Guest";
            TempData["Message"] = $"DEV: now viewing as {label}.";
            return LocalRedirect(role == Roles.Admin ? RoleHome.Admin : RoleHome.Buyer);
        }
    }
}
