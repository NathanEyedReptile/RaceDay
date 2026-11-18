using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace RaceDay.Mvc.Filters
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class SessionAuthorizeAttribute : Attribute, IAuthorizationFilter
    {
        private readonly string[] _allowedRoles;

        public SessionAuthorizeAttribute(params string[] allowedRoles)
        {
            _allowedRoles = allowedRoles;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var session = context.HttpContext.Session;

            // Redirect users who are not logged in.
            if (string.IsNullOrEmpty(session.GetString("AuthToken")))
            {
                context.Result = new RedirectToActionResult(
                    "Login", "Account", null);

                return;
            }

            // Check whether the logged-in user has the required role.
            if (_allowedRoles.Length > 0)
            {
                var userRole = session.GetString("UserRole");

                if (!_allowedRoles.Contains(
                    userRole, StringComparer.OrdinalIgnoreCase))
                {
                    context.Result = new StatusCodeResult(403);
                }
            }
        }
    }
}