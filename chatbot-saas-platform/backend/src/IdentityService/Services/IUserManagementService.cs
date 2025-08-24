using IdentityService.Models;
using Shared.Domain.Entities;

namespace IdentityService.Services;

public interface IUserManagementService
{
    Task<PaginatedResult<UserDto>> GetUsersAsync(int page, int pageSize, string? search);
    Task<User?> GetUserAsync(Guid id);
    Task<UserDto> CreateUserAsync(CreateUserRequest request);
    Task<bool> UpdateUserAsync(Guid id, UpdateUserRequest request);
    Task<bool> DeleteUserAsync(Guid id);
    Task<bool> AssignRoleAsync(Guid userId, string role);
}

public class PaginatedResult<T>
{
    public List<T> Data { get; set; } = new();
    public int TotalCount { get; set; }
    public int CurrentPage { get; set; }
    public int PageSize { get; set; }
}

public class UserDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public string PreferredLanguage { get; set; } = string.Empty;
    public string? PhoneNumber { get; internal set; }
    public string AvatarUrl { get; internal set; }
    public Guid TenantId { get; set; }
}

public class CreateUserRequest
{
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Role { get; set; } = "User";
}

public class UpdateUserRequest
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Role { get; set; }
    public bool? IsActive { get; set; }
}
