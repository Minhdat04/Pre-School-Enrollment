using System;
using System.Threading.Tasks;
using PreschoolEnrollmentSystem.API.DTOs.Auth;

namespace PreschoolEnrollmentSystem.Services.Interfaces
{
    public interface IAuthService
    {
        Task<LoginResponseDto> RegisterAsync(RegisterRequestDto request);
        Task<LoginResponseDto> LoginAsync(LoginRequestDto request);
        Task<bool> SendPasswordResetEmailAsync(PasswordResetDto request);
        Task<bool> ConfirmPasswordResetAsync(ConfirmPasswordResetDto request);
        Task<bool> ChangePasswordAsync(string userId, ChangePasswordDto request);
        Task<bool> SendEmailVerificationAsync(string email);
        Task<bool> VerifyEmailAsync(VerifyEmailDto request);
        Task<LoginResponseDto> RefreshTokenAsync(string refreshToken);
        Task<bool> LogoutAsync(string userId);
        Task<FirebaseTokenInfo> VerifyIdTokenAsync(string idToken);
        Task<UserProfileDto> GetUserProfileAsync(string firebaseUid);
        Task<bool> UpdateUserRoleAsync(string userId, string newRole, string adminUserId);
        Task<bool> DeactivateUserAsync(string userId, string adminUserId);
        Task<bool> ReactivateUserAsync(string userId, string adminUserId);
    }
    public class FirebaseTokenInfo
    {
        public string Uid { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool EmailVerified { get; set; }
        public string Role { get; set; } = string.Empty;
        public DateTime IssuedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
    }
    public class UserProfileDto
    {
        public Guid Id { get; set; }
        public string FirebaseUid { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool EmailVerified { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public int? ProfileCompletionPercentage { get; set; }
    }
}