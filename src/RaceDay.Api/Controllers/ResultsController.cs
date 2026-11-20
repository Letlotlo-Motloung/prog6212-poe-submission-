using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using System.Security.Claims;

namespace RaceDay.Api.Controllers
{
    [Route("api")]
    [ApiController]
    public class ResultsController : ControllerBase
    {
        private readonly RaceDayDbContext _context;
        public ResultsController(RaceDayDbContext context) => _context = context;

        private int GetUserId()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(userIdClaim, out int id) ? id : 0;
        }

        /// <summary>Update a previously captured result.</summary>
        [HttpPut("results/{id}")]
        [Authorize(Roles = "Organiser")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> UpdateResult(int id, [FromBody] ResultDto dto)
        {
            var result = await _context.Results
                .Include(r => r.Enrolment).ThenInclude(e => e!.Category).ThenInclude(c => c!.Event)
                .FirstOrDefaultAsync(r => r.ResultID == id);

            if (result == null || result.Enrolment!.Category!.Event!.OrganiserID != GetUserId()) return Forbid();

            result.FinishTime = dto.FinishTime;
            result.Position = dto.Position;
            result.Status = dto.Status;
            if (dto.IsPublished && !result.IsPublished)
            {
                result.IsPublished = true;
                result.PublishedAt = DateTime.Now;
            }

            await _context.SaveChangesAsync();
            return Ok(result);
        }

        /// <summary>View the logged-in participant's full personal performance history.</summary>
        [HttpGet("users/me/results")]
        [Authorize(Roles = "Participant")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMyResults()
        {
            var participantId = GetUserId();

            var results = await _context.Results
                .AsNoTracking()
                .Where(r => r.IsPublished && r.Enrolment!.ParticipantID == participantId)
                .OrderByDescending(r => r.Enrolment!.Category!.Event!.EventDate)
                .Select(r => new MyResultDto
                {
                    EventName = r.Enrolment!.Category!.Event!.EventName,
                    EventDate = r.Enrolment.Category.Event.EventDate,
                    Location = r.Enrolment.Category.Event.Location,
                    CategoryName = r.Enrolment.Category.CategoryName,
                    FinishTime = r.FinishTime != null ? r.FinishTime.Value.ToString(@"hh\:mm\:ss") : null,
                    Position = r.Position,
                    Status = r.Status,
                    TotalFinishers = _context.Results.Count(x =>
                        x.IsPublished &&
                        x.Enrolment!.Category!.EventID == r.Enrolment.Category.EventID &&
                        x.Enrolment.CategoryID == r.Enrolment.CategoryID)
                })
                .ToListAsync();

            return Ok(results);
        }
    }
}
