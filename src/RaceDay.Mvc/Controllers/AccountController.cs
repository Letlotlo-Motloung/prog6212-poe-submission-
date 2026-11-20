using Microsoft.AspNetCore.Mvc;
using RaceDay.Mvc.Models;
using RaceDay.Mvc.Services;

namespace RaceDay.Mvc.Controllers
{
    public class AccountController : BaseController
    {
        private readonly ApiClient _api;
        public AccountController(ApiClient api) => _api = api;

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (IsAuthenticated) return Redirect(HomeForRole());
            return View(new LoginDto { ReturnUrl = returnUrl });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginDto dto)
        {
            if (!ModelState.IsValid) return View(dto);

            var result = await _api.LoginAsync(dto);
            if (!result.Success || result.Data == null)
            {
                ModelState.AddModelError("", result.Error ?? "Invalid email or password.");
                return View(dto);
            }

            SetUserSession(result.Data);
            TempData["Success"] = $"Welcome back, {result.Data.FullName}!";

            if (!string.IsNullOrEmpty(dto.ReturnUrl) && Url.IsLocalUrl(dto.ReturnUrl))
                return Redirect(dto.ReturnUrl);

            return Redirect(result.Data.Role == "Organiser" ? "/Organiser/Dashboard" : "/Home/Index");
        }

        [HttpGet]
        public IActionResult Register()
        {
            if (IsAuthenticated) return Redirect(HomeForRole());
            return View(new RegisterDto());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterDto dto)
        {
            if (!ModelState.IsValid) return View(dto);

            var result = await _api.RegisterAsync(dto);
            if (!result.Success || result.Data == null)
            {
                ModelState.AddModelError("", result.Error ?? "Registration failed.");
                return View(dto);
            }

            SetUserSession(result.Data);
            TempData["Success"] = "Your account was created successfully.";
            return Redirect(result.Data.Role == "Organiser" ? "/Organiser/Dashboard" : "/Home/Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Logout()
        {
            ClearUserSession();
            TempData["Success"] = "You have been logged out.";
            return RedirectToAction(nameof(Login));
        }

        [HttpGet]
        public IActionResult AccessDenied() => View();
    }
}
