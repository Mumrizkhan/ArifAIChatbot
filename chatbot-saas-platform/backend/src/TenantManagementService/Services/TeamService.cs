using Microsoft.EntityFrameworkCore;
using Shared.Application.Common.Interfaces;
using Shared.Domain.Entities;
using TenantManagementService.Models;

namespace TenantManagementService.Services;

public class TeamService : ITeamService
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<TeamService> _logger;

    public TeamService(IApplicationDbContext context, ILogger<TeamService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<TeamMemberDto> AddTeamMemberAsync(AddTeamMemberRequest request, Guid tenantId)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (user == null)
        {
            user = new User
            {
                Email = request.Email,
                FirstName = "",
                LastName = "",
                TenantId = tenantId,
                Role = request.Role,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
        }

        return MapToDto(user);
    }

    public async Task<List<TeamMemberDto>> GetTeamMembersAsync(Guid tenantId)
    {
        var users = await _context.Users
            .Where(u => u.TenantId == tenantId)
            .ToListAsync();

        return users.Select(MapToDto).ToList();
    }

    public async Task<TeamMemberDto?> UpdateTeamMemberAsync(Guid id, UpdateTeamMemberRequest request, Guid tenantId)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == id && u.TenantId == tenantId);

        if (user == null) return null;

        if (!string.IsNullOrEmpty(request.Role))
            user.Role = request.Role;
        
        if (request.IsActive.HasValue)
            user.IsActive = request.IsActive.Value;

        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return MapToDto(user);
    }

    public async Task<bool> RemoveTeamMemberAsync(Guid id, Guid tenantId)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == id && u.TenantId == tenantId);

        if (user == null) return false;

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();
        return true;
    }

    private TeamMemberDto MapToDto(User user)
    {
        return new TeamMemberDto
        {
            Id = user.Id,
            UserId = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Role = user.Role,
            IsActive = user.IsActive,
            JoinedAt = user.CreatedAt
        };
    }
}
