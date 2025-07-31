using Microsoft.EntityFrameworkCore;
using Shared.Application.Common.Interfaces;
using Shared.Domain.Entities;
using TenantManagementService.Models;

namespace TenantManagementService.Services;

public class ChatbotConfigService : IChatbotConfigService
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<ChatbotConfigService> _logger;

    public ChatbotConfigService(IApplicationDbContext context, ILogger<ChatbotConfigService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ChatbotConfigDto> CreateChatbotConfigAsync(CreateChatbotConfigRequest request, Guid tenantId)
    {
        var config = new ChatbotConfig
        {
            Name = request.Name,
            Description = request.Description,
            TenantId = tenantId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.ChatbotConfigs.Add(config);
        await _context.SaveChangesAsync();

        return MapToDto(config);
    }

    public async Task<List<ChatbotConfigDto>> GetChatbotConfigsAsync(Guid tenantId)
    {
        var configs = await _context.ChatbotConfigs
            .Where(c => c.TenantId == tenantId)
            .ToListAsync();

        return configs.Select(MapToDto).ToList();
    }

    public async Task<ChatbotConfigDto?> GetChatbotConfigAsync(Guid id, Guid tenantId)
    {
        var config = await _context.ChatbotConfigs
            .FirstOrDefaultAsync(c => c.Id == id && c.TenantId == tenantId);

        return config != null ? MapToDto(config) : null;
    }

    public async Task<ChatbotConfigDto?> UpdateChatbotConfigAsync(Guid id, UpdateChatbotConfigRequest request, Guid tenantId)
    {
        var config = await _context.ChatbotConfigs
            .FirstOrDefaultAsync(c => c.Id == id && c.TenantId == tenantId);

        if (config == null) return null;

        if (!string.IsNullOrEmpty(request.Name))
            config.Name = request.Name;
        
        if (request.Description != null)
            config.Description = request.Description;
        
        if (request.IsActive.HasValue)
            config.IsActive = request.IsActive.Value;

        config.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return MapToDto(config);
    }

    public async Task<bool> DeleteChatbotConfigAsync(Guid id, Guid tenantId)
    {
        var config = await _context.ChatbotConfigs
            .FirstOrDefaultAsync(c => c.Id == id && c.TenantId == tenantId);

        if (config == null) return false;

        _context.ChatbotConfigs.Remove(config);
        await _context.SaveChangesAsync();
        return true;
    }

    private ChatbotConfigDto MapToDto(ChatbotConfig config)
    {
        return new ChatbotConfigDto
        {
            Id = config.Id,
            Name = config.Name,
            Description = config.Description,
            IsActive = config.IsActive,
            Configuration = new Dictionary<string, object>(),
            CreatedAt = config.CreatedAt
        };
    }
}
