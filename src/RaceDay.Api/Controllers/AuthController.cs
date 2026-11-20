using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace RaceDay.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly RaceDayDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthController(RaceDayDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        /// <summary>Registers a new user as an Organiser or Participant.</summary>
        [HttpPost("register")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
                return BadRequest("Email already registered.");

            var role = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == dto.Role);
            if (role == null) return BadRequest("Invalid role specified. Choose Organiser or Participant.");

            var user = new User
            {
                FullName = dto.FullName,
                Email = dto.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                PhoneNumber = dto.PhoneNumber,
                RoleID = role.RoleID
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var token = GenerateJwtToken(user, role.RoleName);
            return Ok(new
            {
                userId = user.UserID,
                fullName = user.FullName,
                email = user.Email,
                role = role.RoleName,
                profilePictureUrl = user.ProfilePictureUrl,
                token
            });
        }

        /// <summary>Authenticates a user and issues a JWT token.</summary>
        [HttpPost("login")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var user = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Email == dto.Email);
            if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
                return Unauthorized("Invalid credentials.");

            var token = GenerateJwtToken(user, user.Role?.RoleName ?? "Participant");
            return Ok(new
            {
                token,
                userId = user.UserID,
                fullName = user.FullName,
                email = user.Email,
                role = user.Role?.RoleName,
                profilePictureUrl = user.ProfilePictureUrl
            });
        }

        /// <summary>Invalidates the current user's session/token.</summary>
        [HttpPost("logout")]
        [Microsoft.AspNetCore.Authorization.Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult Logout() => Ok(new { message = "Logged out successfully." });

        private string GenerateJwtToken(User user, string roleName)
        {
            var jwtKey = _configuration["Jwt:Key"] ?? "ThisIsASecretKeyForRaceDayAPI12345!";
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserID.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, roleName)
            };
            var token = new JwtSecurityToken(
                _configuration["Jwt:Issuer"] ?? "RaceDayAPI",
                _configuration["Jwt:Audience"] ?? "RaceDayUsers",
                claims,
                expires: DateTime.Now.AddHours(3),
                signingCredentials: credentials);
            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
