using Microsoft.EntityFrameworkCore;
using Shared.Application.Common.Interfaces;
using Shared.Domain.Entities;
using Shared.Domain.Enums;
using TenantManagementService.Models;

namespace TenantManagementService.Services;

public class TenantManagementService : ITenantManagementService
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<TenantManagementService> _logger;

    public TenantManagementService(IApplicationDbContext context, ILogger<TenantManagementService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<TenantDto> CreateTenantAsync(CreateTenantRequest request, Guid? userId)
    {
        if (await _context.Tenants.AnyAsync(t => t.Subdomain == request.Subdomain))
        {
            throw new InvalidOperationException("Subdomain already exists");
        }

        var tenant = new Tenant
        {
            Name = request.Name,
            Subdomain = request.Subdomain,
            CustomDomain = request.CustomDomain,
            Status = TenantStatus.Trial,
            DatabaseConnectionString = GenerateTenantConnectionString(request.Subdomain),
            PrimaryColor = request.PrimaryColor ?? "#3B82F6",
            SecondaryColor = request.SecondaryColor ?? "#64748B",
            DefaultLanguage = request.DefaultLanguage ?? "en",
            IsRtlEnabled = request.IsRtlEnabled,
            TrialEndsAt = DateTime.UtcNow.AddDays(14),
            CreatedAt = DateTime.UtcNow
        };

        _context.Tenants.Add(tenant);

        if (userId.HasValue)
        {
            var userTenant = new UserTenant
            {
                UserId = userId.Value,
                TenantId = tenant.Id,
                Role = TenantRole.Owner,
                IsActive = true
            };
            _context.UserTenants.Add(userTenant);
        }

        await _context.SaveChangesAsync();
        return MapToDto(tenant);
    }

    private string GenerateTenantConnectionString(string subdomain)
    {
        return $"Server=localhost,1433;Database=ArifPlatform_{subdomain};User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=true;";
    }

    public async Task<PaginatedResult<TenantDto>> GetTenantsAsync(int page, int pageSize, string? search)
    {
        var query = _context.Tenants.AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(t => t.Name.Contains(search) || 
                                   (t.Domain != null && t.Domain.Contains(search)));
        }

        var totalCount = await query.CountAsync();
        var tenants = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PaginatedResult<TenantDto>
        {
            Items = tenants.Select(MapToDto).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<TenantDto?> GetTenantAsync(Guid id, Guid? userId)
    {
        var tenant = await _context.Tenants.FindAsync(id);
        return tenant != null ? MapToDto(tenant) : null;
    }

    public async Task<TenantDto?> UpdateTenantAsync(Guid id, UpdateTenantRequest request, Guid? userId)
    {
        var tenant = await _context.Tenants.FindAsync(id);
        if (tenant == null) return null;

        if (!string.IsNullOrEmpty(request.Name))
            tenant.Name = request.Name;
        
        if (request.CustomDomain != null)
            tenant.CustomDomain = request.CustomDomain;
        
        if (request.LogoUrl != null)
            tenant.LogoUrl = request.LogoUrl;
        
        if (request.IsActive.HasValue)
            tenant.IsActive = request.IsActive.Value;

        tenant.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return MapToDto(tenant);
    }

    public async Task<TenantSettingsDto?> GetTenantSettingsAsync(Guid id, Guid? userId)
    {
        var tenant = await _context.Tenants.FindAsync(id);
        if (tenant == null) return null;

        return new TenantSettingsDto
        {
            TenantId = tenant.Id,
            Settings = new Dictionary<string, object>()
        };
    }

    public async Task<bool> UpdateTenantSettingsAsync(Guid id, UpdateTenantSettingsRequest request, Guid? userId)
    {
        var tenant = await _context.Tenants.FindAsync(id);
        if (tenant == null) return false;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<TenantStatsDto?> GetTenantStatsAsync(Guid id)
    {
        var tenant = await _context.Tenants.FindAsync(id);
        if (tenant == null) return null;

        var totalUsers = await _context.Users.CountAsync(u => u.TenantId == id);
        var totalConversations = await _context.Conversations.CountAsync(c => c.TenantId == id);
        var totalMessages = await _context.Messages
            .CountAsync(m => m.Conversation.TenantId == id);
        var activeChatbots = await _context.ChatbotConfigs
            .CountAsync(c => c.TenantId == id && c.IsActive);

        return new TenantStatsDto
        {
            TotalUsers = totalUsers,
            TotalConversations = totalConversations,
            TotalMessages = totalMessages,
            ActiveChatbots = activeChatbots
        };
    }

    public async Task<bool> DeleteTenantAsync(Guid id)
    {
        var tenant = await _context.Tenants.FindAsync(id);
        if (tenant == null) return false;

        _context.Tenants.Remove(tenant);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<string> UploadTenantLogoAsync(Guid? tenantId, IFormFile logo)
    {
        if (!tenantId.HasValue) throw new ArgumentException("Tenant ID is required");

        var fileName = $"{tenantId.Value}_{Guid.NewGuid()}{Path.GetExtension(logo.FileName)}";
        var filePath = Path.Combine("uploads", "logos", fileName);
        
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
        
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await logo.CopyToAsync(stream);
        }

        var logoUrl = $"/uploads/logos/{fileName}";
        
        var tenant = await _context.Tenants.FindAsync(tenantId.Value);
        if (tenant != null)
        {
            tenant.LogoUrl = logoUrl;
            await _context.SaveChangesAsync();
        }

        return logoUrl;
    }

    public async Task<TenantDto?> GetTenantByUserAsync(Guid tenantId, Guid userId)
    {
        var userTenant = await _context.UserTenants
            .Include(ut => ut.Tenant)
            .FirstOrDefaultAsync(ut => ut.TenantId == tenantId && ut.UserId == userId);

        if (userTenant == null) return null;

        var tenantDto = MapToDto(userTenant.Tenant);
        tenantDto.Role = userTenant.Role.ToString();
        return tenantDto;
    }

    public async Task<TenantDto?> UpdateTenantByUserAsync(Guid tenantId, UpdateTenantRequest request, Guid userId)
    {
        var userTenant = await _context.UserTenants
            .Include(ut => ut.Tenant)
            .FirstOrDefaultAsync(ut => ut.TenantId == tenantId && ut.UserId == userId);

        if (userTenant == null || (userTenant.Role != TenantRole.Owner && userTenant.Role != TenantRole.Admin))
        {
            return null;
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
        return MapToDto(tenant);
    }

    private TenantDto MapToDto(Tenant tenant)
    {
        return new TenantDto
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
        };
    }
}
