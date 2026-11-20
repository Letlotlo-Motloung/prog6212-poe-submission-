using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Models;
using System.Security.Claims;

namespace RaceDay.Api.Controllers
{
    [Route("api/events")]
    [ApiController]
    public class EventsController : ControllerBase
    {
        private readonly RaceDayDbContext _context;
        public EventsController(RaceDayDbContext context) => _context = context;

        private int GetUserId()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(userIdClaim, out int id) ? id : 0;
        }

        /// <summary>List/browse events with optional filtering.</summary>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllEvents(
            [FromQuery] string? search,
            [FromQuery] string? type,
            [FromQuery] string? location,
            [FromQuery] bool upcomingOnly = false,
            [FromQuery] int? organiserId = null)
        {
            var query = _context.Events.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(e => e.EventName.Contains(search) || e.Location.Contains(search) || (e.Description != null && e.Description.Contains(search)));
            if (!string.IsNullOrWhiteSpace(type) && type != "All")
                query = query.Where(e => e.EventType == type);
            if (!string.IsNullOrWhiteSpace(location) && location != "All")
                query = query.Where(e => e.Location == location);
            if (upcomingOnly)
                query = query.Where(e => e.EventDate >= DateTime.Today);
            if (organiserId.HasValue)
                query = query.Where(e => e.OrganiserID == organiserId.Value);

            var events = await query
                .OrderBy(e => e.EventDate)
                .Select(e => new EventDto
                {
                    EventID = e.EventID,
                    EventName = e.EventName,
                    EventDate = e.EventDate,
                    Location = e.Location,
                    DistanceKM = e.DistanceKM,
                    EventType = e.EventType,
                    RouteInfo = e.RouteInfo,
                    Description = e.Description,
                    BannerUrl = e.BannerUrl,
                    OrganiserName = e.Organiser != null ? e.Organiser.FullName : null,
                    CategoryCount = e.Categories != null ? e.Categories.Count : 0,
                    TotalEnrolments = e.Categories != null
                        ? e.Categories.SelectMany(c => c.Enrolments!).Count()
                        : 0,
                    IsPast = e.EventDate < DateTime.Today
                })
                .ToListAsync();

            return Ok(events);
        }

        /// <summary>Get full details of one event, including categories and enrolment counts.</summary>
        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetEvent(int id)
        {
            var ev = await _context.Events
                .AsNoTracking()
                .Include(e => e.Organiser)
                .Include(e => e.Categories).ThenInclude(c => c.Enrolments!)
                .FirstOrDefaultAsync(e => e.EventID == id);

            if (ev == null) return NotFound("Event not found.");

            var result = new
            {
                ev.EventID,
                ev.OrganiserID,
                OrganiserName = ev.Organiser?.FullName,
                ev.EventName,
                ev.EventDate,
                ev.Location,
                ev.DistanceKM,
                ev.EventType,
                ev.RouteInfo,
                ev.Description,
                ev.BannerUrl,
                ev.CreatedAt,
                IsPast = ev.EventDate < DateTime.Today,
                TotalEnrolments = ev.Categories?.SelectMany(c => c.Enrolments ?? new List<Enrolment>()).Count() ?? 0,
                Categories = ev.Categories?.Select(c => new
                {
                    c.CategoryID,
                    c.EventID,
                    c.CategoryName,
                    c.CategoryType,
                    c.DistanceKM,
                    c.MaxParticipants,
                    c.EntryFee,
                    EnrolmentCount = c.Enrolments?.Count ?? 0
                }).ToList()
            };
            return Ok(result);
        }

        /// <summary>List events created by the logged-in organiser.</summary>
        [HttpGet("mine")]
        [Authorize(Roles = "Organiser")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMyEvents()
        {
            var organiserId = GetUserId();
            var events = await _context.Events
                .AsNoTracking()
                .Where(e => e.OrganiserID == organiserId)
                .OrderBy(e => e.EventDate)
                .Select(e => new OrganiserEventSummaryDto
                {
                    EventID = e.EventID,
                    EventName = e.EventName,
                    EventDate = e.EventDate,
                    Location = e.Location,
                    EventType = e.EventType,
                    BannerUrl = e.BannerUrl,
                    CategoryCount = e.Categories != null ? e.Categories.Count : 0,
                    TotalEnrolments = e.Categories != null ? e.Categories.SelectMany(c => c.Enrolments!).Count() : 0,
                    ResultsCaptured = e.Categories != null
                        ? e.Categories.SelectMany(c => c.Enrolments!).Count(en => en.Result != null)
                        : 0,
                    IsPast = e.EventDate < DateTime.Today
                })
                .ToListAsync();
            return Ok(events);
        }

        /// <summary>Organiser dashboard summary: totals, enrolment counts per event, upcoming dates.</summary>
        [HttpGet("dashboard")]
        [Authorize(Roles = "Organiser")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetDashboard()
        {
            var organiserId = GetUserId();
            var events = await _context.Events
                .AsNoTracking()
                .Where(e => e.OrganiserID == organiserId)
                .OrderBy(e => e.EventDate)
                .Select(e => new OrganiserEventSummaryDto
                {
                    EventID = e.EventID,
                    EventName = e.EventName,
                    EventDate = e.EventDate,
                    Location = e.Location,
                    EventType = e.EventType,
                    BannerUrl = e.BannerUrl,
                    CategoryCount = e.Categories != null ? e.Categories.Count : 0,
                    TotalEnrolments = e.Categories != null ? e.Categories.SelectMany(c => c.Enrolments!).Count() : 0,
                    ResultsCaptured = e.Categories != null
                        ? e.Categories.SelectMany(c => c.Enrolments!).Count(en => en.Result != null)
                        : 0,
                    IsPast = e.EventDate < DateTime.Today
                })
                .ToListAsync();

            var participantIds = await _context.Enrolments
                .AsNoTracking()
                .Where(en => en.Category!.Event!.OrganiserID == organiserId)
                .Select(en => en.ParticipantID)
                .Distinct()
                .CountAsync();

            var published = await _context.Results
                .AsNoTracking()
                .CountAsync(r => r.IsPublished && r.Enrolment!.Category!.Event!.OrganiserID == organiserId);

            var dashboard = new OrganiserDashboardDto
            {
                TotalEvents = events.Count,
                UpcomingEvents = events.Count(e => !e.IsPast),
                TotalEnrolments = events.Sum(e => e.TotalEnrolments),
                TotalParticipants = participantIds,
                ResultsPublished = published,
                Events = events,
                Upcoming = events.Where(e => !e.IsPast).Take(5).ToList()
            };
            return Ok(dashboard);
        }

        /// <summary>Create a new event.</summary>
        [HttpPost]
        [Authorize(Roles = "Organiser")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> CreateEvent([FromBody] EventDto dto)
        {
            var ev = new Event
            {
                OrganiserID = GetUserId(),
                EventName = dto.EventName,
                EventDate = dto.EventDate,
                Location = dto.Location,
                DistanceKM = dto.DistanceKM,
                EventType = dto.EventType,
                RouteInfo = dto.RouteInfo,
                Description = dto.Description,
                BannerUrl = dto.BannerUrl
            };
            _context.Events.Add(ev);
            await _context.SaveChangesAsync();
            return Ok(ev);
        }

        /// <summary>Update an existing event you organise.</summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Organiser")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateEvent(int id, [FromBody] EventDto dto)
        {
            var ev = await _context.Events.FindAsync(id);
            if (ev == null) return NotFound("Event not found.");
            if (ev.OrganiserID != GetUserId()) return Forbid();

            ev.EventName = dto.EventName;
            ev.EventDate = dto.EventDate;
            ev.Location = dto.Location;
            ev.DistanceKM = dto.DistanceKM;
            ev.EventType = dto.EventType;
            ev.RouteInfo = dto.RouteInfo;
            ev.Description = dto.Description;
            ev.BannerUrl = dto.BannerUrl;

            await _context.SaveChangesAsync();
            return Ok(ev);
        }

        /// <summary>Delete an event you organise.</summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Organiser")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteEvent(int id)
        {
            var ev = await _context.Events.FindAsync(id);
            if (ev == null) return NotFound();
            if (ev.OrganiserID != GetUserId()) return Forbid();
            _context.Events.Remove(ev);
            await _context.SaveChangesAsync();
            return Ok("Event deleted.");
        }

        /// <summary>Define an age or distance category for a specific event.</summary>
        [HttpPost("{eventId}/categories")]
        [Authorize(Roles = "Organiser")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> CreateCategory(int eventId, [FromBody] CategoryDto dto)
        {
            var ev = await _context.Events.FindAsync(eventId);
            if (ev == null || ev.OrganiserID != GetUserId()) return Forbid();

            var category = new Category
            {
                EventID = eventId,
                CategoryName = dto.CategoryName,
                CategoryType = string.IsNullOrWhiteSpace(dto.CategoryType) ? "Distance" : dto.CategoryType,
                DistanceKM = dto.DistanceKM,
                MaxParticipants = dto.MaxParticipants,
                EntryFee = dto.EntryFee
            };
            _context.Categories.Add(category);
            await _context.SaveChangesAsync();
            return Ok(category);
        }

        /// <summary>Update a category belonging to one of your events.</summary>
        [HttpPut("{eventId}/categories/{categoryId}")]
        [Authorize(Roles = "Organiser")]
        public async Task<IActionResult> UpdateCategory(int eventId, int categoryId, [FromBody] CategoryDto dto)
        {
            var category = await _context.Categories.Include(c => c.Event).FirstOrDefaultAsync(c => c.CategoryID == categoryId && c.EventID == eventId);
            if (category == null) return NotFound();
            if (category.Event!.OrganiserID != GetUserId()) return Forbid();

            category.CategoryName = dto.CategoryName;
            category.CategoryType = string.IsNullOrWhiteSpace(dto.CategoryType) ? "Distance" : dto.CategoryType;
            category.DistanceKM = dto.DistanceKM;
            category.MaxParticipants = dto.MaxParticipants;
            category.EntryFee = dto.EntryFee;
            await _context.SaveChangesAsync();
            return Ok(category);
        }

        /// <summary>Delete a category belonging to one of your events.</summary>
        [HttpDelete("{eventId}/categories/{categoryId}")]
        [Authorize(Roles = "Organiser")]
        public async Task<IActionResult> DeleteCategory(int eventId, int categoryId)
        {
            var category = await _context.Categories.Include(c => c.Event).FirstOrDefaultAsync(c => c.CategoryID == categoryId && c.EventID == eventId);
            if (category == null) return NotFound();
            if (category.Event!.OrganiserID != GetUserId()) return Forbid();
            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();
            return Ok("Category deleted.");
        }

        /// <summary>View all participants enrolled for a specific event.</summary>
        [HttpGet("{id}/enrolments")]
        [Authorize(Roles = "Organiser")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetEventEnrolments(int id)
        {
            var ev = await _context.Events.FindAsync(id);
            if (ev == null) return NotFound("Event not found.");
            if (ev.OrganiserID != GetUserId()) return Forbid();

            var enrolments = await _context.Enrolments
                .AsNoTracking()
                .Where(en => en.Category!.EventID == id)
                .OrderBy(en => en.Category!.CategoryName).ThenBy(en => en.Participant!.FullName)
                .Select(en => new EventEnrolmentDto
                {
                    EnrolmentID = en.EnrolmentID,
                    ParticipantID = en.ParticipantID,
                    ParticipantName = en.Participant!.FullName,
                    Email = en.Participant.Email,
                    PhoneNumber = en.Participant.PhoneNumber,
                    CategoryName = en.Category!.CategoryName,
                    CategoryType = en.Category.CategoryType,
                    Status = en.Status,
                    EnrolmentDate = en.EnrolmentDate,
                    HasResult = en.Result != null,
                    ResultID = en.Result != null ? en.Result.ResultID : null,
                    FinishTime = en.Result != null && en.Result.FinishTime != null ? en.Result.FinishTime.Value.ToString(@"hh\:mm\:ss") : null,
                    Position = en.Result != null ? en.Result.Position : null,
                    IsPublished = en.Result != null && en.Result.IsPublished
                })
                .ToListAsync();

            return Ok(enrolments);
        }

        /// <summary>View all results captured for an event.</summary>
        [HttpGet("{id}/results")]
        [Authorize(Roles = "Organiser")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetEventResults(int id)
        {
            var ev = await _context.Events.FindAsync(id);
            if (ev == null) return NotFound("Event not found.");
            if (ev.OrganiserID != GetUserId()) return Forbid();

            var results = await _context.Enrolments
                .AsNoTracking()
                .Where(en => en.Category!.EventID == id)
                .OrderBy(en => en.Category!.CategoryName).ThenBy(en => en.Participant!.FullName)
                .Select(en => new EventEnrolmentDto
                {
                    EnrolmentID = en.EnrolmentID,
                    ParticipantID = en.ParticipantID,
                    ParticipantName = en.Participant!.FullName,
                    Email = en.Participant.Email,
                    PhoneNumber = en.Participant.PhoneNumber,
                    CategoryName = en.Category!.CategoryName,
                    CategoryType = en.Category.CategoryType,
                    Status = en.Status,
                    EnrolmentDate = en.EnrolmentDate,
                    HasResult = en.Result != null,
                    ResultID = en.Result != null ? en.Result.ResultID : null,
                    FinishTime = en.Result != null && en.Result.FinishTime != null ? en.Result.FinishTime.Value.ToString(@"hh\:mm\:ss") : null,
                    Position = en.Result != null ? en.Result.Position : null,
                    IsPublished = en.Result != null && en.Result.IsPublished
                })
                .ToListAsync();

            return Ok(results);
        }

        /// <summary>Publish all captured results for an event so participants can see them.</summary>
        [HttpPost("{id}/publish-results")]
        [Authorize(Roles = "Organiser")]
        public async Task<IActionResult> PublishResults(int id)
        {
            var ev = await _context.Events.FindAsync(id);
            if (ev == null) return NotFound("Event not found.");
            if (ev.OrganiserID != GetUserId()) return Forbid();

            var results = await _context.Results
                .Include(r => r.Enrolment).ThenInclude(e => e!.Category)
                .Where(r => r.Enrolment!.Category!.EventID == id)
                .ToListAsync();

            foreach (var r in results)
            {
                r.IsPublished = true;
                r.PublishedAt = DateTime.Now;
            }
            await _context.SaveChangesAsync();
            return Ok(new { published = results.Count });
        }
    }
}
