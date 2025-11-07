using FirebaseAdmin.Auth;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using System.Text;
using PreschoolEnrollmentSystem.Infrastructure.Repositories.Interfaces;

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
        private readonly IServiceProvider _serviceProvider;

        // Why: List of paths that don't require authentication (public endpoints)
        private readonly string[] _publicPaths = new[]
        {
            "/api/auth/login",
            "/api/auth/register",
            "/api/auth/reset-password",
            "/api/seed",
            "/swagger",
            "/health"
        };

        public FirebaseAuthMiddleware(RequestDelegate next, ILogger<FirebaseAuthMiddleware> logger, IServiceProvider serviceProvider)
        {
            _next = next;
            _logger = logger;
            _serviceProvider = serviceProvider;
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

                // Check if this is a seed user token (Base64 encoded email:timestamp)
                if (IsSeedUserToken(token))
                {
                    var email = ExtractEmailFromSeedToken(token);
                    if (!string.IsNullOrEmpty(email))
                    {
                        using var scope = _serviceProvider.CreateScope();
                        var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
                        var user = await userRepository.GetUserByEmailAsync(email);

                        if (user != null && user.FirebaseUid.StartsWith("seed_"))
                        {
                            // Create claims for seed user
                            var claims = new List<Claim>
                            {
                                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                                new Claim("firebase_uid", user.FirebaseUid),
                                new Claim(ClaimTypes.Email, user.Email),
                                new Claim(ClaimTypes.Role, user.Role.ToString()),
                                new Claim(ClaimTypes.Name, $"{user.FirstName} {user.LastName}")
                            };

                            // IMPORTANT: Use an authentication type to mark identity as authenticated
                            var identity = new ClaimsIdentity(claims, "Bearer");
                            context.User = new ClaimsPrincipal(identity);

                            _logger.LogInformation("Seed user {Email} authenticated successfully with role {Role}", email, user.Role);
                            await _next(context);
                            return;
                        }
                    }
                }

                // Step 2: Verify the token with Firebase
                // Why: Firebase validates the token signature and expiration
                var decodedToken = await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(token);

                // Step 3: Extract user information from the decoded token
                // Why: We need user ID and other claims for authorization in controllers
                var userId = decodedToken.Uid;
                var email2 = decodedToken.Claims.ContainsKey("email")
                    ? decodedToken.Claims["email"].ToString()
                    : null;

                // Step 4: Add user information to HttpContext
                // Why: Controllers can access this via HttpContext.User
                var firebaseClaims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, userId),
                    new Claim("firebase_uid", userId)
                };

                if (!string.IsNullOrEmpty(email2))
                {
                    firebaseClaims.Add(new Claim(ClaimTypes.Email, email2));
                }

                // Why: Add custom claims (like role) from Firebase
                // You can set custom claims in Firebase like: { role: "parent" }
                if (decodedToken.Claims.ContainsKey("role"))
                {
                    var role = decodedToken.Claims["role"].ToString();
                    firebaseClaims.Add(new Claim(ClaimTypes.Role, role));
                }

                var firebaseIdentity = new ClaimsIdentity(firebaseClaims, "Firebase");
                context.User = new ClaimsPrincipal(firebaseIdentity);

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

        /// <summary>
        /// Check if token is a seed user token (Base64 encoded, not a JWT)
        /// </summary>
        private bool IsSeedUserToken(string token)
        {
            // JWT tokens have 3 parts separated by dots
            // Seed tokens are Base64 encoded strings without dots
            return !token.Contains('.');
        }

        /// <summary>
        /// Extract email from seed user token (format: email:timestamp)
        /// </summary>
        private string ExtractEmailFromSeedToken(string token)
        {
            try
            {
                var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(token));
                var parts = decoded.Split(':');
                return parts.Length > 0 ? parts[0] : null;
            }
            catch
            {
                return null;
            }
        }
    }
}