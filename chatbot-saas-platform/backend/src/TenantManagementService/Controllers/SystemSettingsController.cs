using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Application.Common.Interfaces;
using TenantManagementService.Models;
using TenantManagementService.Services;

namespace TenantManagementService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SystemSettingsController : ControllerBase
{
    private readonly ISystemSettingsService _systemSettingsService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<SystemSettingsController> _logger;

    public SystemSettingsController(
        ISystemSettingsService systemSettingsService,
        ICurrentUserService currentUserService,
        ILogger<SystemSettingsController> logger)
    {
        _systemSettingsService = systemSettingsService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetSystemSettings()
    {
        try
        {
            var userRole = _currentUserService.Role;
            if (!userRole.Equals("SuperAdmin", StringComparison.OrdinalIgnoreCase) &&
                !userRole.Equals("Admin", StringComparison.OrdinalIgnoreCase))
            {
                return Forbid("Only system administrators can access system settings");
            }

            var settings = await _systemSettingsService.GetSystemSettingsAsync();
            return Ok(settings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting system settings");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPut]
    public async Task<IActionResult> UpdateSystemSettings([FromBody] UpdateSystemSettingsRequest request)
    {
        try
        {
            var userRole = _currentUserService.Role;
            if (!userRole.Equals("SuperAdmin", StringComparison.OrdinalIgnoreCase) &&
                !userRole.Equals("Admin", StringComparison.OrdinalIgnoreCase))
            {
                return Forbid("Only system administrators can update system settings");
            }

            var updated = await _systemSettingsService.UpdateSystemSettingsAsync(request, _currentUserService.UserId?.ToString());
            if (!updated)
            {
                return BadRequest(new { message = "Failed to update system settings" });
            }

            return Ok(new { message = "System settings updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating system settings");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }
}
