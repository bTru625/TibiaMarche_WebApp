using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RequestStatus = TibiaMarche_WebApp.Models.Request.RequestStatus;
using TibiaMarche_WebApp.Models;
using TibiaMarche_WebApp.Models.ViewModels;
using TibiaMarche_WebApp.Services.Auth;
using TibiaMarche_WebApp.Services.Data;

namespace TibiaMarche_WebApp.Controllers
{
    /// <summary>
    /// S4/S5 [Web] Admin area: All Items (start page) with edit/delete/price/hold, buyer requests table.
    ///
    /// Index (/Admin) is the All Items table: the admin start page the "TibiaMarché ADMIN" logo links to.
    /// Deliveries and Maintenance are still placeholders so every admin navbar link works before its
    /// real page exists. When a card builds the real page, point the navbar link in _Layout.cshtml at
    /// it and delete the placeholder here.
    ///
    /// Admin navbar highlighting: every admin page sets ViewData["AdminSection"]
    /// to "Items", "Requests", "Deliveries" or "Maintenance".
    ///
    /// Every action needs the admin role, checked here in the server code ([RequireRole]), not only in
    /// the views. Every POST is protected by the antiforgery token (the form tag helper adds it).
    /// </summary>
    
    // Only admins: visitors go to the login page, buyers go back to Home (see Program.cs).
  [RequireRole(Roles.Admin)]  
  [Authorize(Roles = Roles.Admin)]
    public class AdminController : Controller
    {
        private const int PageSize = 10;

        private readonly ItemRepository _items;
        private readonly RequestRepository _requests;
        private readonly SettingsRepository _settings;
        private readonly ILogger<AdminController> _logger;

        public AdminController(
            ItemRepository items,
            RequestRepository requests,
            SettingsRepository settings,
            ILogger<AdminController> logger)
        {
            _items = items;
            _requests = requests;
            _settings = settings;
            _logger = logger;
        }

        // ===================== All Items (admin start page, /Admin) =====================

        [HttpGet]
        public async Task<IActionResult> Index(int page = 1)
        {
            ViewData["AdminSection"] = "Items";
            ViewData["Title"] = "All Items";

            var model = new ItemListViewModel { DatabaseConfigured = _items.IsConfigured };
            if (!model.DatabaseConfigured)
            {
                return View(model);
            }

            try
            {
                List<ItemRecord> all = (await _items.GetAllAsync()).OrderBy(i => i.Name, StringComparer.OrdinalIgnoreCase).ThenBy(i => i.Id).ToList();
                double? goldRate = await _settings.GetGoldRateAsync();

                model.TotalItems = all.Count;
                model.TotalPages = Math.Max(1, (int)Math.Ceiling(all.Count / (double)PageSize));
                model.Page = Math.Clamp(page, 1, model.TotalPages);
                model.GoldRateMissing = goldRate == null;
                model.Rows = all
                    .Skip((model.Page - 1) * PageSize)
                    .Take(PageSize)
                    .Select(i => new ItemRowViewModel
                    {
                        Item = i,
                        BreakEven = GoldCalculator.BreakEven(i.GoldPaid, goldRate),
                        Locked = IsLocked(i.Status),
                        LockReason = LockReason(i.Status)
                    })
                    .ToList();
            }
            catch (Exception ex) when (ex is HttpRequestException or InvalidOperationException or System.Text.Json.JsonException)
            {
                _logger.LogError(ex, "Loading items failed");
                TempData["Error"] = "Could not load the items. Check the database connection.";
            }
            return View(model);
        }

        // Old address (/Admin/Items) still works: it shows the same list.
        [HttpGet]
        public IActionResult Items(int page = 1) => RedirectToAction(nameof(Index), new { page });

        // PROPOSED RULE (needs the project owner's yes; see the Dev Board card and the Todo "Decisions needed"):
        // while an Item is Available, On Hold or Maintenance, the Admin may edit LEVEL, PRICE and GOLD PAID
        // (what the wireframe shows); item, server and the rest are read-only. In Cart, Sold and Delivered
        // Items can never be edited or deleted. Status is never edited on the edit form.
        // To widen the rule later, add fields to ItemEditInput, the view and the Apply step below.

