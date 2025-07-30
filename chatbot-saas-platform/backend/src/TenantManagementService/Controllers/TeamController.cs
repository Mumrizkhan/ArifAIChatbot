using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared.Application.Common.Interfaces;
using Shared.Domain.Entities;
using Shared.Domain.Enums;

namespace TenantManagementService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TeamController : ControllerBase
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<TeamController> _logger;

    public TeamController(
        IApplicationDbContext context,
        ITenantService tenantService,
        ICurrentUserService currentUserService,
        ILogger<TeamController> logger)
    {
        _context = context;
        _tenantService = tenantService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    [HttpGet("members")]
    public async Task<IActionResult> GetTeamMembers()
    {
        try
        {
            var tenantId = _tenantService.GetCurrentTenantId();
            var members = await _context.UserTenants
                .Include(ut => ut.User)
                .Where(ut => ut.TenantId == tenantId && ut.IsActive)
                .Select(ut => new
                {
                    Id = ut.User.Id,
                    Email = ut.User.Email,
                    FirstName = ut.User.FirstName,
                    LastName = ut.User.LastName,
                    Role = ut.Role.ToString(),
                    IsActive = ut.IsActive,
                    JoinedAt = ut.CreatedAt,
                    LastLoginAt = ut.User.LastLoginAt
                })
                .ToListAsync();

            return Ok(members);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting team members");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost("invite")]
    public async Task<IActionResult> InviteTeamMember([FromBody] InviteTeamMemberRequest request)
    {
        try
        {
            var tenantId = _tenantService.GetCurrentTenantId();
            
            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            
            if (existingUser != null)
            {
                var existingMembership = await _context.UserTenants
                    .FirstOrDefaultAsync(ut => ut.UserId == existingUser.Id && ut.TenantId == tenantId);
                
                if (existingMembership != null)
                {
                    return BadRequest(new { message = "User is already a member of this tenant" });
                }

                var userTenant = new UserTenant
                {
                    UserId = existingUser.Id,
                    TenantId = tenantId,
                    Role = Enum.Parse<TenantRole>(request.Role),
                    IsActive = true
                };
                _context.UserTenants.Add(userTenant);
            }
            else
            {
                var newUser = new User
                {
                    Email = request.Email,
                    FirstName = "",
                    LastName = "",
                    PasswordHash = "",
                    Role = UserRole.User,
                    PreferredLanguage = "en",
                    IsActive = false
                };
                _context.Users.Add(newUser);
                await _context.SaveChangesAsync();

                var userTenant = new UserTenant
                {
                    UserId = newUser.Id,
                    TenantId = tenantId,
                    Role = Enum.Parse<TenantRole>(request.Role),
                    IsActive = true
                };
                _context.UserTenants.Add(userTenant);
            }

            await _context.SaveChangesAsync();
            
            return Ok(new { message = "Team member invited successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inviting team member");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPut("members/{id}")]
    public async Task<IActionResult> UpdateTeamMember(Guid id, [FromBody] UpdateTeamMemberRequest request)
    {
        try
        {
            var tenantId = _tenantService.GetCurrentTenantId();
            var userTenant = await _context.UserTenants
                .FirstOrDefaultAsync(ut => ut.UserId == id && ut.TenantId == tenantId);

            if (userTenant == null)
            {
                return NotFound(new { message = "Team member not found" });
            }

            if (!string.IsNullOrEmpty(request.Role))
            {
                userTenant.Role = Enum.Parse<TenantRole>(request.Role);
            }

            if (request.IsActive.HasValue)
            {
                userTenant.IsActive = request.IsActive.Value;
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = "Team member updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating team member");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpDelete("members/{id}")]
    public async Task<IActionResult> RemoveTeamMember(Guid id)
    {
        try
        {
            var tenantId = _tenantService.GetCurrentTenantId();
            var userTenant = await _context.UserTenants
                .FirstOrDefaultAsync(ut => ut.UserId == id && ut.TenantId == tenantId);

            if (userTenant == null)
            {
                return NotFound(new { message = "Team member not found" });
            }

            if (userTenant.Role == TenantRole.Owner)
            {
                return BadRequest(new { message = "Cannot remove tenant owner" });
            }

            _context.UserTenants.Remove(userTenant);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Team member removed successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing team member");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }
}

public class InviteTeamMemberRequest
{
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}

public class UpdateTeamMemberRequest
{
    public string? Role { get; set; }
    public bool? IsActive { get; set; }
}
