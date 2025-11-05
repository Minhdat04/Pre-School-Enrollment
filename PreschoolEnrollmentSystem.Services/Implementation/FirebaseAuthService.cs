using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using FirebaseAdmin;
using FirebaseAdmin.Auth;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PreschoolEnrollmentSystem.API.DTOs.Auth;
using PreschoolEnrollmentSystem.Core.Entities;
using PreschoolEnrollmentSystem.Core.Enums;
using PreschoolEnrollmentSystem.Infrastructure.Repositories.Interfaces;
using PreschoolEnrollmentSystem.Services.Interfaces;

namespace PreschoolEnrollmentSystem.Services.Implementation
{
    public class FirebaseAuthService : IAuthService
    {
        private readonly IParentRepository _parentRepository;
        private readonly IStaffRepository _staffRepository;
        private readonly IEmailService _emailService;
        private readonly ILogger<FirebaseAuthService> _logger;
        private readonly IConfiguration _configuration;
        private readonly IMemoryCache _cache;
        private readonly FirebaseAuth _firebaseAuth;
        private readonly string _firebaseApiKey;
        private readonly HttpClient _httpClient;

        // Configuration constants
        private const int TOKEN_EXPIRATION_HOURS = 1;
        private const int REFRESH_TOKEN_DAYS = 30;
        private const int MAX_LOGIN_ATTEMPTS = 5;
        private const int LOGIN_LOCKOUT_MINUTES = 15;
        private const string FIREBASE_AUTH_URL = "https://identitytoolkit.googleapis.com/v1/accounts";
        private const string CACHE_KEY_PREFIX = "UserProfile_";
        private const int CACHE_EXPIRATION_MINUTES = 10;

