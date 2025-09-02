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
            var userId = _currentUserService.UserId;
            var userEmail = _currentUserService.Email;
            var userRole = _currentUserService.Role;
            var isAuthenticated = _currentUserService.IsAuthenticated;

            _logger.LogInformation("SystemSettings access attempt - UserId: {UserId}, Email: {Email}, Role: {Role}, IsAuthenticated: {IsAuthenticated}", 
                userId, userEmail, userRole, isAuthenticated);

            if (!isAuthenticated)
            {
                _logger.LogWarning("Unauthenticated access attempt to SystemSettings");
                return Unauthorized("User is not authenticated");
            }

            if (string.IsNullOrEmpty(userRole))
            {
                _logger.LogWarning("User {UserId} ({Email}) has no role assigned", userId, userEmail);
                return Forbid("No role assigned to user");
            }

            if (!userRole.Equals("SuperAdmin", StringComparison.OrdinalIgnoreCase) &&
                !userRole.Equals("Admin", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("User {UserId} ({Email}) with role {Role} attempted to access SystemSettings", userId, userEmail, userRole);
                return Forbid($"Only system administrators can access system settings. Current role: {userRole}");
            }

            _logger.LogInformation("Authorized user {UserId} ({Email}) with role {Role} accessing SystemSettings", userId, userEmail, userRole);
            var settings = await _systemSettingsService.GetSystemSettingsAsync();
            return Ok(settings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting system settings for user {UserId}", _currentUserService.UserId);
            return StatusCode(500, new { message = "Internal server error", details = ex.Message });
        }
    }

    [HttpGet("debug-user")]
    public IActionResult DebugCurrentUser()
    {
        try
        {
            var claims = HttpContext.User.Claims.Select(c => new { c.Type, c.Value }).ToList();
            
            return Ok(new
            {
                UserId = _currentUserService.UserId,
                Email = _currentUserService.Email,
                Role = _currentUserService.Role,
                TenantId = _currentUserService.TenantId,
                IsAuthenticated = _currentUserService.IsAuthenticated,
                Claims = claims
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error debugging current user");
            return StatusCode(500, new { message = "Internal server error", details = ex.Message });
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
