using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace Zela.Controllers;

[Authorize]
public class MessengerController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}