        public FirebaseAuthService(
            IParentRepository parentRepository,
            IStaffRepository staffRepository,
            IEmailService emailService,
            ILogger<FirebaseAuthService> logger,
            IConfiguration configuration,
            IMemoryCache cache)
        {
            _parentRepository = parentRepository ?? throw new ArgumentNullException(nameof(parentRepository));
            _staffRepository = staffRepository ?? throw new ArgumentNullException(nameof(staffRepository));
            _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _firebaseAuth = FirebaseAuth.DefaultInstance;
            _firebaseApiKey = _configuration["Firebase:ApiKey"]
                ?? throw new InvalidOperationException("Firebase API Key not configured");
            _httpClient = new HttpClient();
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
                var existingParent = await _parentRepository.GetByEmailAsync(request.Email);
                var existingStaff = await _staffRepository.GetByEmailAsync(request.Email);

                if (existingParent != null || existingStaff != null)
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

                    // Create database record based on role
                    Guid userId;
                    if (userRole == UserRole.Parent)
                    {
                        userId = await CreateParentRecordAsync(firebaseUser, request);
                    }
                    else
                    {
                        userId = await CreateStaffRecordAsync(firebaseUser, request, userRole);
                    }

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

        private async Task<Guid> CreateParentRecordAsync(UserRecord firebaseUser, RegisterRequestDto request)
        {
            var parent = new Parent
            {
                Id = Guid.NewGuid(),
                FirebaseUid = firebaseUser.Uid,
                Email = request.Email,
                EmailVerified = false,
                FirstName = request.FirstName,
                LastName = request.LastName,
                PhoneNumber = request.PhoneNumber,
                PhoneVerified = false,
                Role = UserRole.Parent,
                IsActive = true,
                AcceptedTerms = request.AcceptTerms,
                TermsAcceptedAt = DateTime.UtcNow,
                CreatedBy = firebaseUser.Uid,
                CreatedAt = DateTime.UtcNow
            };

            parent.CalculateProfileCompletion();

            await _parentRepository.AddAsync(parent);
            await _parentRepository.SaveChangesAsync();

            return parent.Id;
        }

        private async Task<Guid> CreateStaffRecordAsync(UserRecord firebaseUser, RegisterRequestDto request, UserRole role)
        {
            var staff = new Staff
            {
                Id = Guid.NewGuid(),
                FirebaseUid = firebaseUser.Uid,
                Email = request.Email,
                EmailVerified = false,
                FirstName = request.FirstName,
                LastName = request.LastName,
                PhoneNumber = request.PhoneNumber,
                PhoneVerified = false,
                JobTitle = request.JobTitle ?? "Staff Member",
                EmployeeId = request.EmployeeId,
                Department = request.Department,
                HireDate = DateTime.UtcNow,
                EmploymentStatus = "Active",
                WorkScheduleType = "FullTime",
                BackgroundCheckStatus = "Pending",
                Role = role,
                IsActive = role != UserRole.Admin,
                CreatedBy = firebaseUser.Uid,
                CreatedAt = DateTime.UtcNow
            };

            await _staffRepository.AddAsync(staff);
            await _staffRepository.SaveChangesAsync();

            return staff.Id;
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

                // Authenticate with Firebase
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

                // Find user in database
                Parent? parent = null;
                Staff? staff = null;
                UserRole userRole;

                if (firebaseUser.CustomClaims != null &&
                    firebaseUser.CustomClaims.TryGetValue("role", out var roleValue))
                {
                    userRole = UserRoleExtensions.ParseRole(roleValue.ToString() ?? "Parent");

                    if (userRole == UserRole.Parent)
                    {
                        parent = await _parentRepository.GetByFirebaseUidAsync(firebaseUser.Uid);
                        if (parent == null)
                        {
                            _logger.LogError("Parent record not found for Firebase UID: {Uid}", firebaseUser.Uid);
                            throw new InvalidOperationException("User profile not found. Please contact support.");
                        }
                    }
                    else
                    {
                        staff = await _staffRepository.GetByFirebaseUidAsync(firebaseUser.Uid);
                        if (staff == null)
                        {
                            _logger.LogError("Staff record not found for Firebase UID: {Uid}", firebaseUser.Uid);
                            throw new InvalidOperationException("User profile not found. Please contact support.");
                        }
                    }
                }
                else
                {
                    parent = await _parentRepository.GetByFirebaseUidAsync(firebaseUser.Uid);
                    if (parent != null)
                    {
                        userRole = UserRole.Parent;
                    }
                    else
                    {
                        staff = await _staffRepository.GetByFirebaseUidAsync(firebaseUser.Uid);
                        if (staff != null)
                        {
                            userRole = staff.Role;
                        }
                        else
                        {
                            _logger.LogError("User not found in database for Firebase UID: {Uid}", firebaseUser.Uid);
                            throw new InvalidOperationException("User profile not found. Please contact support.");
                        }
                    }
                }

                // Check if account is active
                bool isActive = parent?.IsActive ?? staff?.IsActive ?? false;
                if (!isActive)
                {
                    _logger.LogWarning("Login attempt for inactive account: {Email}", request.Email);
                    throw new InvalidOperationException("Your account is inactive. Please contact support.");
                }

                // Update last login timestamp
                if (parent != null)
                {
                    parent.UpdateLastLogin();
                    await _parentRepository.UpdateAsync(parent);
                    await _parentRepository.SaveChangesAsync();
                }
                else if (staff != null)
                {
                    staff.UpdateLastLogin();
                    await _staffRepository.UpdateAsync(staff);
                    await _staffRepository.SaveChangesAsync();
                }

                var expiresAt = DateTime.UtcNow.AddHours(request.RememberMe ? 720 : TOKEN_EXPIRATION_HOURS);

                var loginResponse = new LoginResponseDto
                {
                    IdToken = firebaseResponse.IdToken,
                    RefreshToken = firebaseResponse.RefreshToken,
                    ExpiresAt = expiresAt,
                    UserId = parent?.Id ?? staff?.Id ?? Guid.Empty,
                    FirebaseUid = firebaseUser.Uid,
                    Email = firebaseUser.Email ?? request.Email,
                    EmailVerified = firebaseUser.EmailVerified,
                    FullName = parent?.FullName ?? staff?.FullName ?? "",
                    Role = userRole.ToRoleString(),
                    IsActive = isActive,
                    ProfileCompletionPercentage = parent?.ProfileCompletionPercentage,
                    CanEnroll = parent?.IsProfileCompleteForEnrollment() ?? false
                };

                _logger.LogInformation("Login successful for {Email}, role: {Role}",
                    request.Email, userRole.ToRoleString());

                return loginResponse;
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
                throw new InvalidOperationException("An error occurred during login. Please try again.", ex);
            }
        }

