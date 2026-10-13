using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using AcxiomCRM.Models;

namespace AcxiomCRM.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Dashboard");
        }
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        var exceptionFeature = HttpContext.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerPathFeature>();
        if (exceptionFeature?.Error != null)
        {
            _logger.LogError(exceptionFeature.Error, "Unhandled exception on path: {Path}", exceptionFeature.Path);
        }

        return View(new ErrorViewModel 
        { 
            RequestId = System.Diagnostics.Activity.Current?.Id ?? HttpContext.TraceIdentifier,
            ErrorMessage = exceptionFeature?.Error?.Message,
            StackTrace = exceptionFeature?.Error?.StackTrace
        });
    }
}
