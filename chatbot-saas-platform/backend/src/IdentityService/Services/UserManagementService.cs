using Microsoft.EntityFrameworkCore;
using Shared.Application.Common.Interfaces;
using Shared.Domain.Entities;
using Shared.Domain.Enums;
using IdentityService.Models;

namespace IdentityService.Services;

public class UserManagementService : IUserManagementService
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<UserManagementService> _logger;

    public UserManagementService(IApplicationDbContext context, ILogger<UserManagementService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<PaginatedResult<UserDto>> GetUsersAsync(int page, int pageSize, string? search)
    {
        var query = _context.Users.AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(u => u.Email.Contains(search) || 
                                    u.FirstName.Contains(search) || 
                                    u.LastName.Contains(search));
        }

        var totalCount = await query.CountAsync();
        var users = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new UserDto
            {
                Id = u.Id,
                Email = u.Email,
                FirstName = u.FirstName,
                LastName = u.LastName,
                Role = u.Role.ToString(),
                IsActive = u.IsActive,
                CreatedAt = u.CreatedAt,
                LastLoginAt = u.LastLoginAt,
                PreferredLanguage = u.PreferredLanguage
            })
            .ToListAsync();

        return new PaginatedResult<UserDto>
        {
            Data = users,
            TotalCount = totalCount,
            CurrentPage = page,
            PageSize = pageSize
        };
    }

    public async Task<User?> GetUserAsync(Guid id)
    {
        return await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
    }

    public async Task<UserDto> CreateUserAsync(CreateUserRequest request)
    {
        if (await _context.Users.AnyAsync(u => u.Email == request.Email))
        {
            throw new InvalidOperationException("Email already exists");
        }

        var user = new User
        {
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Role = Enum.Parse<UserRole>(request.Role),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Role = user.Role.ToString(),
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt
        };
    }

    public async Task<bool> UpdateUserAsync(Guid id, UpdateUserRequest request)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (user == null)
        {
            return false;
        }

        user.FirstName = request.FirstName ?? user.FirstName;
        user.LastName = request.LastName ?? user.LastName;
        user.IsActive = request.IsActive ?? user.IsActive;
        
        if (!string.IsNullOrEmpty(request.Role))
        {
            user.Role = Enum.Parse<UserRole>(request.Role);
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteUserAsync(Guid id)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (user == null)
        {
            return false;
        }

        user.IsActive = false;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> AssignRoleAsync(Guid userId, string role)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
        {
            return false;
        }

        user.Role = Enum.Parse<UserRole>(role);
        await _context.SaveChangesAsync();
        return true;
    }
}