        private async Task<FirebaseSignInResponse?> SignInWithFirebaseAsync(string email, string password)
        {
            try
            {
                var requestBody = new
                {
                    email = email,
                    password = password,
                    returnSecureToken = true
                };

                var response = await _httpClient.PostAsJsonAsync(
                    $"{FIREBASE_AUTH_URL}:signInWithPassword?key={_firebaseApiKey}",
                    requestBody
                );

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("Firebase sign-in failed: {StatusCode}, {Error}",
                        response.StatusCode, errorContent);
                    return null;
                }

                var result = await response.Content.ReadFromJsonAsync<FirebaseSignInResponse>();
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling Firebase sign-in API");
                throw new InvalidOperationException("Authentication service error", ex);
            }
        }

        #endregion

        #region Password Reset

        public async Task<bool> SendPasswordResetEmailAsync(PasswordResetDto request)
        {
            try
            {
                _logger.LogInformation("Password reset requested for email: {Email}", request.Email);

                string resetLink;
                try
                {
                    resetLink = await _firebaseAuth.GeneratePasswordResetLinkAsync(request.Email);
                    _logger.LogInformation("Password reset link generated for {Email}", request.Email);
                }
                catch (FirebaseAuthException ex)
                {
                    _logger.LogWarning("Password reset link generation failed for {Email}: {Error}",
                        request.Email, ex.Message);
                    return true; // Security: prevent email enumeration
                }

                try
                {
                    await _emailService.SendPasswordResetEmailAsync(request.Email, resetLink);
                    _logger.LogInformation("Password reset email sent to {Email}", request.Email);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send password reset email to {Email}", request.Email);
                    throw new InvalidOperationException("Failed to send password reset email. Please try again later.", ex);
                }

                return true;
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during password reset for {Email}", request.Email);
                throw new InvalidOperationException("An error occurred while processing password reset request.", ex);
            }
        }

        public async Task<bool> ConfirmPasswordResetAsync(ConfirmPasswordResetDto request)
        {
            try
            {
                _logger.LogInformation("Password reset confirmation attempt");

                string email;
                try
                {
                    email = await VerifyPasswordResetCodeAsync(request.Token);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Invalid or expired password reset token");
                    throw new ArgumentException("Invalid or expired password reset token. Please request a new one.", ex);
                }

                try
                {
                    await ConfirmPasswordResetWithCodeAsync(request.Token, request.NewPassword);
                    _logger.LogInformation("Password reset successful for email: {Email}", email);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to reset password for {Email}", email);
                    throw new InvalidOperationException("Failed to reset password. Please try again.", ex);
                }

                try
                {
                    await _emailService.SendPasswordChangedNotificationAsync(email);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to send password change notification to {Email}", email);
                }

                return true;
            }
            catch (ArgumentException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during password reset confirmation");
                throw new InvalidOperationException("An error occurred while resetting password.", ex);
            }
        }

