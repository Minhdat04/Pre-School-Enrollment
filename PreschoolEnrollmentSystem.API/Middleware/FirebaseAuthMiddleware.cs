using FirebaseAdmin.Auth;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace PreschoolEnrollmentSystem.API.Middleware
{
    /// <summary>
    /// Middleware to authenticate requests using Firebase ID tokens
    /// Why: Validates Firebase tokens and adds user info to HttpContext for use in controllers
    /// </summary>
    public class FirebaseAuthMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<FirebaseAuthMiddleware> _logger;

        // Why: List of paths that don't require authentication (public endpoints)
        private readonly string[] _publicPaths = new[]
        {
            "/api/auth/login",
            "/api/auth/register",
            "/api/auth/reset-password",
            "/swagger",
            "/health"
        };

        public FirebaseAuthMiddleware(RequestDelegate next, ILogger<FirebaseAuthMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Why: Skip authentication for public endpoints
            // Some endpoints like login/register don't need authentication
            if (IsPublicPath(context.Request.Path))
            {
                await _next(context);
                return;
            }

            try
            {
                // Step 1: Extract the token from Authorization header
                // Why: Mobile app sends token as "Bearer <token>"
                var token = ExtractTokenFromHeader(context);

                if (string.IsNullOrEmpty(token))
                {
                    _logger.LogWarning("No authorization token found in request");
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    await context.Response.WriteAsJsonAsync(new
                    {
                        error = "Unauthorized",
                        message = "No authentication token provided"
                    });
                    return;
                }

                // Step 2: Verify the token with Firebase
                // Why: Firebase validates the token signature and expiration
                var decodedToken = await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(token);

                // Step 3: Extract user information from the decoded token
                // Why: We need user ID and other claims for authorization in controllers
                var userId = decodedToken.Uid;
                var email = decodedToken.Claims.ContainsKey("email")
                    ? decodedToken.Claims["email"].ToString()
                    : null;

                // Step 4: Add user information to HttpContext
                // Why: Controllers can access this via HttpContext.User
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, userId),
                    new Claim("firebase_uid", userId)
                };

                if (!string.IsNullOrEmpty(email))
                {
                    claims.Add(new Claim(ClaimTypes.Email, email));
                }

                // Why: Add custom claims (like role) from Firebase
                // You can set custom claims in Firebase like: { role: "parent" }
                if (decodedToken.Claims.ContainsKey("role"))
                {
                    var role = decodedToken.Claims["role"].ToString();
                    claims.Add(new Claim(ClaimTypes.Role, role));
                }

                var identity = new ClaimsIdentity(claims, "Firebase");
                context.User = new ClaimsPrincipal(identity);

                _logger.LogInformation("User {UserId} authenticated successfully", userId);

                // Step 5: Continue to the next middleware/controller
                await _next(context);
            }
            catch (FirebaseAuthException ex)
            {
                // Why: Handle Firebase-specific errors (expired token, invalid token, etc.)
                _logger.LogError(ex, "Firebase authentication failed");

                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new
                {
                    error = "Unauthorized",
                    message = "Invalid or expired token",
                    details = ex.Message
                });
            }
            catch (Exception ex)
            {
                // Why: Catch any other unexpected errors
                _logger.LogError(ex, "Unexpected error during authentication");

                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                await context.Response.WriteAsJsonAsync(new
                {
                    error = "Internal Server Error",
                    message = "An error occurred during authentication"
                });
            }
        }

        /// <summary>
        /// Extract JWT token from Authorization header
        /// Why: Token format is "Bearer <token>", we need just the token part
        /// </summary>
        private string ExtractTokenFromHeader(HttpContext context)
        {
            var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();

            if (string.IsNullOrEmpty(authHeader))
                return null;

            // Why: Check if it starts with "Bearer " and extract the actual token
            if (authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                return authHeader.Substring("Bearer ".Length).Trim();
            }

            return authHeader;
        }

        /// <summary>
        /// Check if the requested path is public (doesn't require authentication)
        /// Why: Login, register, and Swagger docs should be accessible without a token
        /// </summary>
        private bool IsPublicPath(PathString path)
        {
            return _publicPaths.Any(p => path.StartsWithSegments(p, StringComparison.OrdinalIgnoreCase));
        }
    }
}