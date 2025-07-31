using Microsoft.EntityFrameworkCore;
using Shared.Application.Common.Interfaces;
using Shared.Domain.Entities;
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
        var tenant = new Tenant
        {
            Name = request.Name,
            Description = request.Description,
            LogoUrl = request.LogoUrl,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Tenants.Add(tenant);
        await _context.SaveChangesAsync();

        return MapToDto(tenant);
    }

    public async Task<PaginatedResult<TenantDto>> GetTenantsAsync(int page, int pageSize, string? search)
    {
        var query = _context.Tenants.AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(t => t.Name.Contains(search) || 
                                   (t.Description != null && t.Description.Contains(search)));
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
        
        if (request.Description != null)
            tenant.Description = request.Description;
        
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

    private TenantDto MapToDto(Tenant tenant)
    {
        return new TenantDto
        {
            Id = tenant.Id,
            Name = tenant.Name,
            Description = tenant.Description,
            LogoUrl = tenant.LogoUrl,
            IsActive = tenant.IsActive,
            CreatedAt = tenant.CreatedAt,
            UpdatedAt = tenant.UpdatedAt
        };
    }
}