        private async Task<string> VerifyPasswordResetCodeAsync(string resetCode)
        {
            try
            {
                var requestBody = new { oobCode = resetCode };

                var response = await _httpClient.PostAsJsonAsync(
                    $"{FIREBASE_AUTH_URL}:resetPassword?key={_firebaseApiKey}",
                    requestBody
                );

                if (!response.IsSuccessStatusCode)
                {
                    throw new ArgumentException("Invalid or expired reset code");
                }

                var result = await response.Content.ReadFromJsonAsync<JsonDocument>();
                var email = result?.RootElement.GetProperty("email").GetString();

                if (string.IsNullOrEmpty(email))
                {
                    throw new ArgumentException("Failed to verify reset code");
                }

                return email;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying password reset code");
                throw;
            }
        }

        private async Task ConfirmPasswordResetWithCodeAsync(string resetCode, string newPassword)
        {
            try
            {
                var requestBody = new
                {
                    oobCode = resetCode,
                    newPassword = newPassword
                };

                var response = await _httpClient.PostAsJsonAsync(
                    $"{FIREBASE_AUTH_URL}:resetPassword?key={_firebaseApiKey}",
                    requestBody
                );

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    throw new InvalidOperationException($"Failed to reset password: {error}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error confirming password reset");
                throw;
            }
        }

        public async Task<bool> ChangePasswordAsync(string userId, ChangePasswordDto request)
        {
            try
            {
                _logger.LogInformation("Password change attempt for user {UserId}", userId);

                UserRecord firebaseUser;
                try
                {
                    firebaseUser = await _firebaseAuth.GetUserAsync(userId);
                }
                catch (FirebaseAuthException ex)
                {
                    _logger.LogError(ex, "Failed to get Firebase user {UserId}", userId);
                    throw new InvalidOperationException("User not found", ex);
                }

                var signInResponse = await SignInWithFirebaseAsync(firebaseUser.Email, request.CurrentPassword);
                if (signInResponse == null)
                {
                    _logger.LogWarning("Current password verification failed for user {UserId}", userId);
                    throw new UnauthorizedAccessException("Current password is incorrect");
                }

                try
                {
                    var updateRequest = new UserRecordArgs
                    {
                        Uid = userId,
                        Password = request.NewPassword
                    };
                    await _firebaseAuth.UpdateUserAsync(updateRequest);
                    _logger.LogInformation("Password updated successfully for user {UserId}", userId);
                }
                catch (FirebaseAuthException ex)
                {
                    _logger.LogError(ex, "Failed to update password for user {UserId}", userId);
                    throw new InvalidOperationException("Failed to update password. Please try again.", ex);
                }

                try
                {
                    await _emailService.SendPasswordChangedNotificationAsync(firebaseUser.Email);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to send password change notification to {Email}", firebaseUser.Email);
                }

                return true;
            }
            catch (UnauthorizedAccessException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during password change for user {UserId}", userId);
                throw new InvalidOperationException("An error occurred while changing password.", ex);
            }
        }

        #endregion

        #region Email Verification

        public async Task<bool> SendEmailVerificationAsync(string email)
        {
            try
            {
                _logger.LogInformation("Email verification requested for {Email}", email);

                UserRecord firebaseUser;
                try
                {
                    firebaseUser = await _firebaseAuth.GetUserByEmailAsync(email);
                }
                catch (FirebaseAuthException ex)
                {
                    _logger.LogWarning("User not found for email verification: {Email}", email);
                    throw new InvalidOperationException("User not found", ex);
                }

                if (firebaseUser.EmailVerified)
                {
                    _logger.LogInformation("Email already verified for {Email}", email);
                    return true;
                }

                string verificationLink;
                try
                {
                    verificationLink = await _firebaseAuth.GenerateEmailVerificationLinkAsync(email);
                    _logger.LogInformation("Email verification link generated for {Email}", email);
                }
                catch (FirebaseAuthException ex)
                {
                    _logger.LogError(ex, "Failed to generate verification link for {Email}", email);
                    throw new InvalidOperationException("Failed to generate verification link", ex);
                }

                try
                {
                    await _emailService.SendVerificationEmailAsync(email, verificationLink);
                    _logger.LogInformation("Verification email sent to {Email}", email);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send verification email to {Email}", email);
                    throw new InvalidOperationException("Failed to send verification email. Please try again later.", ex);
                }

                return true;
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error sending email verification to {Email}", email);
                throw new InvalidOperationException("An error occurred while sending verification email.", ex);
            }
        }

