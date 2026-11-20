using Microsoft.AspNetCore.Mvc;
using RaceDay.Mvc.Filters;
using RaceDay.Mvc.Models;
using RaceDay.Mvc.Services;

namespace RaceDay.Mvc.Controllers
{
    [SessionAuth("Organiser")]
    public class OrganiserController : BaseController
    {
        private readonly ApiClient _api;
        private readonly IWebHostEnvironment _env;

        public OrganiserController(ApiClient api, IWebHostEnvironment env)
        {
            _api = api;
            _env = env;
        }

        // -------------------- Dashboard --------------------
        public async Task<IActionResult> Dashboard()
        {
            var result = await _api.GetDashboardAsync();
            if (!result.Success || result.Data == null)
            {
                TempData["Error"] = result.Error ?? "Could not load the dashboard.";
                return View(new OrganiserDashboardDto());
            }
            return View(result.Data);
        }

        // -------------------- Events --------------------
        public async Task<IActionResult> Events()
        {
            var result = await _api.GetMyEventsAsync();
            ViewBag.ApiError = result.Success ? null : result.Error;
            return View(result.Data ?? new List<OrganiserEventSummaryDto>());
        }

        [HttpGet]
        public IActionResult CreateEvent() => View(new EventDto { EventDate = DateTime.Today.AddMonths(1) });

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateEvent(EventDto dto, IFormFile? banner)
        {
            if (!ModelState.IsValid) return View(dto);

            if (banner != null && banner.Length > 0)
            {
                var upload = await FileUpload.SaveImageAsync(banner, _env, "events");
                if (!upload.ok) { ModelState.AddModelError("", upload.error!); return View(dto); }
                dto.BannerUrl = upload.url;
            }

            var result = await _api.CreateEventAsync(dto);
            if (!result.Success)
            {
                ModelState.AddModelError("", result.Error ?? "Could not create the event.");
                return View(dto);
            }

            TempData["Success"] = "Event created. Now add its categories.";
            return RedirectToAction(nameof(Categories), new { id = result.Data!.EventID });
        }

