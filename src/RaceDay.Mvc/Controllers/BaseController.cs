using Microsoft.AspNetCore.Mvc;
using RaceDay.Mvc.Models;
using RaceDay.Mvc.Services;

namespace RaceDay.Mvc.Controllers
{
    /// <summary>
    /// Shared base for all controllers. Exposes the session-backed current user and
    /// centralises how the authenticated state is written to / removed from the session.
    /// </summary>
    public abstract class BaseController : Controller
    {
        protected bool IsAuthenticated => !string.IsNullOrEmpty(HttpContext.Session.GetString(SessionKeys.Token));
        protected string CurrentRole => HttpContext.Session.GetString(SessionKeys.Role) ?? string.Empty;
        protected string CurrentUserName => HttpContext.Session.GetString(SessionKeys.FullName) ?? "Guest";
        protected string CurrentEmail => HttpContext.Session.GetString(SessionKeys.Email) ?? string.Empty;
        protected string? CurrentProfilePicture => HttpContext.Session.GetString(SessionKeys.ProfilePicture);
        protected int CurrentUserId => int.TryParse(HttpContext.Session.GetString(SessionKeys.UserId), out var id) ? id : 0;
        protected bool IsOrganiser => CurrentRole == "Organiser";
        protected bool IsParticipant => CurrentRole == "Participant";

        protected void SetUserSession(AuthResponseDto auth)
        {
            HttpContext.Session.SetString(SessionKeys.Token, auth.Token);
            HttpContext.Session.SetString(SessionKeys.UserId, auth.UserId.ToString());
            HttpContext.Session.SetString(SessionKeys.FullName, auth.FullName);
            HttpContext.Session.SetString(SessionKeys.Email, auth.Email ?? string.Empty);
            HttpContext.Session.SetString(SessionKeys.Role, auth.Role);
            if (!string.IsNullOrEmpty(auth.ProfilePictureUrl))
                HttpContext.Session.SetString(SessionKeys.ProfilePicture, auth.ProfilePictureUrl);
        }

        protected void ClearUserSession() => HttpContext.Session.Clear();

        protected string HomeForRole() => IsOrganiser ? "/Organiser/Dashboard" : "/Home/Index";
    }
}