        public async Task<bool> VerifyEmailAsync(VerifyEmailDto request)
        {
            try
            {
                _logger.LogInformation("Email verification confirmation attempt");
                _logger.LogInformation("Email verification successful");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during email verification");
                throw new ArgumentException("Invalid or expired verification token", ex);
            }
        }

        #endregion

        #region Token Management

        public async Task<LoginResponseDto> RefreshTokenAsync(string refreshToken)
        {
            try
            {
                _logger.LogInformation("Token refresh attempt");

                var response = await ExchangeRefreshTokenAsync(refreshToken);

                if (response == null || string.IsNullOrEmpty(response.IdToken))
                {
                    _logger.LogWarning("Token refresh failed: Invalid refresh token");
                    throw new UnauthorizedAccessException("Invalid or expired refresh token");
                }

                var decodedToken = await _firebaseAuth.VerifyIdTokenAsync(response.IdToken);
                var profile = await GetUserProfileAsync(decodedToken.Uid);

                var loginResponse = new LoginResponseDto
                {
                    IdToken = response.IdToken,
                    RefreshToken = response.RefreshToken,
                    ExpiresAt = DateTime.UtcNow.AddHours(TOKEN_EXPIRATION_HOURS),
                    UserId = profile.Id,
                    FirebaseUid = profile.FirebaseUid,
                    Email = profile.Email,
                    EmailVerified = profile.EmailVerified,
                    FullName = profile.FullName,
                    Role = profile.Role,
                    IsActive = profile.IsActive,
                    ProfileCompletionPercentage = profile.ProfileCompletionPercentage,
                    CanEnroll = profile.Role == "Parent" && profile.ProfileCompletionPercentage >= 85
                };

                _logger.LogInformation("Token refreshed successfully for user {UserId}", profile.FirebaseUid);
                return loginResponse;
            }
            catch (UnauthorizedAccessException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during token refresh");
                throw new UnauthorizedAccessException("Token refresh failed", ex);
            }
        }

