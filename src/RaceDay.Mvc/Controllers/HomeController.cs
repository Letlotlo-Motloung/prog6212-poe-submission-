using Microsoft.AspNetCore.Mvc;
using RaceDay.Mvc.Filters;
using RaceDay.Mvc.Models;
using RaceDay.Mvc.Services;

namespace RaceDay.Mvc.Controllers
{
    public class HomeController : BaseController
    {
        private readonly ApiClient _api;
        public HomeController(ApiClient api) => _api = api;

        public async Task<IActionResult> Index(string? search, string? type, string? location, bool includePast = false)
        {
            var all = await _api.GetEventsAsync();
            var events = all.Data ?? new List<EventDto>();

            ViewBag.Search = search;
            ViewBag.Type = type ?? "All";
            ViewBag.Location = location ?? "All";
            ViewBag.IncludePast = includePast;
            ViewBag.Types = events.Select(e => e.EventType).Where(t => !string.IsNullOrWhiteSpace(t)).Distinct().OrderBy(t => t).ToList();
            ViewBag.Locations = events.Select(e => e.Location).Distinct().OrderBy(l => l).ToList();
            ViewBag.ApiError = all.Success ? null : all.Error;

            var filtered = events.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(search))
                filtered = filtered.Where(e => e.EventName.Contains(search, StringComparison.OrdinalIgnoreCase)
                                            || e.Location.Contains(search, StringComparison.OrdinalIgnoreCase)
                                            || (e.Description?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false));
            if (!string.IsNullOrWhiteSpace(type) && type != "All")
                filtered = filtered.Where(e => e.EventType == type);
            if (!string.IsNullOrWhiteSpace(location) && location != "All")
                filtered = filtered.Where(e => e.Location == location);
            if (!includePast)
                filtered = filtered.Where(e => e.EventDate >= DateTime.Today);

            var list = filtered.OrderBy(e => e.EventDate).ToList();

            // For a logged-in participant, mark which events they are already enrolled in.
            var enrolledEventIds = new HashSet<int>();
            if (IsParticipant)
            {
                var mine = await _api.GetMyEnrolmentsAsync();
                foreach (var e in mine.Data ?? new List<MyEnrolmentDto>()) enrolledEventIds.Add(e.EventID);
            }
            ViewBag.EnrolledEventIds = enrolledEventIds;

            return View(list);
        }

        public async Task<IActionResult> Details(int id)
        {
            var result = await _api.GetEventAsync(id);
            if (!result.Success || result.Data == null)
            {
                TempData["Error"] = result.Error ?? "Event not found.";
                return RedirectToAction(nameof(Index));
            }

            var enrolledCategoryIds = new HashSet<int>();
            var enrolledEvent = false;
            if (IsParticipant)
            {
                var mine = await _api.GetMyEnrolmentsAsync();
                foreach (var e in mine.Data ?? new List<MyEnrolmentDto>())
                {
                    if (e.EventID == id) enrolledEvent = true;
                }
                // We only know event-level enrolment from the API; category names are unique per event.
                foreach (var c in result.Data.Categories)
                {
                    if ((mine.Data ?? new List<MyEnrolmentDto>()).Any(m => m.EventID == id && m.CategoryName == c.CategoryName))
                        enrolledCategoryIds.Add(c.CategoryID);
                }
            }
            ViewBag.EnrolledCategoryIds = enrolledCategoryIds;
            ViewBag.EnrolledEvent = enrolledEvent;
            return View(result.Data);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [SessionAuth("Participant")]
        public async Task<IActionResult> Enrol(int categoryId, int eventId)
        {
            var result = await _api.EnrolAsync(categoryId);
            if (result.Success)
                TempData["Success"] = "Your enrolment has been recorded. Good luck!";
            else
                TempData["Error"] = result.Error ?? "Could not enrol in this category.";

            return RedirectToAction(nameof(Details), new { id = eventId });
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error() => View();
    }
}