        [HttpGet]
        public async Task<IActionResult> EditEvent(int id)
        {
            var result = await _api.GetEventAsync(id);
            if (!result.Success || result.Data == null)
            {
                TempData["Error"] = result.Error ?? "Event not found.";
                return RedirectToAction(nameof(Events));
            }
            var e = result.Data;
            return View(new EventDto
            {
                EventID = e.EventID,
                EventName = e.EventName,
                EventDate = e.EventDate,
                Location = e.Location,
                DistanceKM = e.DistanceKM,
                EventType = e.EventType,
                RouteInfo = e.RouteInfo,
                Description = e.Description,
                BannerUrl = e.BannerUrl
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditEvent(int id, EventDto dto, IFormFile? banner)
        {
            if (!ModelState.IsValid) return View(dto);

            if (banner != null && banner.Length > 0)
            {
                var upload = await FileUpload.SaveImageAsync(banner, _env, "events");
                if (!upload.ok) { ModelState.AddModelError("", upload.error!); return View(dto); }
                dto.BannerUrl = upload.url;
            }

            var result = await _api.UpdateEventAsync(id, dto);
            if (!result.Success)
            {
                ModelState.AddModelError("", result.Error ?? "Could not update the event.");
                return View(dto);
            }

            TempData["Success"] = "Event updated.";
            return RedirectToAction(nameof(Events));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteEvent(int id)
        {
            var result = await _api.DeleteEventAsync(id);
            TempData[result.Success ? "Success" : "Error"] = result.Success
                ? "Event deleted."
                : (result.Error ?? "Could not delete the event.");
            return RedirectToAction(nameof(Events));
        }

        // -------------------- Categories --------------------
        public async Task<IActionResult> Categories(int id)
        {
            var ev = await _api.GetEventAsync(id);
            if (!ev.Success || ev.Data == null)
            {
                TempData["Error"] = ev.Error ?? "Event not found.";
                return RedirectToAction(nameof(Events));
            }
            return View(ev.Data);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddCategory(int eventId, CategoryDto dto)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Please complete all category fields.";
                return RedirectToAction(nameof(Categories), new { id = eventId });
            }
            var result = await _api.CreateCategoryAsync(eventId, dto);
            TempData[result.Success ? "Success" : "Error"] = result.Success
                ? "Category added."
                : (result.Error ?? "Could not add the category.");
            return RedirectToAction(nameof(Categories), new { id = eventId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCategory(int eventId, CategoryDto dto)
        {
            var result = await _api.UpdateCategoryAsync(eventId, dto.CategoryID, dto);
            TempData[result.Success ? "Success" : "Error"] = result.Success
                ? "Category updated."
                : (result.Error ?? "Could not update the category.");
            return RedirectToAction(nameof(Categories), new { id = eventId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCategory(int eventId, int categoryId)
        {
            var result = await _api.DeleteCategoryAsync(eventId, categoryId);
            TempData[result.Success ? "Success" : "Error"] = result.Success
                ? "Category deleted."
                : (result.Error ?? "Could not delete the category.");
            return RedirectToAction(nameof(Categories), new { id = eventId });
        }

        // -------------------- Enrolments --------------------
        public async Task<IActionResult> Enrolments(int id)
        {
            var ev = await _api.GetEventAsync(id);
            if (!ev.Success || ev.Data == null)
            {
                TempData["Error"] = ev.Error ?? "Event not found.";
                return RedirectToAction(nameof(Events));
            }
            var list = await _api.GetEventEnrolmentsAsync(id);
            ViewBag.Event = ev.Data;
            ViewBag.ApiError = list.Success ? null : list.Error;
            return View(list.Data ?? new List<EventEnrolmentDto>());
        }

        // -------------------- Results --------------------
        public async Task<IActionResult> Results(int id)
        {
            var ev = await _api.GetEventAsync(id);
            if (!ev.Success || ev.Data == null)
            {
                TempData["Error"] = ev.Error ?? "Event not found.";
                return RedirectToAction(nameof(Events));
            }
            var list = await _api.GetEventResultsAsync(id);
            ViewBag.Event = ev.Data;
            ViewBag.ApiError = list.Success ? null : list.Error;
            return View(list.Data ?? new List<EventEnrolmentDto>());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveResult(ResultEntryInput input)
        {
            if (string.IsNullOrWhiteSpace(input.FinishTime) || !TimeSpan.TryParse(input.FinishTime, out var time))
            {
                TempData["Error"] = "Please enter a valid finish time (hh:mm:ss).";
                return RedirectToAction(nameof(Results), new { id = input.EventID });
            }
            if (input.Position is null or < 1)
            {
                TempData["Error"] = "Please enter a valid finishing position.";
                return RedirectToAction(nameof(Results), new { id = input.EventID });
            }

            var dto = new ResultDto
            {
                FinishTime = time,
                Position = input.Position!.Value,
                Status = string.IsNullOrWhiteSpace(input.Status) ? "Finished" : input.Status,
                IsPublished = input.IsPublished
            };

            var result = input.ResultID.HasValue
                ? await _api.UpdateResultAsync(input.ResultID.Value, dto)
                : await _api.CaptureResultAsync(input.EnrolmentID, dto);

            TempData[result.Success ? "Success" : "Error"] = result.Success
                ? "Result saved."
                : (result.Error ?? "Could not save the result.");
            return RedirectToAction(nameof(Results), new { id = input.EventID });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PublishResults(int eventId)
        {
            var result = await _api.PublishResultsAsync(eventId);
            TempData[result.Success ? "Success" : "Error"] = result.Success
                ? "All captured results have been published to participants."
                : (result.Error ?? "Could not publish results.");
            return RedirectToAction(nameof(Results), new { id = eventId });
        }
    }
}
