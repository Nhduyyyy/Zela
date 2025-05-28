using Microsoft.AspNetCore.Mvc;

namespace Zela.Controllers
{
    public class DashboardController : Controller
    {
        public IActionResult Index()
        {
            ViewBag.FullName = HttpContext.Session.GetString("FullName");
            ViewBag.AvatarUrl = HttpContext.Session.GetString("AvatarUrl");
            return View();
        }
    }
}