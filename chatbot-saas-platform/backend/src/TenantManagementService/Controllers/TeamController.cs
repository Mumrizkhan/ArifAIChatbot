using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TenantManagementService.Services;
using TenantManagementService.Models;
using Shared.Infrastructure.Services;

namespace TenantManagementService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TeamController : ControllerBase
{
    private readonly ITeamService _teamService;
    private readonly ITenantService _tenantService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<TeamController> _logger;

    public TeamController(
        ITeamService teamService,
        ITenantService tenantService,
        ICurrentUserService currentUserService,
        ILogger<TeamController> logger)
    {
        _teamService = teamService;
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
            var members = await _teamService.GetTeamMembersAsync(tenantId);
            return Ok(members);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting team members");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost("invite")]
    public async Task<IActionResult> InviteTeamMember([FromBody] AddTeamMemberRequest request)
    {
        try
        {
            var tenantId = _tenantService.GetCurrentTenantId();
            var member = await _teamService.AddTeamMemberAsync(request, tenantId);
            return Ok(new { message = "Team member invited successfully", member });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
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
            var member = await _teamService.UpdateTeamMemberAsync(id, request, tenantId);

            if (member == null)
            {
                return NotFound(new { message = "Team member not found" });
            }

            return Ok(new { message = "Team member updated successfully", member });
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
            var removed = await _teamService.RemoveTeamMemberAsync(id, tenantId);

            if (!removed)
            {
                return NotFound(new { message = "Team member not found" });
            }

            return Ok(new { message = "Team member removed successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing team member");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("roles")]
    public async Task<IActionResult> GetTeamRoles()
    {
        try
        {
            var roles = await _teamService.GetTeamRolesAsync();
            return Ok(roles);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting team roles");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetTeamStats()
    {
        try
        {
            var tenantId = _tenantService.GetCurrentTenantId();
            var stats = await _teamService.GetTeamStatsAsync(tenantId);
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting team stats");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("permissions")]
    public async Task<IActionResult> GetTeamPermissions()
    {
        try
        {
            var permissions = await _teamService.GetTeamPermissionsAsync();
            return Ok(permissions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting team permissions");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost("members/{id}/resend-invite")]
    public async Task<IActionResult> ResendInvitation(Guid id)
    {
        try
        {
            var tenantId = _tenantService.GetCurrentTenantId();
            var success = await _teamService.ResendInvitationAsync(id, tenantId);

            if (!success)
            {
                return NotFound(new { message = "Team member not found" });
            }

            return Ok(new { message = "Invitation resent successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resending invitation");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpDelete("invites/{id}")]
    public async Task<IActionResult> CancelInvitation(Guid id)
    {
        try
        {
            var tenantId = _tenantService.GetCurrentTenantId();
            var success = await _teamService.CancelInvitationAsync(id, tenantId);

            if (!success)
            {
                return NotFound(new { message = "Invitation not found" });
            }

            return Ok(new { message = "Invitation cancelled successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling invitation");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPut("members/bulk")]
    public async Task<IActionResult> BulkUpdateMembers([FromBody] BulkUpdateMembersRequest request)
    {
        try
        {
            var tenantId = _tenantService.GetCurrentTenantId();
            var success = await _teamService.BulkUpdateMembersAsync(request, tenantId);

            if (!success)
            {
                return BadRequest(new { message = "Failed to update members" });
            }

            return Ok(new { message = "Members updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bulk updating members");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("export")]
    public async Task<IActionResult> ExportTeamData()
    {
        try
        {
            var tenantId = _tenantService.GetCurrentTenantId();
            var data = await _teamService.ExportTeamDataAsync(tenantId);
            return Ok(data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting team data");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

}
