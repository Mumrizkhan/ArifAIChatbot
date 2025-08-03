using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TenantManagementService.Services;
using TenantManagementService.Models;
using Shared.Infrastructure.Services;
using Shared.Application.Common.Interfaces;

namespace TenantManagementService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TenantsController : ControllerBase
{
    private readonly ITenantManagementService _tenantManagementService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<TenantsController> _logger;

    public TenantsController(
        ITenantManagementService tenantManagementService,
        ICurrentUserService currentUserService,
        ILogger<TenantsController> logger)
    {
        _tenantManagementService = tenantManagementService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> CreateTenant([FromBody] CreateTenantRequest request)
    {
        try
        {
            var tenant = await _tenantManagementService.CreateTenantAsync(request, _currentUserService.UserId);
            return Ok(tenant);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating tenant");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetTenants([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? search = null)
    {
        try
        {
            var result = await _tenantManagementService.GetTenantsAsync(page, pageSize, search);
            return Ok(new
            {
                data = result.Items,
                totalCount = result.TotalCount,
                currentPage = page,
                pageSize
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving tenants");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetTenant(Guid id)
    {
        try
        {
            if (!_currentUserService.UserId.HasValue)
            {
                return Unauthorized();
            }

            var tenant = await _tenantManagementService.GetTenantByUserAsync(id, _currentUserService.UserId.Value);
            if (tenant == null)
            {
                return NotFound(new { message = "Tenant not found" });
            }

            return Ok(tenant);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving tenant");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateTenant(Guid id, [FromBody] UpdateTenantRequest request)
    {
        try
        {
            if (!_currentUserService.UserId.HasValue)
            {
                return Unauthorized();
            }

            var tenant = await _tenantManagementService.UpdateTenantByUserAsync(id, request, _currentUserService.UserId.Value);
            if (tenant == null)
            {
                return Forbid();
            }

            return Ok(tenant);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating tenant");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    private string GenerateTenantConnectionString(string subdomain)
    {
        return $"Server=localhost,1433;Database=ArifPlatform_{subdomain};User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=true;";
    }

    [HttpGet("{id}/settings")]
    public async Task<IActionResult> GetTenantSettings(Guid id)
    {
        try
        {
            if (!_currentUserService.UserId.HasValue)
            {
                return Unauthorized();
            }

            var settings = await _tenantManagementService.GetTenantSettingsAsync(id, _currentUserService.UserId.Value);
            if (settings == null)
            {
                return NotFound(new { message = "Tenant not found" });
            }

            return Ok(settings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting tenant settings");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPut("{id}/settings")]
    public async Task<IActionResult> UpdateTenantSettings(Guid id, [FromBody] UpdateTenantSettingsRequest request)
    {
        try
        {
            if (!_currentUserService.UserId.HasValue)
            {
                return Unauthorized();
            }

            var updated = await _tenantManagementService.UpdateTenantSettingsAsync(id, request, _currentUserService.UserId.Value);
            if (!updated)
            {
                return Forbid();
            }

            return Ok(new { message = "Tenant settings updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating tenant settings");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("{id}/stats")]
    public async Task<IActionResult> GetTenantStats(Guid id)
    {
        try
        {
            var stats = await _tenantManagementService.GetTenantStatsAsync(id);
            if (stats == null)
            {
                return NotFound(new { message = "Tenant not found" });
            }

            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting tenant stats for {TenantId}", id);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTenant(Guid id)
    {
        try
        {
            var deleted = await _tenantManagementService.DeleteTenantAsync(id);
            if (!deleted)
            {
                return NotFound(new { message = "Tenant not found" });
            }

            return Ok(new { message = "Tenant deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting tenant {TenantId}", id);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost("logo")]
    public async Task<IActionResult> UploadTenantLogo([FromForm] IFormFile logo)
    {
        try
        {
            var tenantId = _currentUserService.TenantId;
            if (logo == null || logo.Length == 0)
            {
                return BadRequest(new { message = "No file provided" });
            }

            var logoUrl = await _tenantManagementService.UploadTenantLogoAsync(tenantId, logo);
            return Ok(new { logo = logoUrl });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading tenant logo");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }
}
