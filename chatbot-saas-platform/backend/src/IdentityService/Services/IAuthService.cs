using IdentityService.Models;
using Shared.Domain.Entities;

namespace IdentityService.Services;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request);
    Task<AuthResponse> LoginAsync(LoginRequest request);
    Task<AuthResponse> RefreshTokenAsync();
    Task<string> RefreshTokenAsync(string refreshToken);
    Task SendPasswordResetEmailAsync(string email);
    Task<bool> ResetPasswordAsync(string token, string newPassword);
    Task<bool> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword);
    Task<User?> GetCurrentUserAsync(Guid userId);
    Task<bool> UpdateProfileAsync(Guid userId, UpdateProfileRequest request);
    Task<string> UploadAvatarAsync(Guid userId, IFormFile avatar);
}
