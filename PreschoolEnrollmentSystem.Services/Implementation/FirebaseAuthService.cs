using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Http;
using FirebaseAdmin;
using FirebaseAdmin.Auth;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PreschoolEnrollmentSystem.Core.DTOs.Auth;
using PreschoolEnrollmentSystem.Core.Entities;
using PreschoolEnrollmentSystem.Core.Enums;
using PreschoolEnrollmentSystem.Core.Extensions;
using PreschoolEnrollmentSystem.Infrastructure.Repositories.Interfaces;
using PreschoolEnrollmentSystem.Services.Interfaces;

namespace PreschoolEnrollmentSystem.Services.Implementation
{
    public class FirebaseAuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IEmailService _emailService;
        private readonly ILogger<FirebaseAuthService> _logger;
        private readonly IConfiguration _configuration;
        private readonly IMemoryCache _cache;
        private readonly FirebaseAuth _firebaseAuth;
        private readonly string _firebaseApiKey;
        private readonly IHttpClientFactory _httpClientFactory;

        // Configuration constants
        private const int TOKEN_EXPIRATION_HOURS = 1;
        private const int REFRESH_TOKEN_DAYS = 30;
        private const int MAX_LOGIN_ATTEMPTS = 5;
        private const int LOGIN_LOCKOUT_MINUTES = 15;
        private const string FIREBASE_AUTH_URL = "https://identitytoolkit.googleapis.com/v1/accounts";
        private const string CACHE_KEY_PREFIX = "UserProfile_";
        private const int CACHE_EXPIRATION_MINUTES = 10;

        public FirebaseAuthService(
            IUserRepository userRepository,
            IEmailService emailService,
            ILogger<FirebaseAuthService> logger,
            IConfiguration configuration,
            IMemoryCache cache,
            IHttpClientFactory httpClientFactory)
        {
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
            _firebaseAuth = FirebaseAuth.DefaultInstance;
            _firebaseApiKey = _configuration["Firebase:ApiKey"]
                ?? throw new InvalidOperationException("Firebase API Key not configured");
        }

        #region Registration

