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
            Configuration = request.Configuration,
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
        
        if (request.Configuration != null)
            config.Configuration = request.Configuration;

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

    public async Task<string> UploadAvatarAsync(Guid configId, IFormFile avatar, Guid tenantId)
    {
        var config = await _context.ChatbotConfigs
            .FirstOrDefaultAsync(c => c.Id == configId && c.TenantId == tenantId);

        if (config == null) throw new InvalidOperationException("Chatbot config not found");

        var fileName = $"{configId}_{Guid.NewGuid()}{Path.GetExtension(avatar.FileName)}";
        var filePath = Path.Combine("uploads", "avatars", fileName);
        
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
        
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await avatar.CopyToAsync(stream);
        }

        var avatarUrl = $"/uploads/avatars/{fileName}";
        
        var configuration = config.Configuration ?? new Dictionary<string, object>();
        configuration["avatarUrl"] = avatarUrl;
        config.Configuration = configuration;
        
        await _context.SaveChangesAsync();
        return avatarUrl;
    }

    public async Task<object> GetAnalyticsAsync(Guid configId, Guid tenantId)
    {
        var conversationCount = await _context.Conversations
            .CountAsync(c => c.ChatbotConfigId == configId && c.TenantId == tenantId);
        
        var messageCount = await _context.Messages
            .CountAsync(m => m.Conversation.ChatbotConfigId == configId && m.Conversation.TenantId == tenantId);

        return new
        {
            TotalConversations = conversationCount,
            TotalMessages = messageCount,
            AverageMessagesPerConversation = conversationCount > 0 ? (double)messageCount / conversationCount : 0
        };
    }

    public async Task<object> GetTrainingDataAsync(Guid configId, Guid tenantId)
    {
        return new
        {
            TotalDocuments = 0,
            TotalQuestions = 0,
            LastTrainingDate = (DateTime?)null
        };
    }

    public async Task<bool> TrainChatbotAsync(Guid configId, Guid tenantId)
    {
        var config = await _context.ChatbotConfigs
            .FirstOrDefaultAsync(c => c.Id == configId && c.TenantId == tenantId);

        if (config == null) return false;

        var configuration = config.Configuration ?? new Dictionary<string, object>();
        configuration["lastTrainingDate"] = DateTime.UtcNow;
        config.Configuration = configuration;
        
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<object> GetKnowledgeBaseAsync(Guid configId, Guid tenantId)
    {
        var documents = await _context.Documents
            .Where(d => d.ChatbotConfigId == configId && d.TenantId == tenantId)
            .Select(d => new
            {
                d.Id,
                d.Title,
                d.FileType,
                d.FileSize,
                d.CreatedAt
            })
            .ToListAsync();

        return new { Documents = documents };
    }

    public async Task<bool> AddKnowledgeBaseDocumentAsync(Guid configId, IFormFile document, Guid tenantId)
    {
        var config = await _context.ChatbotConfigs
            .FirstOrDefaultAsync(c => c.Id == configId && c.TenantId == tenantId);

        if (config == null) return false;

        var fileName = $"{configId}_{Guid.NewGuid()}{Path.GetExtension(document.FileName)}";
        var filePath = Path.Combine("uploads", "documents", fileName);
        
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
        
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await document.CopyToAsync(stream);
        }

        var doc = new Document
        {
            Title = document.FileName,
            FileType = document.ContentType,
            FileSize = document.Length,
            FilePath = filePath,
            ChatbotConfigId = configId,
            TenantId = tenantId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Documents.Add(doc);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RemoveKnowledgeBaseDocumentAsync(Guid configId, Guid documentId, Guid tenantId)
    {
        var document = await _context.Documents
            .FirstOrDefaultAsync(d => d.Id == documentId && d.ChatbotConfigId == configId && d.TenantId == tenantId);

        if (document == null) return false;

        _context.Documents.Remove(document);
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
            Configuration = config.Configuration,
            CreatedAt = config.CreatedAt
        };
    }
}
