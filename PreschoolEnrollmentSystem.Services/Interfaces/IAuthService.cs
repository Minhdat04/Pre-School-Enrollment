using System;
using System.Threading.Tasks;
using PreschoolEnrollmentSystem.Core.DTOs.Auth;

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
}