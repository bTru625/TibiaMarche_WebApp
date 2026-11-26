using Microsoft.AspNetCore.Mvc;
using TibiaMarche_WebApp.Models;
using TibiaMarche_WebApp.Models.ViewModels;
using TibiaMarche_WebApp.Services.Auth;
using TibiaMarche_WebApp.Services.Data;
using RequestStatus = TibiaMarche_WebApp.Models.Request.RequestStatus;

namespace TibiaMarche_WebApp.Controllers
{
    /// <summary>
    /// S5 [Web] Buyer "Request an item" form (/Request). Saves to requests/{key} with the same fields
    /// as Android. The admin table that groups these lives in AdminController.Requests.
    ///
    /// Guests may open the form; when a guest submits, they are sent to Login and come back to the form.
    /// </summary>
    [BlockAdmin]
    public class RequestController : Controller
    {
        // TODO(J - S2 "[Web] Auth"): replace with the real login URL (Url.Action("Login", "Auth")).
        private const string LoginPath = "/Account/Login";

        private readonly RequestRepository _requests;
        private readonly ItemRepository _items;
        private readonly ICurrentUser _user;
        private readonly ILogger<RequestController> _logger;

        public RequestController(RequestRepository requests, ItemRepository items, ICurrentUser user, ILogger<RequestController> logger)
        {
            _requests = requests;
            _items = items;
            _user = user;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index(bool submitted = false)
        {
            ViewData["Title"] = "Request an item";
            RequestFormViewModel model = await BuildModelAsync(new RequestFormInput());
            model.Submitted = submitted;
            return View(model);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(RequestFormInput input)
        {
            ViewData["Title"] = "Request an item";

            // Guests log in first and come back to this form (local path only).
            if (!_user.IsAuthenticated)
            {
                return Redirect($"{LoginPath}?returnUrl={Uri.EscapeDataString(Url.Action(nameof(Index), "Request") ?? "/Request")}");
            }

            input.ItemName = input.ItemName?.Trim();
            if (!ServerList.Exists(input.ServerName))
            {
                ModelState.AddModelError(nameof(input.ServerName), "Choose a server from the list.");
            }
            if (!ModelState.IsValid)
            {
                return View(await BuildModelAsync(input));
            }

            try
            {
                await _requests.AddAsync(new RequestRecord
                {
                    ItemName = input.ItemName!,
                    ServerName = input.ServerName!,
                    BuyerId = _user.Uid,
                    NotifyEmail = input.UseAltEmail ? input.AltEmail!.Trim() : _user.Email,
                    CreatedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    Status = RequestStatus.OPEN
                });
            }
            catch (Exception ex) when (ex is HttpRequestException or InvalidOperationException)
            {
                _logger.LogError(ex, "Saving a request failed");
                ModelState.AddModelError(string.Empty, "We couldn't save your request. Please try again in a moment.");
                return View(await BuildModelAsync(input));
            }

            // Post/Redirect/Get so a refresh does not send the request twice.
            return RedirectToAction(nameof(Index), new { submitted = true });
        }

        private async Task<RequestFormViewModel> BuildModelAsync(RequestFormInput input)
        {
            var model = new RequestFormViewModel
            {
                Input = input,
                Servers = ServerList.All.Select(s => s.Name).ToList(),
                AccountEmail = _user.Email
            };

            // Suggestions for the item box: names (and pictures) of items already in the catalogue.
            // The box also accepts any other name, because buyers request items that were never listed.
            if (_items.IsConfigured)
            {
                try
                {
                    model.KnownItems = (await _items.GetAllAsync())
                        .Where(i => !string.IsNullOrWhiteSpace(i.Name))
                        .GroupBy(i => RequestRepository.Normalise(i.Name))
                        .Select(g => new KnownItemViewModel { Name = g.First().Name, Img = g.First().Img })
                        .OrderBy(k => k.Name, StringComparer.OrdinalIgnoreCase)
                        .ToList();
                }
                catch (Exception ex) when (ex is HttpRequestException or InvalidOperationException or System.Text.Json.JsonException)
                {
                    _logger.LogWarning(ex, "Could not load item suggestions for the request form");
                }
            }
            return model;
        }
    }
}
