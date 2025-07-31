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
            throw new InvalidOperationException("User not found");
        }

        var userTenant = new UserTenant
        {
            UserId = user.Id,
            TenantId = tenantId,
            Role = Enum.Parse<TenantRole>(request.Role),
            IsActive = true,
            JoinedAt = DateTime.UtcNow
        };

        _context.UserTenants.Add(userTenant);
        await _context.SaveChangesAsync();

        return MapToTeamMemberDto(user, userTenant);
    }

    public async Task<List<TeamMemberDto>> GetTeamMembersAsync(Guid tenantId)
    {
        var userTenants = await _context.UserTenants
            .Include(ut => ut.User)
            .Where(ut => ut.TenantId == tenantId)
            .ToListAsync();

        return userTenants.Select(ut => MapToTeamMemberDto(ut.User, ut)).ToList();
    }

    public async Task<TeamMemberDto?> UpdateTeamMemberAsync(Guid id, UpdateTeamMemberRequest request, Guid tenantId)
    {
        var userTenant = await _context.UserTenants
            .Include(ut => ut.User)
            .FirstOrDefaultAsync(ut => ut.Id == id && ut.TenantId == tenantId);

        if (userTenant == null) return null;

        if (!string.IsNullOrEmpty(request.Role))
            userTenant.Role = Enum.Parse<TenantRole>(request.Role);
        
        if (request.IsActive.HasValue)
            userTenant.IsActive = request.IsActive.Value;

        await _context.SaveChangesAsync();
        return MapToTeamMemberDto(userTenant.User, userTenant);
    }

    public async Task<bool> RemoveTeamMemberAsync(Guid id, Guid tenantId)
    {
        var userTenant = await _context.UserTenants
            .FirstOrDefaultAsync(ut => ut.Id == id && ut.TenantId == tenantId);

        if (userTenant == null) return false;

        _context.UserTenants.Remove(userTenant);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<object>> GetTeamRolesAsync()
    {
        return new List<object>
        {
            new { value = "Owner", label = "Owner" },
            new { value = "Admin", label = "Admin" },
            new { value = "Member", label = "Member" },
            new { value = "Agent", label = "Agent" }
        };
    }

    public async Task<object> GetTeamStatsAsync(Guid tenantId)
    {
        var totalMembers = await _context.UserTenants.CountAsync(ut => ut.TenantId == tenantId);
        var activeMembers = await _context.UserTenants.CountAsync(ut => ut.TenantId == tenantId && ut.IsActive);
        var pendingInvitations = 0;

        return new
        {
            TotalMembers = totalMembers,
            ActiveMembers = activeMembers,
            PendingInvitations = pendingInvitations
        };
    }

    public async Task<object> GetTeamPermissionsAsync()
    {
        return new
        {
            Owner = new[] { "all" },
            Admin = new[] { "manage_team", "manage_settings", "view_analytics" },
            Member = new[] { "view_dashboard", "manage_conversations" },
            Agent = new[] { "handle_conversations", "view_queue" }
        };
    }

    public async Task<bool> ResendInvitationAsync(Guid id, Guid tenantId)
    {
        return true;
    }

    public async Task<bool> CancelInvitationAsync(Guid id, Guid tenantId)
    {
        return await RemoveTeamMemberAsync(id, tenantId);
    }

    public async Task<bool> BulkUpdateMembersAsync(BulkUpdateMembersRequest request, Guid tenantId)
    {
        var userTenants = await _context.UserTenants
            .Where(ut => request.MemberIds.Contains(ut.Id) && ut.TenantId == tenantId)
            .ToListAsync();

        foreach (var userTenant in userTenants)
        {
            if (!string.IsNullOrEmpty(request.Role))
                userTenant.Role = Enum.Parse<TenantRole>(request.Role);
            
            if (request.IsActive.HasValue)
                userTenant.IsActive = request.IsActive.Value;
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<object>> ExportTeamDataAsync(Guid tenantId)
    {
        var userTenants = await _context.UserTenants
            .Include(ut => ut.User)
            .Where(ut => ut.TenantId == tenantId)
            .ToListAsync();

        return userTenants.Select(ut => new
        {
            Email = ut.User.Email,
            FirstName = ut.User.FirstName,
            LastName = ut.User.LastName,
            Role = ut.Role.ToString(),
            IsActive = ut.IsActive,
            JoinedAt = ut.JoinedAt
        }).Cast<object>().ToList();
    }

    private TeamMemberDto MapToTeamMemberDto(User user, UserTenant userTenant)
    {
        return new TeamMemberDto
        {
            Id = userTenant.Id,
            UserId = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Role = userTenant.Role.ToString(),
            IsActive = userTenant.IsActive,
            JoinedAt = userTenant.JoinedAt ?? DateTime.UtcNow
        };
    }
}