        public async Task<LoginResponseDto> RegisterAsync(RegisterRequestDto request)
        {
            try
            {
                _logger.LogInformation("Registration attempt for email: {Email}, role: {Role}",
                    request.Email, request.Role);

                // Validate role
                UserRole userRole;
                try
                {
                    userRole = UserRoleExtensions.ParseRole(request.Role);
                }
                catch (ArgumentException ex)
                {
                    _logger.LogWarning("Invalid role in registration: {Role}", request.Role);
                    throw new ArgumentException($"Invalid role: {request.Role}", ex);
                }

                // Additional validation for Staff/Admin roles
                if (userRole != UserRole.Parent)
                {
                    ValidateStaffRegistrationData(request);
                }

                // Check if email already exists in database
                var existingUser = await _userRepository.GetUserByEmailAsync(request.Email);

                if (existingUser != null)
                {
                    _logger.LogWarning("Registration failed: Email already exists - {Email}", request.Email);
                    throw new InvalidOperationException("An account with this email already exists.");
                }

                // Create Firebase Auth user
                var userRecordArgs = new UserRecordArgs
                {
                    Email = request.Email,
                    Password = request.Password,
                    EmailVerified = false,
                    DisplayName = $"{request.FirstName} {request.LastName}",
                    PhoneNumber = request.PhoneNumber,
                    Disabled = false
                };

                UserRecord firebaseUser;
                try
                {
                    firebaseUser = await _firebaseAuth.CreateUserAsync(userRecordArgs);
                    _logger.LogInformation("Firebase user created: {Uid}", firebaseUser.Uid);
                }
                catch (FirebaseAuthException ex)
                {
                    _logger.LogError(ex, "Firebase user creation failed for {Email}", request.Email);

                    if (ex.Message.Contains("EMAIL_EXISTS"))
                        throw new InvalidOperationException("An account with this email already exists in Firebase.");

                    if (ex.Message.Contains("WEAK_PASSWORD"))
                        throw new ArgumentException("Password is too weak. Please use a stronger password.");

                    throw new InvalidOperationException("Failed to create user account. Please try again.", ex);
                }

                try
                {
                    // Set custom claims for role
                    var claims = new Dictionary<string, object>
                    {
                        { "role", userRole.ToRoleString() }
                    };
                    await _firebaseAuth.SetCustomUserClaimsAsync(firebaseUser.Uid, claims);
                    _logger.LogInformation("Custom claims set for user {Uid}: role={Role}",
                        firebaseUser.Uid, userRole.ToRoleString());

                    // Create database record
                    var userId = await CreateUserRecordAsync(firebaseUser, request, userRole);

                    _logger.LogInformation("Database record created for user {Uid} with ID {UserId}",
                        firebaseUser.Uid, userId);

                    // Send verification email
                    try
                    {
                        await SendEmailVerificationAsync(request.Email);
                        _logger.LogInformation("Verification email sent to {Email}", request.Email);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to send verification email to {Email}", request.Email);
                    }

                    // Generate custom token for immediate login
                    string customToken = await _firebaseAuth.CreateCustomTokenAsync(firebaseUser.Uid, claims);

                    // Return login response
                    var loginResponse = new LoginResponseDto
                    {
                        IdToken = customToken,
                        RefreshToken = string.Empty,
                        ExpiresAt = DateTime.UtcNow.AddHours(TOKEN_EXPIRATION_HOURS),
                        UserId = userId,
                        FirebaseUid = firebaseUser.Uid,
                        Email = request.Email,
                        EmailVerified = false,
                        FullName = $"{request.FirstName} {request.LastName}",
                        Role = userRole.ToRoleString(),
                        IsActive = true,
                        ProfileCompletionPercentage = userRole == UserRole.Parent ? 60 : null,
                        CanEnroll = false
                    };

                    _logger.LogInformation("Registration successful for {Email} with role {Role}",
                        request.Email, userRole.ToRoleString());

                    return loginResponse;
                }
                catch (Exception ex)
                {
                    // Rollback: Delete Firebase user if database creation fails
                    _logger.LogError(ex, "Database record creation failed, rolling back Firebase user {Uid}",
                        firebaseUser.Uid);

                    try
                    {
                        await _firebaseAuth.DeleteUserAsync(firebaseUser.Uid);
                        _logger.LogInformation("Firebase user {Uid} deleted as part of rollback",
                            firebaseUser.Uid);
                    }
                    catch (Exception rollbackEx)
                    {
                        _logger.LogError(rollbackEx,
                            "Failed to rollback Firebase user {Uid}. Manual cleanup required.",
                            firebaseUser.Uid);
                    }

                    throw;
                }
            }
            catch (ArgumentException)
            {
                throw;
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during registration for {Email}", request.Email);
                throw new InvalidOperationException("An unexpected error occurred during registration. Please try again.", ex);
            }
        }

        private async Task<Guid> CreateUserRecordAsync(UserRecord firebaseUser, RegisterRequestDto request, UserRole role)
        {
            var user = new User
            {
                Id = Guid.NewGuid(),
                FirebaseUid = firebaseUser.Uid,
                Email = request.Email,
                EmailVerified = false,
                FirstName = request.FirstName,
                LastName = request.LastName,
                Username = request.Email.Split('@')[0],
                Phone = request.PhoneNumber,
                PhoneVerified = false,
                PasswordHash = "FIREBASE_MANAGED",
                Role = role,
                Status = UserStatus.Active,
                IsActive = true,
                AcceptedTerms = request.AcceptTerms,
                TermsAcceptedAt = DateTime.UtcNow,
                CreatedBy = firebaseUser.Uid,
                CreatedAt = DateTime.UtcNow
            };

            user.CalculateProfileCompletion();

            await _userRepository.AddAsync(user);
            await _userRepository.SaveChangesAsync();

            return user.Id;
        }

