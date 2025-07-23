using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Zela.Models;

namespace Zela.Controllers;

public class HomeController : Controller
{
    public IActionResult Spline3D()
    {
        return View();
    }
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        return View();
    }
    

}