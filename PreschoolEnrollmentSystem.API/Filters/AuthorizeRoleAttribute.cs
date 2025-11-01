using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace PreschoolEnrollmentSystem.API.Filters
{
    /// <summary>
    /// Custom authorization attribute to restrict access based on user roles
    /// Why: Different endpoints need different access levels (Parent, Staff, Admin)
    /// 
    /// Usage in controllers:
    /// [AuthorizeRole("Admin")]
    /// [AuthorizeRole("Parent", "Staff")]
    /// </summary>
    public class AuthorizeRoleAttribute : Attribute, IAuthorizationFilter
    {
        private readonly string[] _allowedRoles;

        /// <summary>
        /// Constructor that accepts one or more allowed roles
        /// </summary>
        /// <param name="roles">Roles that are allowed to access the endpoint</param>
        public AuthorizeRoleAttribute(params string[] roles)
        {
            _allowedRoles = roles ?? throw new ArgumentNullException(nameof(roles));
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            // Why: Check if user is authenticated first
            if (context.HttpContext.User?.Identity?.IsAuthenticated != true)
            {
                context.Result = new UnauthorizedObjectResult(new
                {
                    error = "Unauthorized",
                    message = "You must be logged in to access this resource"
                });
                return;
            }

            // Why: Extract user's role from claims (set by our middleware)
            var userRole = context.HttpContext.User.FindFirst(ClaimTypes.Role)?.Value;

            if (string.IsNullOrEmpty(userRole))
            {
                // Why: If no role claim exists, user might not have completed registration
                context.Result = new ForbiddenObjectResult(new
                {
                    error = "Forbidden",
                    message = "User role not assigned. Please complete your registration."
                });
                return;
            }

            // Why: Check if user's role is in the list of allowed roles
            if (!_allowedRoles.Contains(userRole, StringComparer.OrdinalIgnoreCase))
            {
                context.Result = new ForbiddenObjectResult(new
                {
                    error = "Forbidden",
                    message = $"This resource requires one of the following roles: {string.Join(", ", _allowedRoles)}"
                });
                return;
            }

            // Why: User has the correct role, allow access to continue
        }
    }

    /// <summary>
    /// Custom result for 403 Forbidden responses
    /// Why: Distinguish between 401 (not authenticated) and 403 (authenticated but not authorized)
    /// </summary>
    public class ForbiddenObjectResult : ObjectResult
    {
        public ForbiddenObjectResult(object value) : base(value)
        {
            StatusCode = StatusCodes.Status403Forbidden;
        }
    }
}