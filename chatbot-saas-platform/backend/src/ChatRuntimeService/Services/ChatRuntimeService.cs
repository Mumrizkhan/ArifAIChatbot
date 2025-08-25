using Microsoft.EntityFrameworkCore;
using Shared.Application.Common.Interfaces;
using Shared.Domain.Entities;
using ChatRuntimeService.Models;
using Shared.Domain.Enums;

namespace ChatRuntimeService.Services;

public class ChatRuntimeService : IChatRuntimeService
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<ChatRuntimeService> _logger;

    public ChatRuntimeService(IApplicationDbContext context, ILogger<ChatRuntimeService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ConversationDto> CreateConversationAsync(CreateConversationRequest request, Guid tenantId)
    {
        var conversation = new Conversation
        {
            TenantId = tenantId,
            UserId = request.UserId,
            Status =  Shared.Domain.Enums.ConversationStatus.Active,
            CreatedAt = DateTime.UtcNow
        };

        _context.Conversations.Add(conversation);
        await _context.SaveChangesAsync();

        if (!string.IsNullOrEmpty(request.InitialMessage))
        {
            var message = new Message
            {
                ConversationId = conversation.Id,
                Content = request.InitialMessage,
                SenderType = "User",
                SenderId = request.UserId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();
        }

        return MapToDto(conversation);
    }

    public async Task<PaginatedResult<ConversationDto>> GetConversationsAsync(Guid tenantId, int page, int pageSize)
    {
        var query = _context.Conversations
            .Where(c => c.TenantId == tenantId)
            .Include(c => c.Messages.OrderBy(m => m.CreatedAt).Take(5));

        var totalCount = await query.CountAsync();
        var conversations = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PaginatedResult<ConversationDto>
        {
            Items = conversations.Select(MapToDto).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<ConversationDetailDto?> GetConversationAsync(Guid id, Guid tenantId)
    {
        var conversation = await _context.Conversations
            .Include(c => c.Messages.OrderBy(m => m.CreatedAt))
            .Include(c => c.User)
            .FirstOrDefaultAsync(c => c.Id == id && c.TenantId == tenantId);

        if (conversation == null) return null;

        return new ConversationDetailDto
        {
            Id = conversation.Id,
            Status = conversation.Status.ToString(),
            TenantId = conversation.TenantId,
            UserId = conversation.UserId,
            AssignedAgentId = conversation.AssignedAgentId,
            CreatedAt = conversation.CreatedAt.ToString("O"),
            UpdatedAt = conversation.UpdatedAt?.ToString("O")??"",
            UserName = conversation.User != null ? $"{conversation.User.FirstName} {conversation.User.LastName}" : null,
            Messages = conversation.Messages.Select(MapMessageToDto).ToList()
        };
    }

    public async Task<bool> AssignConversationAsync(Guid id, AssignConversationRequest request, Guid tenantId)
    {
        var conversation = await _context.Conversations
            .FirstOrDefaultAsync(c => c.Id == id && c.TenantId == tenantId);

        if (conversation == null) return false;

        conversation.AssignedAgentId = request.AgentId;
        conversation.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdateConversationStatusAsync(Guid id, UpdateStatusRequest request, Guid tenantId)
    {
        var conversation = await _context.Conversations
            .FirstOrDefaultAsync(c => c.Id == id && c.TenantId == tenantId);

        if (conversation == null) return false;

        conversation.Status =Enum.Parse<ConversationStatus>( request.Status);
        conversation.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<MessageDto> SendMessageAsync(Guid conversationId, SendMessageRequest request, Guid tenantId, Guid? userId)
    {
        var conversation = await _context.Conversations
            .FirstOrDefaultAsync(c => c.Id == conversationId && c.TenantId == tenantId);

        if (conversation == null)
            throw new ArgumentException("Conversation not found");

        var message = new Message
        {
            ConversationId = conversationId,
            Content = request.Content,
            SenderType = request.SenderType,
            SenderId = request.SenderId ?? userId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Messages.Add(message);
        await _context.SaveChangesAsync();

        return MapMessageToDto(message);
    }

    private ConversationDto MapToDto(Conversation conversation)
    {
        return new ConversationDto
        {
            Id = conversation.Id,
            Status = conversation.Status.ToString(),
            TenantId = conversation.TenantId,
            UserId = conversation.UserId,
            AssignedAgentId = conversation.AssignedAgentId,
            CreatedAt = conversation.CreatedAt.ToString("O"),
            UpdatedAt = conversation.UpdatedAt?.ToString("O"),
            Messages = conversation.Messages?.Select(MapMessageToDto).ToList() ?? new List<MessageDto>()
        };
    }

    private MessageDto MapMessageToDto(Message message)
    {
        return new MessageDto
        {
            Id = message.Id,
            ConversationId = message.ConversationId,
            Content = message.Content,
            SenderType = message.SenderType,
            SenderId = message.SenderId,
            CreatedAt = message.CreatedAt.ToString("O")
        };
    }
}
