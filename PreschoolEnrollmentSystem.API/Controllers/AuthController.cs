using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using PreschoolEnrollmentSystem.API.DTOs.Auth;
using PreschoolEnrollmentSystem.API.Filters;
using PreschoolEnrollmentSystem.Services.Interfaces;

namespace PreschoolEnrollmentSystem.API.Controllers
{
    /// Authentication Controller
    /// Routes: /api/auth/*
    /// Security: Public endpoints (no authentication required)
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            IAuthService authService,
            ILogger<AuthController> logger)
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #region Registration

        /// Register a new user account
        /// <param name="request">Registration request data</param>
        /// <returns>Login response with authentication tokens</returns>
        /// <response code="200">Registration successful, returns authentication tokens</response>
        /// <response code="400">Invalid request data or validation error</response>
        /// <response code="409">Email already exists</response>
        /// <response code="500">Internal server error</response>
        [HttpPost("register")]
        [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Register([FromBody] RegisterRequestDto request)
        {
            try
            {
                // Log registration attempt (without sensitive data)
                _logger.LogInformation("Registration attempt for email: {Email}, role: {Role}",
                    request.Email, request.Role);

                // Validate model state
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Registration validation failed for {Email}", request.Email);
                    return BadRequest(new ErrorResponse
                    {
                        Error = "ValidationError",
                        Message = "Invalid registration data",
                        Details = ModelState.ToErrorString(),
                        Timestamp = DateTime.UtcNow,
                        Path = HttpContext.Request.Path
                    });
                }

                // Perform registration
                var result = await _authService.RegisterAsync(request);

                _logger.LogInformation("Registration successful for {Email}", request.Email);

                return Ok(new
                {
                    success = true,
                    message = "Registration successful. Please check your email to verify your account.",
                    data = result
                });
            }
            catch (ArgumentException ex)
            {
                // Validation errors
                _logger.LogWarning(ex, "Registration validation error for {Email}", request.Email);
                return BadRequest(new ErrorResponse
                {
                    Error = "ValidationError",
                    Message = ex.Message,
                    Timestamp = DateTime.UtcNow,
                    Path = HttpContext.Request.Path
                });
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("already exists"))
            {
                // Email already exists
                _logger.LogWarning("Registration failed: Email already exists - {Email}", request.Email);
                return Conflict(new ErrorResponse
                {
                    Error = "EmailExists",
                    Message = "An account with this email address already exists.",
                    Timestamp = DateTime.UtcNow,
                    Path = HttpContext.Request.Path
                });
            }
            catch (Exception ex)
            {
                // Unexpected errors
                _logger.LogError(ex, "Registration error for {Email}", request.Email);
                return StatusCode(500, new ErrorResponse
                {
                    Error = "InternalServerError",
                    Message = "An error occurred during registration. Please try again later.",
                    Details = HttpContext.RequestServices.GetService<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>()?.IsDevelopment() == true
                        ? ex.Message : null,
                    Timestamp = DateTime.UtcNow,
                    Path = HttpContext.Request.Path
                });
            }
        }

        #endregion

        #region Login

        /// Authenticate user with email and password
        /// <param name="request">Login credentials</param>
        /// <returns>Login response with authentication tokens</returns>
        /// <response code="200">Login successful, returns authentication tokens</response>
        /// <response code="400">Invalid request data</response>
        /// <response code="401">Invalid credentials</response>
        /// <response code="403">Account inactive or locked</response>
        /// <response code="500">Internal server error</response>
        [HttpPost("login")]
        [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
        {
            try
            {
                // Log login attempt (without password)
                _logger.LogInformation("Login attempt for email: {Email}", request.Email);

                // Validate model state
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Login validation failed for {Email}", request.Email);
                    return BadRequest(new ErrorResponse
                    {
                        Error = "ValidationError",
                        Message = "Invalid login data",
                        Details = ModelState.ToErrorString(),
                        Timestamp = DateTime.UtcNow,
                        Path = HttpContext.Request.Path
                    });
                }

                // Perform login
                var result = await _authService.LoginAsync(request);

                _logger.LogInformation("Login successful for {Email}", request.Email);

                return Ok(new
                {
                    success = true,
                    message = "Login successful",
                    data = result
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                // Invalid credentials
                _logger.LogWarning("Login failed: Invalid credentials for {Email}", request.Email);
                return Unauthorized(new ErrorResponse
                {
                    Error = "InvalidCredentials",
                    Message = "Invalid email or password.",
                    Timestamp = DateTime.UtcNow,
                    Path = HttpContext.Request.Path
                });
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("inactive") || ex.Message.Contains("disabled"))
            {
                // Account inactive
                _logger.LogWarning("Login failed: Account inactive - {Email}", request.Email);
                return StatusCode(403, new ErrorResponse
                {
                    Error = "AccountInactive",
                    Message = "Your account is inactive. Please contact support.",
                    Timestamp = DateTime.UtcNow,
                    Path = HttpContext.Request.Path
                });
            }
            catch (Exception ex)
            {
                // Unexpected errors
                _logger.LogError(ex, "Login error for {Email}", request.Email);
                return StatusCode(500, new ErrorResponse
                {
                    Error = "InternalServerError",
                    Message = "An error occurred during login. Please try again later.",
                    Details = HttpContext.RequestServices.GetService<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>()?.IsDevelopment() == true
                        ? ex.Message : null,
                    Timestamp = DateTime.UtcNow,
                    Path = HttpContext.Request.Path
                });
            }
        }

        #endregion

        #region Password Reset

        /// Send password reset email
        /// <param name="request">Password reset request with email</param>
        /// <returns>Success status</returns>
        /// <response code="200">Password reset email sent</response>
        /// <response code="400">Invalid request data</response>
        /// <response code="429">Too many requests (rate limited)</response>
        /// <response code="500">Internal server error</response>
        [HttpPost("reset-password")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ResetPassword([FromBody] PasswordResetDto request)
        {
            try
            {
                _logger.LogInformation("Password reset requested for email: {Email}", request.Email);

                // Validate model state
                if (!ModelState.IsValid)
                {
                    return BadRequest(new ErrorResponse
                    {
                        Error = "ValidationError",
                        Message = "Invalid request data",
                        Details = ModelState.ToErrorString(),
                        Timestamp = DateTime.UtcNow,
                        Path = HttpContext.Request.Path
                    });
                }

                // Send password reset email
                // Note: Always return success even if email doesn't exist (security)
                await _authService.SendPasswordResetEmailAsync(request);

                _logger.LogInformation("Password reset email sent to {Email}", request.Email);

                return Ok(new SuccessResponse
                {
                    Success = true,
                    Message = "If an account with this email exists, a password reset link has been sent."
                });
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("rate limit"))
            {
                // Rate limit exceeded
                _logger.LogWarning("Password reset rate limit exceeded for {Email}", request.Email);
                return StatusCode(429, new ErrorResponse
                {
                    Error = "RateLimitExceeded",
                    Message = "Too many password reset requests. Please try again later.",
                    Timestamp = DateTime.UtcNow,
                    Path = HttpContext.Request.Path
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Password reset error for {Email}", request.Email);
                return StatusCode(500, new ErrorResponse
                {
                    Error = "InternalServerError",
                    Message = "An error occurred while processing your request. Please try again later.",
                    Timestamp = DateTime.UtcNow,
                    Path = HttpContext.Request.Path
                });
            }
        }

        /// Confirm password reset with token
        /// <param name="request">Reset token and new password</param>
        /// <returns>Success status</returns>
        /// <response code="200">Password reset successful</response>
        /// <response code="400">Invalid token or password</response>
        /// <response code="500">Internal server error</response>
        [HttpPost("confirm-reset-password")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ConfirmResetPassword([FromBody] ConfirmPasswordResetDto request)
        {
            try
            {
                _logger.LogInformation("Password reset confirmation attempt");

                // Validate model state
                if (!ModelState.IsValid)
                {
                    return BadRequest(new ErrorResponse
                    {
                        Error = "ValidationError",
                        Message = "Invalid request data",
                        Details = ModelState.ToErrorString(),
                        Timestamp = DateTime.UtcNow,
                        Path = HttpContext.Request.Path
                    });
                }

                // Confirm password reset
                await _authService.ConfirmPasswordResetAsync(request);

                _logger.LogInformation("Password reset successful");

                return Ok(new SuccessResponse
                {
                    Success = true,
                    Message = "Password has been reset successfully. You can now login with your new password."
                });
            }
            catch (ArgumentException ex)
            {
                // Invalid token or password
                _logger.LogWarning("Password reset confirmation failed: {Message}", ex.Message);
                return BadRequest(new ErrorResponse
                {
                    Error = "InvalidToken",
                    Message = ex.Message,
                    Timestamp = DateTime.UtcNow,
                    Path = HttpContext.Request.Path
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Password reset confirmation error");
                return StatusCode(500, new ErrorResponse
                {
                    Error = "InternalServerError",
                    Message = "An error occurred while resetting your password. Please try again later.",
                    Timestamp = DateTime.UtcNow,
                    Path = HttpContext.Request.Path
                });
            }
        }

        #endregion

        #region Change Password (Authenticated)

        /// Change password for authenticated user
        /// <param name="request">Current and new password</param>
        /// <returns>Success status</returns>
        /// <response code="200">Password changed successfully</response>
        /// <response code="400">Invalid request data</response>
        /// <response code="401">Unauthorized (not logged in or invalid token)</response>
        /// <response code="500">Internal server error</response>
        [HttpPost("change-password")]
        [AuthorizeRole("Parent", "Staff", "Admin")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto request)
        {
            try
            {
                // Get user ID from claims (set by FirebaseAuthMiddleware)
                var userId = User.FindFirst("firebase_uid")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new ErrorResponse
                    {
                        Error = "Unauthorized",
                        Message = "User not authenticated",
                        Timestamp = DateTime.UtcNow,
                        Path = HttpContext.Request.Path
                    });
                }

                _logger.LogInformation("Password change attempt for user {UserId}", userId);

                // Validate model state
                if (!ModelState.IsValid)
                {
                    return BadRequest(new ErrorResponse
                    {
                        Error = "ValidationError",
                        Message = "Invalid request data",
                        Details = ModelState.ToErrorString(),
                        Timestamp = DateTime.UtcNow,
                        Path = HttpContext.Request.Path
                    });
                }

                // Change password
                await _authService.ChangePasswordAsync(userId, request);

                _logger.LogInformation("Password changed successfully for user {UserId}", userId);

                return Ok(new SuccessResponse
                {
                    Success = true,
                    Message = "Password has been changed successfully."
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                // Current password incorrect
                _logger.LogWarning("Password change failed: {Message}", ex.Message);
                return Unauthorized(new ErrorResponse
                {
                    Error = "InvalidPassword",
                    Message = "Current password is incorrect.",
                    Timestamp = DateTime.UtcNow,
                    Path = HttpContext.Request.Path
                });
            }
            catch (ArgumentException ex)
            {
                // New password validation failed
                _logger.LogWarning("Password change validation failed: {Message}", ex.Message);
                return BadRequest(new ErrorResponse
                {
                    Error = "ValidationError",
                    Message = ex.Message,
                    Timestamp = DateTime.UtcNow,
                    Path = HttpContext.Request.Path
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Password change error");
                return StatusCode(500, new ErrorResponse
                {
                    Error = "InternalServerError",
                    Message = "An error occurred while changing your password. Please try again later.",
                    Timestamp = DateTime.UtcNow,
                    Path = HttpContext.Request.Path
                });
            }
        }

        #endregion

        #region Email Verification

        /// Send email verification link
        /// <returns>Success status</returns>
        /// <response code="200">Verification email sent</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="500">Internal server error</response>
        [HttpPost("send-verification-email")]
        [AuthorizeRole("Parent", "Staff", "Admin")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> SendVerificationEmail()
        {
            try
            {
                var email = User.FindFirst(ClaimTypes.Email)?.Value;
                if (string.IsNullOrEmpty(email))
                {
                    return Unauthorized(new ErrorResponse
                    {
                        Error = "Unauthorized",
                        Message = "User not authenticated",
                        Timestamp = DateTime.UtcNow,
                        Path = HttpContext.Request.Path
                    });
                }

                _logger.LogInformation("Sending verification email to {Email}", email);

                await _authService.SendEmailVerificationAsync(email);

                return Ok(new SuccessResponse
                {
                    Success = true,
                    Message = "Verification email sent. Please check your inbox."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Send verification email error");
                return StatusCode(500, new ErrorResponse
                {
                    Error = "InternalServerError",
                    Message = "An error occurred while sending verification email.",
                    Timestamp = DateTime.UtcNow,
                    Path = HttpContext.Request.Path
                });
            }
        }

        #endregion

        #region Token Management

        /// Refresh authentication token
        /// <param name="refreshToken">Refresh token from login</param>
        /// <returns>New authentication tokens</returns>
        /// <response code="200">Token refreshed successfully</response>
        /// <response code="401">Invalid refresh token</response>
        /// <response code="500">Internal server error</response>
        [HttpPost("refresh-token")]
        [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            try
            {
                _logger.LogInformation("Token refresh attempt");

                if (string.IsNullOrWhiteSpace(request.RefreshToken))
                {
                    return BadRequest(new ErrorResponse
                    {
                        Error = "ValidationError",
                        Message = "Refresh token is required",
                        Timestamp = DateTime.UtcNow,
                        Path = HttpContext.Request.Path
                    });
                }

                var result = await _authService.RefreshTokenAsync(request.RefreshToken);

                return Ok(new
                {
                    success = true,
                    message = "Token refreshed successfully",
                    data = result
                });
            }
            catch (UnauthorizedAccessException)
            {
                _logger.LogWarning("Token refresh failed: Invalid token");
                return Unauthorized(new ErrorResponse
                {
                    Error = "InvalidToken",
                    Message = "Invalid or expired refresh token",
                    Timestamp = DateTime.UtcNow,
                    Path = HttpContext.Request.Path
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Token refresh error");
                return StatusCode(500, new ErrorResponse
                {
                    Error = "InternalServerError",
                    Message = "An error occurred while refreshing token",
                    Timestamp = DateTime.UtcNow,
                    Path = HttpContext.Request.Path
                });
            }
        }

        /// Logout user (revoke tokens)
        /// <returns>Success status</returns>
        /// <response code="200">Logout successful</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="500">Internal server error</response>
        [HttpPost("logout")]
        [AuthorizeRole("Parent", "Staff", "Admin")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Logout()
        {
            try
            {
                var userId = User.FindFirst("firebase_uid")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new ErrorResponse
                    {
                        Error = "Unauthorized",
                        Message = "User not authenticated",
                        Timestamp = DateTime.UtcNow,
                        Path = HttpContext.Request.Path
                    });
                }

                _logger.LogInformation("Logout request for user {UserId}", userId);

                await _authService.LogoutAsync(userId);

                return Ok(new SuccessResponse
                {
                    Success = true,
                    Message = "Logout successful"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Logout error");
                return StatusCode(500, new ErrorResponse
                {
                    Error = "InternalServerError",
                    Message = "An error occurred during logout",
                    Timestamp = DateTime.UtcNow,
                    Path = HttpContext.Request.Path
                });
            }
        }

        #endregion

        #region User Profile

        /// Get current user profile
        /// <returns>User profile information</returns>
        /// <response code="200">User profile retrieved successfully</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="404">User not found</response>
        /// <response code="500">Internal server error</response>
        [HttpGet("profile")]
        [AuthorizeRole("Parent", "Staff", "Admin")]
        [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetProfile()
        {
            try
            {
                var userId = User.FindFirst("firebase_uid")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new ErrorResponse
                    {
                        Error = "Unauthorized",
                        Message = "User not authenticated",
                        Timestamp = DateTime.UtcNow,
                        Path = HttpContext.Request.Path
                    });
                }

                var profile = await _authService.GetUserProfileAsync(userId);

                return Ok(new
                {
                    success = true,
                    data = profile
                });
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
            {
                return NotFound(new ErrorResponse
                {
                    Error = "UserNotFound",
                    Message = "User profile not found",
                    Timestamp = DateTime.UtcNow,
                    Path = HttpContext.Request.Path
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Get profile error");
                return StatusCode(500, new ErrorResponse
                {
                    Error = "InternalServerError",
                    Message = "An error occurred while retrieving profile",
                    Timestamp = DateTime.UtcNow,
                    Path = HttpContext.Request.Path
                });
            }
        }

        #endregion
    }

    #region Helper Classes

    /// Standard error response format
    public class ErrorResponse
    {
        public string Error { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? Details { get; set; }
        public DateTime Timestamp { get; set; }
        public string Path { get; set; } = string.Empty;
    }

    /// Standard success response format
    public class SuccessResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    /// Refresh token request
    public class RefreshTokenRequest
    {
        public string RefreshToken { get; set; } = string.Empty;
    }

    #endregion
}

/// Extension methods for ModelStateDictionary
public static class ModelStateExtensions
{
    public static string ToErrorString(this Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary modelState)
    {
        var errors = modelState.Values
            .SelectMany(v => v.Errors)
            .Select(e => e.ErrorMessage)
            .ToList();

        return string.Join("; ", errors);
    }
}