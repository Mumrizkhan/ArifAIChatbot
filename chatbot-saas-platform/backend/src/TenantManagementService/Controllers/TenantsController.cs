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
public class TenantsController : ControllerBase
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<TenantsController> _logger;

    public TenantsController(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        ILogger<TenantsController> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> CreateTenant([FromBody] CreateTenantRequest request)
    {
        try
        {
            if (await _context.Tenants.AnyAsync(t => t.Subdomain == request.Subdomain))
            {
                return BadRequest(new { message = "Subdomain already exists" });
            }

            var tenant = new Tenant
            {
                Name = request.Name,
                Subdomain = request.Subdomain,
                CustomDomain = request.CustomDomain,
                Status = TenantStatus.Trial,
                DatabaseConnectionString = GenerateTenantConnectionString(request.Subdomain),
                TrialEndsAt = DateTime.UtcNow.AddDays(14),
                PrimaryColor = request.PrimaryColor ?? "#3B82F6",
                SecondaryColor = request.SecondaryColor ?? "#64748B",
                DefaultLanguage = request.DefaultLanguage ?? "en",
                IsRtlEnabled = request.IsRtlEnabled
            };

            _context.Tenants.Add(tenant);

            if (_currentUserService.UserId.HasValue)
            {
                var userTenant = new UserTenant
                {
                    UserId = _currentUserService.UserId.Value,
                    TenantId = tenant.Id,
                    Role = TenantRole.Owner,
                    IsActive = true
                };
                _context.UserTenants.Add(userTenant);
            }

            await _context.SaveChangesAsync();

            return Ok(new TenantDto
            {
                Id = tenant.Id,
                Name = tenant.Name,
                Subdomain = tenant.Subdomain,
                CustomDomain = tenant.CustomDomain,
                Status = tenant.Status.ToString(),
                PrimaryColor = tenant.PrimaryColor,
                SecondaryColor = tenant.SecondaryColor,
                DefaultLanguage = tenant.DefaultLanguage,
                IsRtlEnabled = tenant.IsRtlEnabled,
                TrialEndsAt = tenant.TrialEndsAt,
                CreatedAt = tenant.CreatedAt
            });
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
            var query = _context.Tenants.AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(t => t.Name.Contains(search) || 
                                        t.Subdomain.Contains(search) ||
                                        (t.CustomDomain != null && t.CustomDomain.Contains(search)));
            }

            var totalCount = await query.CountAsync();
            var tenants = await query
                .OrderByDescending(t => t.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(t => new TenantDto
                {
                    Id = t.Id,
                    Name = t.Name,
                    Subdomain = t.Subdomain,
                    CustomDomain = t.CustomDomain,
                    Status = t.Status.ToString(),
                    PrimaryColor = t.PrimaryColor,
                    SecondaryColor = t.SecondaryColor,
                    DefaultLanguage = t.DefaultLanguage,
                    IsRtlEnabled = t.IsRtlEnabled,
                    TrialEndsAt = t.TrialEndsAt,
                    CreatedAt = t.CreatedAt,
                    LogoUrl = t.LogoUrl
                })
                .ToListAsync();

            return Ok(new
            {
                data = tenants,
                totalCount,
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
            var userTenant = await _context.UserTenants
                .Include(ut => ut.Tenant)
                .FirstOrDefaultAsync(ut => ut.TenantId == id && ut.UserId == _currentUserService.UserId);

            if (userTenant == null)
            {
                return NotFound(new { message = "Tenant not found" });
            }

            return Ok(new TenantDto
            {
                Id = userTenant.Tenant.Id,
                Name = userTenant.Tenant.Name,
                Subdomain = userTenant.Tenant.Subdomain,
                CustomDomain = userTenant.Tenant.CustomDomain,
                Status = userTenant.Tenant.Status.ToString(),
                PrimaryColor = userTenant.Tenant.PrimaryColor,
                SecondaryColor = userTenant.Tenant.SecondaryColor,
                DefaultLanguage = userTenant.Tenant.DefaultLanguage,
                IsRtlEnabled = userTenant.Tenant.IsRtlEnabled,
                TrialEndsAt = userTenant.Tenant.TrialEndsAt,
                CreatedAt = userTenant.Tenant.CreatedAt,
                Role = userTenant.Role.ToString()
            });
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
            var userTenant = await _context.UserTenants
                .Include(ut => ut.Tenant)
                .FirstOrDefaultAsync(ut => ut.TenantId == id && ut.UserId == _currentUserService.UserId);

            if (userTenant == null || (userTenant.Role != TenantRole.Owner && userTenant.Role != TenantRole.Admin))
            {
                return Forbid();
            }

            var tenant = userTenant.Tenant;
            tenant.Name = request.Name ?? tenant.Name;
            tenant.CustomDomain = request.CustomDomain;
            tenant.PrimaryColor = request.PrimaryColor ?? tenant.PrimaryColor;
            tenant.SecondaryColor = request.SecondaryColor ?? tenant.SecondaryColor;
            tenant.DefaultLanguage = request.DefaultLanguage ?? tenant.DefaultLanguage;
            tenant.IsRtlEnabled = request.IsRtlEnabled;
            tenant.LogoUrl = request.LogoUrl;

            await _context.SaveChangesAsync();

            return Ok(new TenantDto
            {
                Id = tenant.Id,
                Name = tenant.Name,
                Subdomain = tenant.Subdomain,
                CustomDomain = tenant.CustomDomain,
                Status = tenant.Status.ToString(),
                PrimaryColor = tenant.PrimaryColor,
                SecondaryColor = tenant.SecondaryColor,
                DefaultLanguage = tenant.DefaultLanguage,
                IsRtlEnabled = tenant.IsRtlEnabled,
                TrialEndsAt = tenant.TrialEndsAt,
                CreatedAt = tenant.CreatedAt,
                LogoUrl = tenant.LogoUrl
            });
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
            var userTenant = await _context.UserTenants
                .Include(ut => ut.Tenant)
                .FirstOrDefaultAsync(ut => ut.TenantId == id && ut.UserId == _currentUserService.UserId);

            if (userTenant == null)
            {
                return NotFound(new { message = "Tenant not found" });
            }

            var tenant = userTenant.Tenant;
            return Ok(new
            {
                tenant.Id,
                tenant.Name,
                tenant.Subdomain,
                tenant.CustomDomain,
                tenant.PrimaryColor,
                tenant.SecondaryColor,
                tenant.DefaultLanguage,
                tenant.IsRtlEnabled,
                tenant.LogoUrl,
                Status = tenant.Status.ToString(),
                tenant.TrialEndsAt,
                Settings = new
                {
                    AllowFileUploads = true,
                    MaxFileSize = 10485760,
                    AllowedFileTypes = new[] { "image/*", ".pdf", ".doc", ".docx", ".txt" },
                    EnableVoiceMessages = true,
                    EnableTypingIndicators = true,
                    EnableReadReceipts = true,
                    AutoAssignAgents = true,
                    BusinessHours = new
                    {
                        Enabled = false,
                        Timezone = "UTC",
                        Monday = new { Start = "09:00", End = "17:00", Enabled = true },
                        Tuesday = new { Start = "09:00", End = "17:00", Enabled = true },
                        Wednesday = new { Start = "09:00", End = "17:00", Enabled = true },
                        Thursday = new { Start = "09:00", End = "17:00", Enabled = true },
                        Friday = new { Start = "09:00", End = "17:00", Enabled = true },
                        Saturday = new { Start = "09:00", End = "17:00", Enabled = false },
                        Sunday = new { Start = "09:00", End = "17:00", Enabled = false }
                    }
                }
            });
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
            var userTenant = await _context.UserTenants
                .Include(ut => ut.Tenant)
                .FirstOrDefaultAsync(ut => ut.TenantId == id && ut.UserId == _currentUserService.UserId);

            if (userTenant == null || (userTenant.Role != TenantRole.Owner && userTenant.Role != TenantRole.Admin))
            {
                return Forbid();
            }

            var tenant = userTenant.Tenant;
            tenant.Name = request.Name ?? tenant.Name;
            tenant.CustomDomain = request.CustomDomain;
            tenant.PrimaryColor = request.PrimaryColor ?? tenant.PrimaryColor;
            tenant.SecondaryColor = request.SecondaryColor ?? tenant.SecondaryColor;
            tenant.DefaultLanguage = request.DefaultLanguage ?? tenant.DefaultLanguage;
            tenant.IsRtlEnabled = request.IsRtlEnabled;
            tenant.LogoUrl = request.LogoUrl;

            await _context.SaveChangesAsync();

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
            var tenant = await _context.Tenants.FirstOrDefaultAsync(t => t.Id == id);
            if (tenant == null)
            {
                return NotFound(new { message = "Tenant not found" });
            }

            var conversationCount = await _context.Conversations
                .CountAsync(c => c.TenantId == id);
            
            var messageCount = await _context.Messages
                .CountAsync(m => m.TenantId == id);
            
            var userCount = await _context.UserTenants
                .CountAsync(ut => ut.TenantId == id && ut.IsActive);

            var stats = new
            {
                TotalConversations = conversationCount,
                TotalMessages = messageCount,
                TotalUsers = userCount,
                Status = tenant.Status.ToString(),
                CreatedAt = tenant.CreatedAt,
                TrialEndsAt = tenant.TrialEndsAt
            };

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
            var tenant = await _context.Tenants.FirstOrDefaultAsync(t => t.Id == id);
            if (tenant == null)
            {
                return NotFound(new { message = "Tenant not found" });
            }

            tenant.Status = TenantStatus.Inactive;
            await _context.SaveChangesAsync();

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

            var logoUrl = $"/uploads/tenants/{tenantId}_{Path.GetFileName(logo.FileName)}";
            
            var tenant = await _context.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId);
            if (tenant != null)
            {
                tenant.LogoUrl = logoUrl;
                await _context.SaveChangesAsync();
            }

            return Ok(new { logo = logoUrl });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading tenant logo");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }
}

public class CreateTenantRequest
{
    public string Name { get; set; } = string.Empty;
    public string Subdomain { get; set; } = string.Empty;
    public string? CustomDomain { get; set; }
    public string? PrimaryColor { get; set; }
    public string? SecondaryColor { get; set; }
    public string? DefaultLanguage { get; set; }
    public bool IsRtlEnabled { get; set; } = false;
}

public class UpdateTenantRequest
{
    public string? Name { get; set; }
    public string? CustomDomain { get; set; }
    public string? PrimaryColor { get; set; }
    public string? SecondaryColor { get; set; }
    public string? DefaultLanguage { get; set; }
    public bool IsRtlEnabled { get; set; } = false;
    public string? LogoUrl { get; set; }
}

public class TenantDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Subdomain { get; set; } = string.Empty;
    public string? CustomDomain { get; set; }
    public string Status { get; set; } = string.Empty;
    public string PrimaryColor { get; set; } = string.Empty;
    public string SecondaryColor { get; set; } = string.Empty;
    public string DefaultLanguage { get; set; } = string.Empty;
    public bool IsRtlEnabled { get; set; }
    public DateTime? TrialEndsAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? LogoUrl { get; set; }
    public string? Role { get; set; }
}

public class UpdateTenantSettingsRequest
{
    public string? Name { get; set; }
    public string? CustomDomain { get; set; }
    public string? PrimaryColor { get; set; }
    public string? SecondaryColor { get; set; }
    public string? DefaultLanguage { get; set; }
    public bool IsRtlEnabled { get; set; }
    public string? LogoUrl { get; set; }
}
