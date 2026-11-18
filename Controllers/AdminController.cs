using Microsoft.AspNetCore.Mvc;

namespace TibiaMarche_WebApp.Controllers
{
    /// <summary>
    /// S4 [Web] Admin area entry point.
    ///
    /// Index (/Admin) is the admin home that the "TibiaMarché ADMIN" logo links
    /// to. It becomes the Admin hub page in the S4 hub card.
    ///
    /// The other actions are placeholders so every admin navbar link works
    /// before its real page exists. When a card builds the real page, point
    /// the navbar link in _Layout.cshtml at it and delete the placeholder here.
    ///
    /// Admin navbar highlighting: every admin page sets ViewData["AdminSection"]
    /// to "Items", "Requests", "Deliveries" or "Maintenance".
    /// </summary>
    // TODO(auth): lock this to admins once login exists: [Authorize(Roles = Roles.Admin)]
    public class AdminController : Controller
    {
        // TODO(B - S4 "[Web] Admin hub page"): replace with the real hub.
        [HttpGet]
        public IActionResult Index()
            => ComingSoon("", "Admin hub",
                "The Admin hub with a section for each admin job.",
                "Sprint 4");

        // TODO(B - S4 "[Web] Admin: item list, edit, delete and price change"):
        // replace with the item list page (with an "Add item" button that opens
        // J's S3 Add Item page, which should also set AdminSection = "Items").
        [HttpGet]
        public IActionResult Items()
            => ComingSoon("Items", "Items",
                "All items in a table with Edit and Delete, plus an Add item button.",
                "Sprint 4");

        // TODO(B - S5 "[Web] Item requests: buyer form and admin table").
        [HttpGet]
        public IActionResult Requests()
            => ComingSoon("Requests", "Requests",
                "Buyer item requests grouped by item and server.",
                "Sprint 5");

        // TODO(S7 "[Web] Admin: orders, sale alerts, mark delivered").
        [HttpGet]
        public IActionResult Deliveries()
            => ComingSoon("Deliveries", "Deliveries",
                "Orders waiting for in-game delivery, with Mark delivered.",
                "Sprint 7");

        // TODO(J - S8 "Maintenance mode").
        [HttpGet]
        public IActionResult Maintenance()
            => ComingSoon("Maintenance", "Maintenance",
                "Start and stop daily maintenance.",
                "Sprint 8");

        private ViewResult ComingSoon(string section, string title, string description, string sprint)
        {
            ViewData["AdminSection"] = section;
            ViewData["Title"] = title;
            ViewData["Description"] = description;
            ViewData["Sprint"] = sprint;
            return View("ComingSoon");
        }
    }
}
