using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using TibiaMarche_WebApp.Models;
using TibiaMarche_WebApp.Services.Auth;

namespace TibiaMarche_WebApp.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    // Home is the catalogue (guests and buyers). Admins are sent to All Items (/Admin).
    // Privacy and Error stay open to everyone.
    [BlockAdmin]
    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