        [HttpGet]
        public async Task<IActionResult> ItemEdit(int id)
        {
            ViewData["AdminSection"] = "Items";
            ViewData["Title"] = "Edit Item";

            ItemRecord? item;
            double? goldRate;
            try
            {
                item = await _items.GetAsync(id);
                goldRate = await _settings.GetGoldRateAsync();
            }
            catch (Exception ex) when (ex is HttpRequestException or InvalidOperationException or System.Text.Json.JsonException)
            {
                _logger.LogError(ex, "Loading item {Id} failed", id);
                TempData["Error"] = "Could not load the item. Check the database connection.";
                return RedirectToAction(nameof(Index));
            }
            if (item == null)
            {
                TempData["Error"] = "That item no longer exists.";
                return RedirectToAction(nameof(Index));
            }

            return View(BuildEditModel(item, goldRate, new ItemEditInput
            {
                Id = item.Id,
                Level = item.Level,
                Price = item.Price,
                GoldPaid = item.GoldPaid
            }));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ItemEdit(ItemEditInput input)
        {
            ViewData["AdminSection"] = "Items";
            ViewData["Title"] = "Edit Item";

            if (!ModelState.IsValid)
            {
                ItemRecord? current = await SafeAsync(() => _items.GetAsync(input.Id));
                if (current == null)
                {
                    TempData["Error"] = "That item no longer exists.";
                    return RedirectToAction(nameof(Index));
                }
                return View(BuildEditModel(current, await SafeAsync(() => _settings.GetGoldRateAsync()), input));
            }

            RepoResult result = await RunAsync(() => _items.UpdateAsync(input.Id, item =>
            {
                // Checked again on the Item as it is right now (it may just have gone into a cart).
                if (IsLocked(item.Status))
                {
                    return LockReason(item.Status);
                }
                item.Level = input.Level!.Value;
                item.Price = Math.Round(input.Price!.Value, 2);
                item.GoldPaid = input.GoldPaid!.Value;
                return null;
            }, "Item saved."));

            Flash(result);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ItemPrice(int id, double? price, int page = 1)
        {
            if (price == null || price <= 0 || price > 1_000_000 || double.IsNaN(price.Value))
            {
                TempData["Error"] = "Enter a new price greater than 0.";
                return RedirectToAction(nameof(Index), new { page });
            }

            RepoResult result = await RunAsync(() => _items.UpdateAsync(id, item =>
            {
                if (IsLocked(item.Status))
                {
                    return LockReason(item.Status);
                }
                item.Price = Math.Round(price.Value, 2);
                return null;
            }, "Price updated."));

            Flash(result);
            return RedirectToAction(nameof(Index), new { page });
        }

        // Admin hold: Available -> On Hold. Never allowed for an Item that is In Cart.
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ItemHold(int id, string? returnTo, int page = 1)
        {
            RepoResult result = await RunAsync(() => _items.UpdateAsync(id, item =>
            {
                if (item.Status != Status.Available)
                {
                    return item.Status == Status.In_Cart
                        ? "That item is in a cart right now, so it cannot be put on hold."
                        : $"Only an Available item can be put on hold (this one is {item.Status.DisplayName()}).";
                }
                item.Status = Status.On_Hold;
                return null;
            }, "Item put on hold."));

            Flash(result);
            return BackTo(returnTo, id, page);
        }

        // Release: On Hold -> Available.
        // TODO(J - S8 "Maintenance mode"): if a daily maintenance window is active, release to
        // Status.Maintenance instead (it becomes Available when maintenance stops). MaintenanceWindow
        // data does not exist yet, so for now this always goes to Available.
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ItemRelease(int id, string? returnTo, int page = 1)
        {
            RepoResult result = await RunAsync(() => _items.UpdateAsync(id, item =>
            {
                if (item.Status != Status.On_Hold)
                {
                    return $"Only an item on hold can be released (this one is {item.Status.DisplayName()}).";
                }
                item.Status = Status.Available;
                return null;
            }, "Item released."));

            Flash(result);
            return BackTo(returnTo, id, page);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ItemDelete(int id, int page = 1)
        {
            RepoResult result = await RunAsync(() => _items.DeleteAsync(id,
                item => IsLocked(item.Status) ? LockReason(item.Status) : null,
                "Item deleted."));

            Flash(result);
            return RedirectToAction(nameof(Index), new { page });
        }

        // ===================== Requests =====================

        // One row per item + server (names compared ignoring case and extra spaces), counting OPEN requests only.
        [HttpGet]
        public async Task<IActionResult> Requests()
        {
            ViewData["AdminSection"] = "Requests";
            ViewData["Title"] = "Item Requests";

            var model = new RequestListViewModel { DatabaseConfigured = _requests.IsConfigured };
            if (!model.DatabaseConfigured)
            {
                return View(model);
            }

            try
            {
                List<RequestRecord> all = await _requests.GetAllAsync();
                model.Groups = all
                    .Where(r => r.Status == RequestStatus.OPEN)
                    .GroupBy(r => (Name: RequestRepository.Normalise(r.ItemName), Server: RequestRepository.Normalise(r.ServerName)))
                    .Select(g => new RequestGroupViewModel
                    {
                        // Show the spelling of the most recent request in the group.
                        ItemName = g.OrderByDescending(r => r.CreatedAt).First().ItemName,
                        ServerName = g.OrderByDescending(r => r.CreatedAt).First().ServerName,
                        Count = g.Count(),
                        Latest = DateTimeOffset.FromUnixTimeMilliseconds(g.Max(r => r.CreatedAt))
                    })
                    .OrderByDescending(g => g.Count)
                    .ThenBy(g => g.ItemName, StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }
            catch (Exception ex) when (ex is HttpRequestException or InvalidOperationException or System.Text.Json.JsonException)
            {
                _logger.LogError(ex, "Loading requests failed");
                TempData["Error"] = "Could not load the requests. Check the database connection.";
            }
            return View(model);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestsFulfill(string itemName, string serverName)
        {
            try
            {
                int changed = await _requests.MarkGroupFulfilledAsync(itemName, serverName);
                TempData["Message"] = changed == 0
                    ? "Nothing to mark: those requests were already handled."
                    : $"Marked {changed} request{(changed == 1 ? "" : "s")} for {itemName} on {serverName} as fulfilled.";
            }
            catch (Exception ex) when (ex is HttpRequestException or InvalidOperationException or System.Text.Json.JsonException)
            {
                _logger.LogError(ex, "Marking requests fulfilled failed");
                TempData["Error"] = "Could not update the requests. Check the database connection.";
            }
            return RedirectToAction(nameof(Requests));
        }

        // ===================== Placeholders (other cards) =====================

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

        // ===================== Helpers =====================

        private static bool IsLocked(Status status)
            => status == Status.In_Cart || status == Status.Sold || status == Status.Delivered;

        private static string LockReason(Status status) => status switch
        {
            Status.In_Cart => "This item is in a buyer's cart, so it cannot be changed or deleted right now.",
            Status.Sold => "This item is sold and waiting for delivery, so it cannot be changed or deleted.",
            Status.Delivered => "This item has been delivered, so it cannot be changed or deleted.",
            _ => string.Empty
        };

        private static ItemEditViewModel BuildEditModel(ItemRecord item, double? goldRate, ItemEditInput input)
        {
            input.Id = item.Id;
            return new ItemEditViewModel
            {
                Item = item,
                Input = input,
                GoldRate = goldRate,
                Locked = IsLocked(item.Status),
                LockReason = LockReason(item.Status)
            };
        }

        private void Flash(RepoResult result)
        {
            TempData[result.Ok ? "Message" : "Error"] = result.Message;
        }

        // Only whitelisted places: never redirect to a URL taken from the request.
        private IActionResult BackTo(string? returnTo, int id, int page)
        {
            return returnTo == "edit"
                ? RedirectToAction(nameof(ItemEdit), new { id })
                : RedirectToAction(nameof(Index), new { page });
        }

        private async Task<RepoResult> RunAsync(Func<Task<RepoResult>> action)
        {
            try
            {
                return await action();
            }
            catch (Exception ex) when (ex is HttpRequestException or InvalidOperationException or System.Text.Json.JsonException)
            {
                _logger.LogError(ex, "Item change failed");
                return new RepoResult(false, "Could not reach the database. Nothing was changed.");
            }
        }

        private async Task<T> SafeAsync<T>(Func<Task<T>> read)
        {
            try
            {
                return await read();
            }
            catch (Exception ex) when (ex is HttpRequestException or InvalidOperationException or System.Text.Json.JsonException)
            {
                _logger.LogError(ex, "Database read failed");
                return default!;
            }
        }

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