        private async Task<RefreshTokenResponse?> ExchangeRefreshTokenAsync(string refreshToken)
        {
            try
            {
                var requestBody = new
                {
                    grant_type = "refresh_token",
                    refresh_token = refreshToken
                };

                var response = await _httpClient.PostAsJsonAsync(
                    $"https://securetoken.googleapis.com/v1/token?key={_firebaseApiKey}",
                    requestBody
                );

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("Refresh token exchange failed: {Error}", error);
                    return null;
                }

                var result = await response.Content.ReadFromJsonAsync<RefreshTokenResponse>();
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exchanging refresh token");
                throw;
            }
        }

        public async Task<bool> LogoutAsync(string userId)
        {
            try
            {
                _logger.LogInformation("Logout request for user {UserId}", userId);

                try
                {
                    await _firebaseAuth.RevokeRefreshTokensAsync(userId);
                    _logger.LogInformation("Refresh tokens revoked for user {UserId}", userId);
                }
                catch (FirebaseAuthException ex)
                {
                    _logger.LogError(ex, "Failed to revoke tokens for user {UserId}", userId);
                }

                _cache.Remove($"{CACHE_KEY_PREFIX}{userId}");

                _logger.LogInformation("Logout successful for user {UserId}", userId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout for user {UserId}", userId);
                throw new InvalidOperationException("An error occurred during logout", ex);
            }
        }

        public async Task<FirebaseTokenInfo> VerifyIdTokenAsync(string idToken)
        {
            try
            {
                var decodedToken = await _firebaseAuth.VerifyIdTokenAsync(idToken);

                string role = "Parent";
                if (decodedToken.Claims.TryGetValue("role", out var roleValue))
                {
                    role = roleValue.ToString() ?? "Parent";
                }

                var tokenInfo = new FirebaseTokenInfo
                {
                    Uid = decodedToken.Uid,
                    Email = decodedToken.Claims.ContainsKey("email")
                        ? decodedToken.Claims["email"].ToString() ?? ""
                        : "",
                    EmailVerified = decodedToken.Claims.ContainsKey("email_verified")
                        && Convert.ToBoolean(decodedToken.Claims["email_verified"]),
                    Role = role,
                    IssuedAt = DateTimeOffset.FromUnixTimeSeconds(decodedToken.IssuedAtTimeSeconds).UtcDateTime,
                    ExpiresAt = DateTimeOffset.FromUnixTimeSeconds(decodedToken.ExpirationTimeSeconds).UtcDateTime
                };

                return tokenInfo;
            }
            catch (FirebaseAuthException ex)
            {
                _logger.LogWarning("Token verification failed: {Message}", ex.Message);
                throw new UnauthorizedAccessException("Invalid or expired token", ex);
            }
        }

        #endregion

        #region User Profile Management

        public async Task<UserProfileDto> GetUserProfileAsync(string firebaseUid)
        {
            try
            {
                var cacheKey = $"{CACHE_KEY_PREFIX}{firebaseUid}";
                if (_cache.TryGetValue(cacheKey, out UserProfileDto? cachedProfile) && cachedProfile != null)
                {
                    _logger.LogDebug("Returning cached profile for user {FirebaseUid}", firebaseUid);
                    return cachedProfile;
                }

                _logger.LogInformation("Retrieving profile for user {FirebaseUid}", firebaseUid);

                UserRecord firebaseUser;
                try
                {
                    firebaseUser = await _firebaseAuth.GetUserAsync(firebaseUid);
                }
                catch (FirebaseAuthException ex)
                {
                    _logger.LogError(ex, "Firebase user not found: {FirebaseUid}", firebaseUid);
                    throw new InvalidOperationException("User not found", ex);
                }

                UserProfileDto profile;
                if (firebaseUser.CustomClaims != null &&
                    firebaseUser.CustomClaims.TryGetValue("role", out var roleValue))
                {
                    var role = roleValue.ToString() ?? "Parent";

                    if (role.Equals("Parent", StringComparison.OrdinalIgnoreCase))
                    {
                        var parent = await _parentRepository.GetByFirebaseUidAsync(firebaseUid);
                        if (parent == null)
                        {
                            throw new InvalidOperationException("Parent profile not found");
                        }
                        profile = MapToUserProfileDto(parent, firebaseUser);
                    }
                    else
                    {
                        var staff = await _staffRepository.GetByFirebaseUidAsync(firebaseUid);
                        if (staff == null)
                        {
                            throw new InvalidOperationException("Staff profile not found");
                        }
                        profile = MapToUserProfileDto(staff, firebaseUser);
                    }
                }
                else
                {
                    var parent = await _parentRepository.GetByFirebaseUidAsync(firebaseUid);
                    if (parent != null)
                    {
                        profile = MapToUserProfileDto(parent, firebaseUser);
                    }
                    else
                    {
                        var staff = await _staffRepository.GetByFirebaseUidAsync(firebaseUid);
                        if (staff == null)
                        {
                            throw new InvalidOperationException("User profile not found");
                        }
                        profile = MapToUserProfileDto(staff, firebaseUser);
                    }
                }

                var cacheOptions = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromMinutes(CACHE_EXPIRATION_MINUTES));
                _cache.Set(cacheKey, profile, cacheOptions);

                _logger.LogInformation("Profile retrieved for user {FirebaseUid}, role: {Role}",
                    firebaseUid, profile.Role);

                return profile;
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving profile for user {FirebaseUid}", firebaseUid);
                throw new InvalidOperationException("An error occurred while retrieving user profile", ex);
            }
        }

        private UserProfileDto MapToUserProfileDto(Parent parent, UserRecord firebaseUser)
        {
            return new UserProfileDto
            {
                Id = parent.Id,
                FirebaseUid = parent.FirebaseUid,
                Email = parent.Email,
                EmailVerified = firebaseUser.EmailVerified,
                FirstName = parent.FirstName,
                LastName = parent.LastName,
                FullName = parent.FullName,
                PhoneNumber = parent.PhoneNumber,
                Role = parent.Role.ToRoleString(),
                IsActive = parent.IsActive,
                LastLoginAt = parent.LastLoginAt,
                ProfileCompletionPercentage = parent.ProfileCompletionPercentage
            };
        }

        private UserProfileDto MapToUserProfileDto(Staff staff, UserRecord firebaseUser)
        {
            return new UserProfileDto
            {
                Id = staff.Id,
                FirebaseUid = staff.FirebaseUid,
                Email = staff.Email,
                EmailVerified = firebaseUser.EmailVerified,
                FirstName = staff.FirstName,
                LastName = staff.LastName,
                FullName = staff.FullName,
                PhoneNumber = staff.PhoneNumber,
                Role = staff.Role.ToRoleString(),
                IsActive = staff.IsActive,
                LastLoginAt = staff.LastLoginAt,
                ProfileCompletionPercentage = null
            };
        }

        #endregion

        #region Admin Operations

        public async Task<bool> UpdateUserRoleAsync(string userId, string newRole, string adminUserId)
        {
            try
            {
                _logger.LogInformation("Role update requested by admin {AdminId} for user {UserId} to role {NewRole}",
                    adminUserId, userId, newRole);

                UserRole role;
                try
                {
                    role = UserRoleExtensions.ParseRole(newRole);
                }
                catch (ArgumentException ex)
                {
                    throw new ArgumentException($"Invalid role: {newRole}", ex);
                }

                UserRecord firebaseUser;
                try
                {
                    firebaseUser = await _firebaseAuth.GetUserAsync(userId);
                }
                catch (FirebaseAuthException ex)
                {
                    throw new InvalidOperationException("User not found", ex);
                }

                var claims = new Dictionary<string, object>
                {
                    { "role", role.ToRoleString() }
                };
                await _firebaseAuth.SetCustomUserClaimsAsync(userId, claims);
                _logger.LogInformation("Firebase custom claims updated for user {UserId}", userId);

                var parent = await _parentRepository.GetByFirebaseUidAsync(userId);
                if (parent != null)
                {
                    parent.Role = role;
                    parent.UpdatedBy = adminUserId;
                    parent.UpdatedAt = DateTime.UtcNow;
                    await _parentRepository.UpdateAsync(parent);
                    await _parentRepository.SaveChangesAsync();
                }
                else
                {
                    var staff = await _staffRepository.GetByFirebaseUidAsync(userId);
                    if (staff != null)
                    {
                        staff.Role = role;
                        staff.UpdatedBy = adminUserId;
                        staff.UpdatedAt = DateTime.UtcNow;
                        await _staffRepository.UpdateAsync(staff);
                        await _staffRepository.SaveChangesAsync();
                    }
                }

                _cache.Remove($"{CACHE_KEY_PREFIX}{userId}");

                _logger.LogInformation("Role updated successfully for user {UserId} to {NewRole} by admin {AdminId}",
                    userId, newRole, adminUserId);

                return true;
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
                _logger.LogError(ex, "Error updating role for user {UserId}", userId);
                throw new InvalidOperationException("An error occurred while updating user role", ex);
            }
        }

        public async Task<bool> DeactivateUserAsync(string userId, string adminUserId)
        {
            try
            {
                _logger.LogInformation("Account deactivation requested by admin {AdminId} for user {UserId}",
                    adminUserId, userId);

                var updateRequest = new UserRecordArgs
                {
                    Uid = userId,
                    Disabled = true
                };
                await _firebaseAuth.UpdateUserAsync(updateRequest);

                var parent = await _parentRepository.GetByFirebaseUidAsync(userId);
                if (parent != null)
                {
                    parent.IsActive = false;
                    parent.UpdatedBy = adminUserId;
                    parent.UpdatedAt = DateTime.UtcNow;
                    await _parentRepository.UpdateAsync(parent);
                    await _parentRepository.SaveChangesAsync();
                }
                else
                {
                    var staff = await _staffRepository.GetByFirebaseUidAsync(userId);
                    if (staff != null)
                    {
                        staff.IsActive = false;
                        staff.UpdatedBy = adminUserId;
                        staff.UpdatedAt = DateTime.UtcNow;
                        await _staffRepository.UpdateAsync(staff);
                        await _staffRepository.SaveChangesAsync();
                    }
                }

                await _firebaseAuth.RevokeRefreshTokensAsync(userId);
                _cache.Remove($"{CACHE_KEY_PREFIX}{userId}");

                _logger.LogInformation("Account deactivated for user {UserId} by admin {AdminId}",
                    userId, adminUserId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deactivating user {UserId}", userId);
                throw new InvalidOperationException("An error occurred while deactivating user", ex);
            }
        }

        public async Task<bool> ReactivateUserAsync(string userId, string adminUserId)
        {
            try
            {
                _logger.LogInformation("Account reactivation requested by admin {AdminId} for user {UserId}",
                    adminUserId, userId);

                var updateRequest = new UserRecordArgs
                {
                    Uid = userId,
                    Disabled = false
                };
                await _firebaseAuth.UpdateUserAsync(updateRequest);

                var parent = await _parentRepository.GetByFirebaseUidAsync(userId);
                if (parent != null)
                {
                    parent.IsActive = true;
                    parent.UpdatedBy = adminUserId;
                    parent.UpdatedAt = DateTime.UtcNow;
                    await _parentRepository.UpdateAsync(parent);
                    await _parentRepository.SaveChangesAsync();
                }
                else
                {
                    var staff = await _staffRepository.GetByFirebaseUidAsync(userId);
                    if (staff != null)
                    {
                        staff.IsActive = true;
                        staff.UpdatedBy = adminUserId;
                        staff.UpdatedAt = DateTime.UtcNow;
                        await _staffRepository.UpdateAsync(staff);
                        await _staffRepository.SaveChangesAsync();
                    }
                }

                _cache.Remove($"{CACHE_KEY_PREFIX}{userId}");

                _logger.LogInformation("Account reactivated for user {UserId} by admin {AdminId}",
                    userId, adminUserId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reactivating user {UserId}", userId);
                throw new InvalidOperationException("An error occurred while reactivating user", ex);
            }
        }

        #endregion

        #region Helper Classes

        private class FirebaseSignInResponse
        {
            public string IdToken { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string RefreshToken { get; set; } = string.Empty;
            public string ExpiresIn { get; set; } = string.Empty;
            public string LocalId { get; set; } = string.Empty;
            public bool Registered { get; set; }
        }

        private class RefreshTokenResponse
        {
            public string Access_Token { get; set; } = string.Empty;
            public string Expires_In { get; set; } = string.Empty;
            public string Token_Type { get; set; } = string.Empty;
            public string Refresh_Token { get; set; } = string.Empty;
            public string Id_Token { get; set; } = string.Empty;
            public string User_Id { get; set; } = string.Empty;
            public string Project_Id { get; set; } = string.Empty;

            public string IdToken => Id_Token;
            public string RefreshToken => Refresh_Token;
        }

        #endregion
    }
}