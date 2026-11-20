using Microsoft.AspNetCore.Mvc;
using RaceDay.Mvc.Filters;
using RaceDay.Mvc.Models;
using RaceDay.Mvc.Services;

namespace RaceDay.Mvc.Controllers
{
    [SessionAuth]
    public class ProfileController : BaseController
    {
        private readonly ApiClient _api;
        private readonly IWebHostEnvironment _env;

        public ProfileController(ApiClient api, IWebHostEnvironment env)
        {
            _api = api;
            _env = env;
        }

        public async Task<IActionResult> Index()
        {
            var result = await _api.GetProfileAsync();
            if (!result.Success || result.Data == null)
            {
                TempData["Error"] = result.Error ?? "Could not load your profile.";
                return Redirect(HomeForRole());
            }
            ViewBag.Password = new UpdatePasswordDto();
            return View(result.Data);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(ProfileDto model, IFormFile? profilePicture)
        {
            var dto = new UpdateProfileDto { FullName = model.FullName, PhoneNumber = model.PhoneNumber };

            if (profilePicture != null && profilePicture.Length > 0)
            {
                var upload = await FileUpload.SaveImageAsync(profilePicture, _env, "profiles");
                if (!upload.ok)
                {
                    TempData["Error"] = upload.error;
                    return RedirectToAction(nameof(Index));
                }
                dto.ProfilePictureUrl = upload.url;
            }

            var result = await _api.UpdateProfileAsync(dto);
            if (!result.Success)
            {
                TempData["Error"] = result.Error ?? "Could not update your profile.";
                return RedirectToAction(nameof(Index));
            }

            HttpContext.Session.SetString(SessionKeys.FullName, dto.FullName);
            if (!string.IsNullOrEmpty(dto.ProfilePictureUrl))
                HttpContext.Session.SetString(SessionKeys.ProfilePicture, dto.ProfilePictureUrl);

            TempData["Success"] = "Your profile has been updated.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(UpdatePasswordDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.CurrentPassword) || string.IsNullOrWhiteSpace(dto.NewPassword))
            {
                TempData["Error"] = "Please enter your current and new password.";
                return RedirectToAction(nameof(Index));
            }

            var result = await _api.ChangePasswordAsync(dto);
            TempData[result.Success ? "Success" : "Error"] = result.Success
                ? "Your password has been changed."
                : (result.Error ?? "Could not change your password.");
            return RedirectToAction(nameof(Index));
        }
    }
}
