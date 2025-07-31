using Microsoft.EntityFrameworkCore;
using Shared.Application.Common.Interfaces;
using Shared.Domain.Entities;
using ChatRuntimeService.Models;

namespace ChatRuntimeService.Services;

public class ChatService : IChatService
{
    private readonly IApplicationDbContext _context;
    private readonly IAIIntegrationService _aiIntegrationService;
    private readonly ILogger<ChatService> _logger;

    public ChatService(
        IApplicationDbContext context,
        IAIIntegrationService aiIntegrationService,
        ILogger<ChatService> logger)
    {
        _context = context;
        _aiIntegrationService = aiIntegrationService;
        _logger = logger;
    }

    public async Task<MessageDto> SendMessageAsync(SendChatMessageRequest request, Guid tenantId)
    {
        Conversation conversation;

        if (request.ConversationId.HasValue)
        {
            conversation = await _context.Conversations
                .FirstOrDefaultAsync(c => c.Id == request.ConversationId.Value && c.TenantId == tenantId);

            if (conversation == null)
                throw new ArgumentException("Conversation not found");
        }
        else
        {
            conversation = new Conversation
            {
                TenantId = tenantId,
                Status = "Active",
                CreatedAt = DateTime.UtcNow
            };

            _context.Conversations.Add(conversation);
            await _context.SaveChangesAsync();
        }

        var userMessage = new Message
        {
            ConversationId = conversation.Id,
            Content = request.Message,
            SenderType = "User",
            CreatedAt = DateTime.UtcNow
        };

        _context.Messages.Add(userMessage);
        await _context.SaveChangesAsync();

        var botResponse = await _aiIntegrationService.GetBotResponseAsync(
            request.Message, 
            conversation.Id.ToString(), 
            request.Language ?? "en");

        var botMessage = new Message
        {
            ConversationId = conversation.Id,
            Content = botResponse,
            SenderType = "Bot",
            CreatedAt = DateTime.UtcNow
        };

        _context.Messages.Add(botMessage);
        await _context.SaveChangesAsync();

        return new MessageDto
        {
            Id = botMessage.Id,
            ConversationId = botMessage.ConversationId,
            Content = botMessage.Content,
            SenderType = botMessage.SenderType,
            SenderId = botMessage.SenderId,
            CreatedAt = botMessage.CreatedAt
        };
    }

    public async Task<ConversationDto> StartConversationAsync(StartConversationRequest request, Guid tenantId)
    {
        var conversation = new Conversation
        {
            TenantId = tenantId,
            Status = "Active",
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
                CreatedAt = DateTime.UtcNow
            };

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();
        }

        return new ConversationDto
        {
            Id = conversation.Id,
            Status = conversation.Status,
            TenantId = conversation.TenantId,
            UserId = conversation.UserId,
            AssignedAgentId = conversation.AssignedAgentId,
            CreatedAt = conversation.CreatedAt,
            UpdatedAt = conversation.UpdatedAt,
            Messages = new List<MessageDto>()
        };
    }

    public async Task<bool> EscalateConversationAsync(Guid conversationId, Guid tenantId)
    {
        var conversation = await _context.Conversations
            .FirstOrDefaultAsync(c => c.Id == conversationId && c.TenantId == tenantId);

        if (conversation == null) return false;

        conversation.Status = "Escalated";
        conversation.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }
}
