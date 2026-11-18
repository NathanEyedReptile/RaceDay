using Microsoft.AspNetCore.Mvc;
using RaceDay.Mvc.Models.Auth;
using RaceDay.Mvc.Services;

namespace RaceDay.Mvc.Controllers
{
    public class AccountController : Controller
    {
        private readonly AuthService _authService;

        public AccountController(AuthService authService)
        {
            _authService = authService;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginRequest request)
        {
            if (!ModelState.IsValid)
            {
                return View(request);
            }

            var result = await _authService.LoginAsync(
                request, HttpContext.Session);

            if (!result.Success)
            {
                ViewBag.Error = result.Error;
                return View(request);
            }

            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();

            return RedirectToAction("Index", "Home");
        }
    }
}