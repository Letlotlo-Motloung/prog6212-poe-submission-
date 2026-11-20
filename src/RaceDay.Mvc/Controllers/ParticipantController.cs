using Microsoft.AspNetCore.Mvc;
using RaceDay.Mvc.Filters;
using RaceDay.Mvc.Models;
using RaceDay.Mvc.Services;

namespace RaceDay.Mvc.Controllers
{
    [SessionAuth("Participant")]
    public class ParticipantController : BaseController
    {
        private readonly ApiClient _api;
        public ParticipantController(ApiClient api) => _api = api;

        public async Task<IActionResult> MyEnrolments()
        {
            var result = await _api.GetMyEnrolmentsAsync();
            ViewBag.ApiError = result.Success ? null : result.Error;
            return View(result.Data ?? new List<MyEnrolmentDto>());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelEnrolment(int enrolmentId)
        {
            var result = await _api.CancelEnrolmentAsync(enrolmentId);
            TempData[result.Success ? "Success" : "Error"] = result.Success
                ? "Your enrolment was cancelled."
                : (result.Error ?? "Could not cancel the enrolment.");
            return RedirectToAction(nameof(MyEnrolments));
        }

        public async Task<IActionResult> MyResults()
        {
            var result = await _api.GetMyResultsAsync();
            ViewBag.ApiError = result.Success ? null : result.Error;
            return View(result.Data ?? new List<MyResultDto>());
        }
    }
}
