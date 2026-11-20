using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Models;
using System.Security.Claims;

namespace RaceDay.Api.Controllers
{
    [Route("api")]
    [ApiController]
    public class EnrolmentsController : ControllerBase
    {
        private readonly RaceDayDbContext _context;
        public EnrolmentsController(RaceDayDbContext context) => _context = context;

        private int GetUserId()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(userIdClaim, out int id) ? id : 0;
        }

        /// <summary>View all available categories for a specific event.</summary>
        [HttpGet("events/{eventId}/categories")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetCategories(int eventId)
        {
            var categories = await _context.Categories
                .AsNoTracking()
                .Where(c => c.EventID == eventId)
                .Select(c => new CategoryDto
                {
                    CategoryID = c.CategoryID,
                    CategoryName = c.CategoryName,
                    CategoryType = c.CategoryType,
                    DistanceKM = c.DistanceKM,
                    MaxParticipants = c.MaxParticipants,
                    EntryFee = c.EntryFee,
                    EnrolmentCount = c.Enrolments != null ? c.Enrolments.Count : 0
                })
                .ToListAsync();
            return Ok(categories);
        }

        /// <summary>Enrol the logged-in participant into a specific category.</summary>
        [HttpPost("categories/{categoryId}/enrolments")]
        [Authorize(Roles = "Participant")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> Enrol(int categoryId)
        {
            var category = await _context.Categories.FindAsync(categoryId);
            if (category == null) return NotFound("Category not found.");

            var participantId = GetUserId();
            if (await _context.Enrolments.AnyAsync(e => e.ParticipantID == participantId && e.CategoryID == categoryId))
                return BadRequest("You are already enrolled in this category.");

            var count = await _context.Enrolments.CountAsync(e => e.CategoryID == categoryId);
            if (count >= category.MaxParticipants) return BadRequest("This category is full.");

            var enrolment = new Enrolment { ParticipantID = participantId, CategoryID = categoryId, Status = "Confirmed" };
            _context.Enrolments.Add(enrolment);
            await _context.SaveChangesAsync();
            return Ok(new { enrolment.EnrolmentID, enrolment.CategoryID, enrolment.Status, enrolment.EnrolmentDate });
        }

        /// <summary>List the logged-in participant's own enrolments.</summary>
        [HttpGet("users/me/enrolments")]
        [Authorize(Roles = "Participant")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMyEnrolments()
        {
            var participantId = GetUserId();
            var enrolments = await _context.Enrolments
                .AsNoTracking()
                .Where(e => e.ParticipantID == participantId)
                .OrderBy(e => e.Category!.Event!.EventDate)
                .Select(e => new MyEnrolmentDto
                {
                    EnrolmentID = e.EnrolmentID,
                    EventID = e.Category!.EventID,
                    EventName = e.Category.Event!.EventName,
                    EventDate = e.Category.Event.EventDate,
                    Location = e.Category.Event.Location,
                    CategoryName = e.Category.CategoryName,
                    CategoryType = e.Category.CategoryType,
                    Status = e.Status,
                    EnrolmentDate = e.EnrolmentDate,
                    HasResult = e.Result != null
                })
                .ToListAsync();
            return Ok(enrolments);
        }

        /// <summary>Cancel an enrolment.</summary>
        [HttpDelete("enrolments/{id}")]
        [Authorize(Roles = "Participant")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> CancelEnrolment(int id)
        {
            var enrolment = await _context.Enrolments.FindAsync(id);
            if (enrolment == null || enrolment.ParticipantID != GetUserId()) return Forbid();
            _context.Enrolments.Remove(enrolment);
            await _context.SaveChangesAsync();
            return Ok("Enrolment cancelled.");
        }

        /// <summary>Capture a result for a participant's enrolment (Organiser only).</summary>
        [HttpPost("enrolments/{id}/results")]
        [Authorize(Roles = "Organiser")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> CaptureResult(int id, [FromBody] ResultDto dto)
        {
            var enrolment = await _context.Enrolments
                .Include(e => e.Category).ThenInclude(c => c!.Event)
                .FirstOrDefaultAsync(e => e.EnrolmentID == id);
            if (enrolment == null || enrolment.Category!.Event!.OrganiserID != GetUserId()) return Forbid();

            var existing = await _context.Results.FirstOrDefaultAsync(r => r.EnrolmentID == id);
            if (existing != null) return BadRequest("A result already exists for this enrolment. Use update instead.");

            var result = new Result
            {
                EnrolmentID = id,
                FinishTime = dto.FinishTime,
                Position = dto.Position,
                Status = dto.Status,
                IsPublished = dto.IsPublished,
                PublishedAt = dto.IsPublished ? DateTime.Now : null
            };
            _context.Results.Add(result);
            await _context.SaveChangesAsync();
            return Ok(result);
        }
    }
}