        private void ValidateStaffRegistrationData(RegisterRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.JobTitle))
            {
                throw new ArgumentException("Job title is required for staff registration.", nameof(request.JobTitle));
            }
        }

        #endregion

        #region Login

        public async Task<LoginResponseDto> LoginAsync(LoginRequestDto request)
        {
            try
            {
                _logger.LogInformation("Login attempt for email: {Email}", request.Email);

                // First check if user exists in database
                var user = await _userRepository.GetUserByEmailAsync(request.Email);

                if (user == null)
                {
                    _logger.LogWarning("Login failed: User not found - {Email}", request.Email);
                    throw new UnauthorizedAccessException("Invalid email or password");
                }

                // Check if this is a seed user (database-only authentication)
                if (user.FirebaseUid.StartsWith("seed_") && user.PasswordHash != "FIREBASE_MANAGED")
                {
                    _logger.LogInformation("Database-only authentication for seed user: {Email}", request.Email);

                    // Verify password with BCrypt
                    if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                    {
                        _logger.LogWarning("Database authentication failed for {Email}", request.Email);
                        throw new UnauthorizedAccessException("Invalid email or password");
                    }

                    // Check if user is active
                    if (!user.IsActive || user.Status != UserStatus.Active)
                    {
                        _logger.LogWarning("Login attempt for inactive user: {Email}", request.Email);
                        throw new InvalidOperationException("Your account is currently inactive. Please contact support.");
                    }

                    // Update last login
                    user.UpdateLastLogin();
                    _userRepository.Update(user);
                    await _userRepository.SaveChangesAsync();

                    _logger.LogInformation("Database login successful for {Email}", request.Email);

                    // Generate a mock token for seed users (for testing purposes)
                    var mockToken = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{user.Email}:{DateTime.UtcNow.Ticks}"));

                    return new LoginResponseDto
                    {
                        IdToken = mockToken,
                        RefreshToken = string.Empty,
                        ExpiresAt = DateTime.UtcNow.AddHours(TOKEN_EXPIRATION_HOURS),
                        UserId = user.Id,
                        FirebaseUid = user.FirebaseUid,
                        Email = user.Email,
                        EmailVerified = user.EmailVerified,
                        FullName = user.GetFullName(),
                        Role = user.Role.ToRoleString(),
                        IsActive = user.IsActive,
                        ProfileCompletionPercentage = user.ProfileCompletionPercentage,
                        CanEnroll = user.CanEnroll()
                    };
                }

                // Original Firebase authentication for non-seed users
                var firebaseResponse = await SignInWithFirebaseAsync(request.Email, request.Password);

                if (firebaseResponse == null || string.IsNullOrEmpty(firebaseResponse.IdToken))
                {
                    _logger.LogWarning("Firebase authentication failed for {Email}", request.Email);
                    throw new UnauthorizedAccessException("Invalid email or password");
                }

                // Get user record from Firebase
                UserRecord firebaseUser;
                try
                {
                    firebaseUser = await _firebaseAuth.GetUserAsync(firebaseResponse.LocalId);
                }
                catch (FirebaseAuthException ex)
                {
                    _logger.LogError(ex, "Failed to get Firebase user for {Email}", request.Email);
                    throw new InvalidOperationException("User account not found", ex);
                }

                // Update Firebase UID if it was missing
                if (user.FirebaseUid != firebaseUser.Uid)
                {
                    user.FirebaseUid = firebaseUser.Uid;
                    _userRepository.Update(user);
                    await _userRepository.SaveChangesAsync();
                }

                // Check if user is active
                if (!user.IsActive || user.Status != UserStatus.Active)
                {
                    _logger.LogWarning("Login attempt for inactive user: {Email}", request.Email);
                    throw new InvalidOperationException("Your account is currently inactive. Please contact support.");
                }

                // Update last login
                user.UpdateLastLogin();
                _userRepository.Update(user);
                await _userRepository.SaveChangesAsync();

                _logger.LogInformation("Firebase login successful for {Email}", request.Email);

                // Build and return login response
                return new LoginResponseDto
                {
                    IdToken = firebaseResponse.IdToken,
                    RefreshToken = firebaseResponse.RefreshToken ?? string.Empty,
                    ExpiresAt = DateTime.UtcNow.AddSeconds(firebaseResponse.ExpiresIn),
                    UserId = user.Id,
                    FirebaseUid = firebaseUser.Uid,
                    Email = user.Email,
                    EmailVerified = user.EmailVerified,
                    FullName = user.GetFullName(),
                    Role = user.Role.ToRoleString(),
                    IsActive = user.IsActive,
                    ProfileCompletionPercentage = user.ProfileCompletionPercentage,
                    CanEnroll = user.CanEnroll()
                };
            }
            catch (UnauthorizedAccessException)
            {
                throw;
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during login for {Email}", request.Email);
                throw new InvalidOperationException("An unexpected error occurred during login. Please try again.", ex);
            }
        }

        private async Task<FirebaseSignInResponse?> SignInWithFirebaseAsync(string email, string password)
        {
            try
            {
                var httpClient = _httpClientFactory.CreateClient();
                var requestBody = new
                {
                    email,
                    password,
                    returnSecureToken = true
                };

                var response = await httpClient.PostAsJsonAsync(
                    $"{FIREBASE_AUTH_URL}:signInWithPassword?key={_firebaseApiKey}",
                    requestBody);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("Firebase sign-in failed: {StatusCode} - {Error}",
                        response.StatusCode, errorContent);
                    return null;
                }

                return await response.Content.ReadFromJsonAsync<FirebaseSignInResponse>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling Firebase sign-in API");
                return null;
            }
        }

        #endregion

        #region Password Management

        public async Task<bool> SendPasswordResetEmailAsync(PasswordResetDto request)
        {
            try
            {
                _logger.LogInformation("Password reset requested for email: {Email}", request.Email);

                // Verify user exists in database
                var user = await _userRepository.GetUserByEmailAsync(request.Email);
                if (user == null)
                {
                    _logger.LogWarning("Password reset requested for non-existent email: {Email}", request.Email);
                    // Don't reveal that user doesn't exist
                    return true;
                }

                // Send password reset email via Firebase
                var link = await _firebaseAuth.GeneratePasswordResetLinkAsync(request.Email);

                // Optionally send custom email
                await _emailService.SendPasswordResetEmailAsync(request.Email, link);

                _logger.LogInformation("Password reset email sent to {Email}", request.Email);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending password reset email to {Email}", request.Email);
                throw new InvalidOperationException("Failed to send password reset email. Please try again.", ex);
            }
        }

        public async Task<bool> ConfirmPasswordResetAsync(ConfirmPasswordResetDto request)
        {
            try
            {
                _logger.LogInformation("Password reset confirmation attempt");

                // Firebase handles the actual password reset via the reset link
                // This method might be used if you're doing a custom flow
                // For now, we'll just log and return success
                _logger.LogInformation("Password reset confirmed");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error confirming password reset");
                throw new InvalidOperationException("Failed to confirm password reset. Please try again.", ex);
            }
        }

        public async Task<bool> ChangePasswordAsync(string userId, ChangePasswordDto request)
        {
            try
            {
                _logger.LogInformation("Password change requested for user: {UserId}", userId);

                // Get user from database
                var user = await _userRepository.GetByIdAsync(Guid.Parse(userId));
                if (user == null)
                {
                    throw new InvalidOperationException("User not found");
                }

                // Verify current password by attempting sign-in
                var signInResponse = await SignInWithFirebaseAsync(user.Email, request.CurrentPassword);
                if (signInResponse == null)
                {
                    throw new UnauthorizedAccessException("Current password is incorrect");
                }

                // Update password in Firebase
                var updateArgs = new UserRecordArgs
                {
                    Uid = user.FirebaseUid,
                    Password = request.NewPassword
                };

                await _firebaseAuth.UpdateUserAsync(updateArgs);

                _logger.LogInformation("Password changed successfully for user: {UserId}", userId);
                return true;
            }
            catch (UnauthorizedAccessException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password for user: {UserId}", userId);
                throw new InvalidOperationException("Failed to change password. Please try again.", ex);
            }
        }

        #endregion

        #region Email Verification

        public async Task<bool> SendEmailVerificationAsync(string email)
        {
            try
            {
                _logger.LogInformation("Email verification requested for: {Email}", email);

                var link = await _firebaseAuth.GenerateEmailVerificationLinkAsync(email);
                await _emailService.SendEmailVerificationAsync(email, link);

                _logger.LogInformation("Email verification sent to {Email}", email);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending email verification to {Email}", email);
                throw new InvalidOperationException("Failed to send email verification. Please try again.", ex);
            }
        }

        public async Task<bool> VerifyEmailAsync(VerifyEmailDto request)
        {
            try
            {
                _logger.LogInformation("Email verification attempt for: {Email}", request.Email);

                // Get user from Firebase
                var firebaseUser = await _firebaseAuth.GetUserByEmailAsync(request.Email);

                // Update user in database
                var user = await _userRepository.FindSingleAsync(u => u.FirebaseUid == firebaseUser.Uid);
                if (user != null)
                {
                    user.EmailVerified = true;
                    user.CalculateProfileCompletion();
                    _userRepository.Update(user);
                    await _userRepository.SaveChangesAsync();
                }

                _logger.LogInformation("Email verified for: {Email}", request.Email);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying email for {Email}", request.Email);
                throw new InvalidOperationException("Failed to verify email. Please try again.", ex);
            }
        }

        #endregion

        #region Token Management

        public async Task<LoginResponseDto> RefreshTokenAsync(string refreshToken)
        {
            try
            {
                _logger.LogInformation("Token refresh requested");

                var httpClient = _httpClientFactory.CreateClient();
                var requestBody = new
                {
                    grant_type = "refresh_token",
                    refresh_token = refreshToken
                };

                var response = await httpClient.PostAsJsonAsync(
                    $"https://securetoken.googleapis.com/v1/token?key={_firebaseApiKey}",
                    requestBody);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Token refresh failed");
                    throw new UnauthorizedAccessException("Invalid refresh token");
                }

                var tokenResponse = await response.Content.ReadFromJsonAsync<FirebaseRefreshTokenResponse>();
                if (tokenResponse == null)
                {
                    throw new InvalidOperationException("Failed to parse token response");
                }

                // Decode the new ID token to get user info
                var decodedToken = await _firebaseAuth.VerifyIdTokenAsync(tokenResponse.IdToken);
                var user = await _userRepository.FindSingleAsync(u => u.FirebaseUid == decodedToken.Uid);

                if (user == null)
                {
                    throw new InvalidOperationException("User not found");
                }

                _logger.LogInformation("Token refreshed successfully for user: {UserId}", user.Id);

                return new LoginResponseDto
                {
                    IdToken = tokenResponse.IdToken,
                    RefreshToken = tokenResponse.RefreshToken,
                    ExpiresAt = DateTime.UtcNow.AddSeconds(int.Parse(tokenResponse.ExpiresIn)),
                    UserId = user.Id,
                    FirebaseUid = user.FirebaseUid,
                    Email = user.Email,
                    EmailVerified = user.EmailVerified,
                    FullName = user.GetFullName(),
                    Role = user.Role.ToRoleString(),
                    IsActive = user.IsActive,
                    ProfileCompletionPercentage = user.ProfileCompletionPercentage,
                    CanEnroll = user.CanEnroll()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing token");
                throw new InvalidOperationException("Failed to refresh token. Please login again.", ex);
            }
        }

        public async Task<FirebaseTokenInfo> VerifyIdTokenAsync(string idToken)
        {
            try
            {
                var decodedToken = await _firebaseAuth.VerifyIdTokenAsync(idToken);

                string role = "Parent";
                if (decodedToken.Claims.TryGetValue("role", out var roleClaim))
                {
                    role = roleClaim.ToString() ?? "Parent";
                }

                return new FirebaseTokenInfo
                {
                    Uid = decodedToken.Uid,
                    Email = decodedToken.Claims.TryGetValue("email", out var emailClaim)
                        ? emailClaim.ToString() ?? string.Empty
                        : string.Empty,
                    EmailVerified = decodedToken.Claims.TryGetValue("email_verified", out var verifiedClaim)
                        && Convert.ToBoolean(verifiedClaim),
                    Role = role,
                    IssuedAt = DateTimeOffset.FromUnixTimeSeconds((long)decodedToken.IssuedAtTimeSeconds).DateTime,
                    ExpiresAt = DateTimeOffset.FromUnixTimeSeconds((long)decodedToken.ExpirationTimeSeconds).DateTime
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying ID token");
                throw new UnauthorizedAccessException("Invalid ID token", ex);
            }
        }

        public async Task<bool> LogoutAsync(string userId)
        {
            try
            {
                _logger.LogInformation("Logout requested for user: {UserId}", userId);

                // Clear user profile cache
                _cache.Remove($"{CACHE_KEY_PREFIX}{userId}");

                // Firebase doesn't have server-side logout
                // Token invalidation happens on client side
                // We just clear our cache and return success

                _logger.LogInformation("Logout successful for user: {UserId}", userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout for user: {UserId}", userId);
                return false;
            }
        }

        #endregion

        #region User Profile

        public async Task<UserProfileDto> GetUserProfileAsync(string firebaseUid)
        {
            try
            {
                _logger.LogInformation("Fetching user profile for Firebase UID: {Uid}", firebaseUid);

                // Check cache first
                string cacheKey = $"{CACHE_KEY_PREFIX}{firebaseUid}";
                if (_cache.TryGetValue<UserProfileDto>(cacheKey, out var cachedProfile) && cachedProfile != null)
                {
                    _logger.LogInformation("User profile retrieved from cache for {Uid}", firebaseUid);
                    return cachedProfile;
                }

                // Get user from database
                var user = await _userRepository.FindSingleAsync(u => u.FirebaseUid == firebaseUid);

                if (user == null)
                {
                    // Try to get from Firebase and create database record
                    var firebaseUser = await _firebaseAuth.GetUserAsync(firebaseUid);
                    if (firebaseUser == null)
                    {
                        throw new InvalidOperationException("User not found");
                    }

                    // User exists in Firebase but not in database - this is a data inconsistency
                    _logger.LogError("User exists in Firebase but not in database: {Uid}", firebaseUid);
                    throw new InvalidOperationException("User profile not found. Please contact support.");
                }

                // Map to DTO
                var profile = MapToUserProfileDto(user);

                // Cache the profile
                _cache.Set(cacheKey, profile, TimeSpan.FromMinutes(CACHE_EXPIRATION_MINUTES));

                _logger.LogInformation("User profile fetched for {Uid}", firebaseUid);
                return profile;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching user profile for Firebase UID: {Uid}", firebaseUid);
                throw new InvalidOperationException("Failed to fetch user profile. Please try again.", ex);
            }
        }

        private UserProfileDto MapToUserProfileDto(User user)
        {
            return new UserProfileDto
            {
                Id = user.Id,
                FirebaseUid = user.FirebaseUid,
                Email = user.Email,
                EmailVerified = user.EmailVerified,
                FirstName = user.FirstName,
                LastName = user.LastName,
                FullName = user.GetFullName(),
                PhoneNumber = user.Phone ?? string.Empty,
                Role = user.Role.ToRoleString(),
                IsActive = user.IsActive,
                LastLoginAt = user.LastLoginAt,
                ProfileCompletionPercentage = user.ProfileCompletionPercentage
            };
        }

        #endregion

        #region Admin Operations

        public async Task<bool> UpdateUserRoleAsync(string userId, string newRole, string adminUserId)
        {
            try
            {
                _logger.LogInformation("Role update requested for user {UserId} by admin {AdminId}",
                    userId, adminUserId);

                var user = await _userRepository.GetByIdAsync(Guid.Parse(userId));
                if (user == null)
                {
                    throw new InvalidOperationException("User not found");
                }

                var userRole = UserRoleExtensions.ParseRole(newRole);

                // Update role in Firebase custom claims
                var claims = new Dictionary<string, object>
                {
                    { "role", userRole.ToRoleString() }
                };
                await _firebaseAuth.SetCustomUserClaimsAsync(user.FirebaseUid, claims);

                // Update role in database
                user.Role = userRole;
                _userRepository.Update(user);
                await _userRepository.SaveChangesAsync();

                // Clear cache
                _cache.Remove($"{CACHE_KEY_PREFIX}{user.FirebaseUid}");

                _logger.LogInformation("Role updated successfully for user {UserId} to {Role}",
                    userId, newRole);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating role for user: {UserId}", userId);
                throw new InvalidOperationException("Failed to update user role. Please try again.", ex);
            }
        }

        public async Task<bool> DeactivateUserAsync(string userId, string adminUserId)
        {
            try
            {
                _logger.LogInformation("Deactivation requested for user {UserId} by admin {AdminId}",
                    userId, adminUserId);

                var user = await _userRepository.GetByIdAsync(Guid.Parse(userId));
                if (user == null)
                {
                    throw new InvalidOperationException("User not found");
                }

                // Disable in Firebase
                var updateArgs = new UserRecordArgs
                {
                    Uid = user.FirebaseUid,
                    Disabled = true
                };
                await _firebaseAuth.UpdateUserAsync(updateArgs);

                // Deactivate in database
                user.IsActive = false;
                user.Status = UserStatus.Inactive;
                _userRepository.Update(user);
                await _userRepository.SaveChangesAsync();

                // Clear cache
                _cache.Remove($"{CACHE_KEY_PREFIX}{user.FirebaseUid}");

                _logger.LogInformation("User deactivated successfully: {UserId}", userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deactivating user: {UserId}", userId);
                throw new InvalidOperationException("Failed to deactivate user. Please try again.", ex);
            }
        }

        public async Task<bool> ReactivateUserAsync(string userId, string adminUserId)
        {
            try
            {
                _logger.LogInformation("Reactivation requested for user {UserId} by admin {AdminId}",
                    userId, adminUserId);

                var user = await _userRepository.GetByIdAsync(Guid.Parse(userId));
                if (user == null)
                {
                    throw new InvalidOperationException("User not found");
                }

                // Enable in Firebase
                var updateArgs = new UserRecordArgs
                {
                    Uid = user.FirebaseUid,
                    Disabled = false
                };
                await _firebaseAuth.UpdateUserAsync(updateArgs);

                // Reactivate in database
                user.IsActive = true;
                user.Status = UserStatus.Active;
                _userRepository.Update(user);
                await _userRepository.SaveChangesAsync();

                // Clear cache
                _cache.Remove($"{CACHE_KEY_PREFIX}{user.FirebaseUid}");

                _logger.LogInformation("User reactivated successfully: {UserId}", userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reactivating user: {UserId}", userId);
                throw new InvalidOperationException("Failed to reactivate user. Please try again.", ex);
            }
        }

        #endregion

        #region Helper Classes

        private class FirebaseSignInResponse
        {
            public string LocalId { get; set; } = string.Empty;
            public string IdToken { get; set; } = string.Empty;
            public string RefreshToken { get; set; } = string.Empty;
            public int ExpiresIn { get; set; }
        }

        private class FirebaseRefreshTokenResponse
        {
            public string IdToken { get; set; } = string.Empty;
            public string RefreshToken { get; set; } = string.Empty;
            public string ExpiresIn { get; set; } = string.Empty;
        }

        #endregion
    }
}
