using Microsoft.AspNetCore.Mvc;
namespace Zela.Controllers;

public class MessengerController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}