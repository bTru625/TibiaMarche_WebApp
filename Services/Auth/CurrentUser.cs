using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using TibiaMarche_WebApp.Controllers;
using TibiaMarche_WebApp.Models;

namespace TibiaMarche_WebApp.Services.Auth
{
    /// <summary>
    /// Who is signed in, read from the auth cookie claims (uid = ClaimTypes.NameIdentifier,
    /// ClaimTypes.Email, ClaimTypes.Role with the lowercase values in <see cref="Roles"/>).
    /// </summary>
    public interface ICurrentUser
    {
        bool IsAuthenticated { get; }
        string? Uid { get; }
        string? Email { get; }
        string? Role { get; }
        bool IsAdmin { get; }
    }

    /// <summary>
    /// TODO(J - S2 "[Web] Auth"): login does not exist yet, so there is no cookie to read.
    /// To let these pages be tried in the meantime, DEVELOPMENT ONLY: when nobody is signed in and
    /// Dev:FallbackRole is set in appsettings.Development.json ("admin" or "buyer"), a fake user with
    /// that role is used. In any other environment, or with the setting removed, signed-out stays
    /// signed-out. Delete the fallback (the else-if branch) once real login is merged.
    /// </summary>
    public class CurrentUser : ICurrentUser
    {
        public const string DevUid = "dev-uid";
        public const string DevEmail = "dev@tibiamarche.test";

        public bool IsAuthenticated { get; }
        public string? Uid { get; }
        public string? Email { get; }
        public string? Role { get; }
        public bool IsAdmin => Role == Roles.Admin;

        public CurrentUser(IHttpContextAccessor accessor, IWebHostEnvironment environment, IConfiguration configuration)
        {
            ClaimsPrincipal? principal = accessor.HttpContext?.User;

            if (principal?.Identity?.IsAuthenticated == true)
            {
                IsAuthenticated = true;
                Uid = principal.FindFirstValue(ClaimTypes.NameIdentifier);
                Email = principal.FindFirstValue(ClaimTypes.Email);
                Role = principal.FindFirstValue(ClaimTypes.Role);
            }
            else if (environment.IsDevelopment())
            {
                // TODO(DEV ONLY - DELETE BEFORE RELEASE): the role switcher in the navbar stores the chosen
                // role in the "tm_dev_role" cookie ("guest", "buyer" or "admin"; see DevRoleController).
                // Without the cookie, Dev:FallbackRole from appsettings.Development.json is used.
                string? chosen = accessor.HttpContext?.Request.Cookies[DevRoleController.CookieName]
                    ?? configuration["Dev:FallbackRole"];

                if (chosen == Roles.Admin || chosen == Roles.Buyer)
                {
                    IsAuthenticated = true;
                    Uid = DevUid;
                    Email = DevEmail;
                    Role = chosen;
                }
                // "guest" (or anything else) stays signed out.
            }
        }
    }

    /// <summary>
    /// Each role's start page, and the page access table.
    ///
    ///   Page                         Guest          Buyer   Admin
    ///   Home (catalogue), Privacy    yes            yes     no -> All Items
    ///   Request an item              yes (*)        yes     no -> All Items
    ///   Account                      no -> login    yes     yes
    ///   Admin (hub, items, requests) no -> login    no      yes
    ///   (*) guests can fill in the form; submitting sends them to login first.
    /// </summary>
    public static class RoleHome
    {
        public const string Admin = "/Admin";
        public const string Buyer = "/";

        public static string For(ICurrentUser user) => user.IsAdmin ? Admin : Buyer;
    }

    /// <summary>
    /// Server-side role check for a controller or action. The admin role must be checked here, in the
    /// server code: the web server talks to Firebase with a service account, which ignores the database
    /// security rules, so hiding a link in the view protects nothing.
    ///
    /// [RequireRole(Roles.Admin)] = admins only. [RequireRole] with no role = any signed-in user.
    /// Signed out -> sent to login and back; wrong role -> sent to their own start page with a message.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class RequireRoleAttribute : ActionFilterAttribute
    {
        // TODO(J - S2): replace with the real login URL (Url.Action("Login", "Auth")) once it exists.
        private const string LoginPath = "/Account/Login";

        private readonly string? _role;

        public RequireRoleAttribute(string? role = null)
        {
            _role = role;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            ICurrentUser user = context.HttpContext.RequestServices.GetRequiredService<ICurrentUser>();

            if (!user.IsAuthenticated)
            {
                // Only local paths go into returnUrl (never an absolute URL from the request).
                string returnUrl = context.HttpContext.Request.Path + context.HttpContext.Request.QueryString;
                context.Result = new RedirectResult($"{LoginPath}?returnUrl={Uri.EscapeDataString(returnUrl)}");
            }
            else if (_role != null && user.Role != _role)
            {
                RedirectHome(context, user, "That page isn't available for your account.");
            }
        }

        internal static void RedirectHome(ActionExecutingContext context, ICurrentUser user, string message)
        {
            if (context.Controller is Controller controller)
            {
                controller.TempData["Error"] = message;
            }
            context.Result = new LocalRedirectResult(RoleHome.For(user));
        }
    }

    /// <summary>
    /// Shopper pages (catalogue, request an item): guests and buyers only. Admins are sent to the admin start page (All Items).
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class BlockAdminAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            ICurrentUser user = context.HttpContext.RequestServices.GetRequiredService<ICurrentUser>();
            if (user.IsAdmin)
            {
                RequireRoleAttribute.RedirectHome(context, user, "Shop pages are for buyers.");
            }
        }
    }
}
