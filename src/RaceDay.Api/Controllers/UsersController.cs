using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using System.Security.Claims;

namespace RaceDay.Api.Controllers
{
    [Route("api/users")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly RaceDayDbContext _context;
        public UsersController(RaceDayDbContext context) => _context = context;

        private int GetUserId()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(userIdClaim, out int id) ? id : 0;
        }

        /// <summary>Get the logged-in user's own profile details.</summary>
        [HttpGet("me")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetMyProfile()
        {
            var user = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.UserID == GetUserId());
            if (user == null) return NotFound();
            return Ok(new ProfileDto
            {
                UserID = user.UserID,
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Role = user.Role?.RoleName ?? string.Empty,
                ProfilePictureUrl = user.ProfilePictureUrl,
                CreatedAt = user.CreatedAt
            });
        }

        /// <summary>Update the logged-in user's profile information.</summary>
        [HttpPut("me")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto)
        {
            var user = await _context.Users.FindAsync(GetUserId());
            if (user == null) return NotFound();

            user.FullName = dto.FullName;
            user.PhoneNumber = dto.PhoneNumber;
            if (dto.ProfilePictureUrl != null) user.ProfilePictureUrl = dto.ProfilePictureUrl;
            await _context.SaveChangesAsync();
            return Ok(new { user.UserID, user.FullName, user.Email, user.PhoneNumber, user.ProfilePictureUrl });
        }

        /// <summary>Change the logged-in user's password.</summary>
        [HttpPut("me/password")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ChangePassword([FromBody] UpdatePasswordDto dto)
        {
            var user = await _context.Users.FindAsync(GetUserId());
            if (user == null) return NotFound();

            if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.PasswordHash))
                return BadRequest("Current password is incorrect.");

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Password updated successfully." });
        }

        /// <summary>View a limited public profile of a user.</summary>
        [HttpGet("{id}")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetPublicProfile(int id)
        {
            var user = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.UserID == id);
            if (user == null) return NotFound("User not found.");
            return Ok(new { user.UserID, user.FullName, role = user.Role?.RoleName, user.ProfilePictureUrl });
        }
    }
}
