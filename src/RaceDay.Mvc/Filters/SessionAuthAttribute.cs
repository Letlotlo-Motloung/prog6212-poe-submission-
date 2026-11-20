using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using RaceDay.Mvc.Services;

namespace RaceDay.Mvc.Filters
{
    /// <summary>
    /// Guards protected pages using the session. Optionally restricts access to one or more roles.
    /// Unauthenticated users are redirected to the login page; authenticated users with the wrong
    /// role are redirected to the access-denied page.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public class SessionAuthAttribute : Attribute, IAsyncActionFilter
    {
        private readonly string[] _roles;

        public SessionAuthAttribute(params string[] roles) => _roles = roles;

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var session = context.HttpContext.Session;
            var token = session.GetString(SessionKeys.Token);

            if (string.IsNullOrEmpty(token))
            {
                var returnUrl = context.HttpContext.Request.Path + context.HttpContext.Request.QueryString;
                context.Result = new RedirectResult("/Account/Login?returnUrl=" + Uri.EscapeDataString(returnUrl));
                return;
            }

            if (_roles.Length > 0)
            {
                var role = session.GetString(SessionKeys.Role);
                if (!_roles.Contains(role))
                {
                    context.Result = new RedirectResult("/Account/AccessDenied");
                    return;
                }
            }

            await next();
        }
    }
}
