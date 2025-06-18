using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Zela.Models.ViewModels;
using Zela.Services;

namespace Zela.Controllers
{
    public class MeetingController : Controller
    {
        private readonly IMeetingService _svc;
        public MeetingController(IMeetingService svc) => _svc = svc;

        [HttpGet]
        public IActionResult Index() => View();

        [HttpGet]
        public IActionResult Create()
        {
            var vm = new CreateMeetingViewModel {
                CreatorId = int.Parse(User.FindFirst("UserId")?.Value ?? "0")
            };
            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateMeetingViewModel vm)
        {
            if (!ModelState.IsValid) return View(vm);

            var code = await _svc.CreateMeetingAsync(vm.CreatorId);
            return RedirectToAction(nameof(Room), new { code });
        }

        [HttpGet]
        public IActionResult Join()
            => View(new JoinMeetingViewModel());

        [HttpPost]
        public async Task<IActionResult> Join(JoinMeetingViewModel vm)
        {
            if (!ModelState.IsValid) return View(vm);

            var result = await _svc.JoinMeetingAsync(vm.Password);
            if (!result.Success)
            {
                ModelState.AddModelError("", result.Error);
                return View(vm);
            }

            return RedirectToAction(nameof(Room), new { code = vm.Password });
        }
        
        [HttpGet]
        public async Task<IActionResult> Room(string code)
        {
            // Lấy userId từ claim "UserId" (vì lúc tạo bạn đã gán claim này)
            int userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");

            // 2. Xác định host
            ViewBag.IsHost = await _svc.IsHostAsync(code, userId);

            // 3. Luôn truyền meeting code
            ViewBag.MeetingCode = code;
            return View();
        }
    }
}