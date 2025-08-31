using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using Shared.Application.Common.Interfaces;
using Shared.Domain.Entities;
using Shared.Domain.Enums;
using Shared.Domain.Events;
using ChatRuntimeService.Hubs;

namespace ChatRuntimeService.Services;

public class ChatService : IChatService
{
    private readonly IApplicationDbContext _context;
    private readonly IHubContext<ChatHub> _hubContext;
    private readonly IMessageBusNotificationService _notificationService;
    private readonly ITenantService _tenantService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<ChatService> _logger;

    public ChatService(
        IApplicationDbContext context,
        IHubContext<ChatHub> hubContext,
        IMessageBusNotificationService notificationService,
        ITenantService tenantService,
        ICurrentUserService currentUserService,
        ILogger<ChatService> logger)
    {
        _context = context;
        _hubContext = hubContext;
        _notificationService = notificationService;
        _tenantService = tenantService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    // Main implementation methods that match interface exactly
    public async Task<Conversation> StartConversationAsync(StartConversationRequest request, Guid tenantId)
    {
        try
        {
            var conversation = new Conversation
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                CustomerName = request.CustomerName,
                CustomerEmail = request.CustomerEmail,
                Status = ConversationStatus.Active,
                StartedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            _context.Conversations.Add(conversation);

            if (!string.IsNullOrEmpty(request.InitialMessage))
            {
                var message = new Message
                {
                    Id = Guid.NewGuid(),
                    ConversationId = conversation.Id,
                    Content = request.InitialMessage,
                    Type = MessageType.Text,
                    Sender = MessageSender.Customer,
                    SenderName = request.CustomerName,
                    TenantId = tenantId,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Messages.Add(message);

                // Publish new message event for notifications
                var newMessageEvent = new NewMessageEvent
                {
                    MessageId = message.Id,
                    ConversationId = conversation.Id,
                    SenderName = request.CustomerName,
                    Content = request.InitialMessage,
                    MessageType = "text",
                    TenantId = tenantId
                };

                await _notificationService.PublishNewMessageAsync(newMessageEvent);
            }

            await _context.SaveChangesAsync();

            // Send to SignalR hub
            await _hubContext.Clients.Group($"tenant_{tenantId}")
                .SendAsync("ConversationStarted", new
                {
                    conversation.Id,
                    conversation.CustomerName,
                    conversation.CustomerEmail,
                    InitialMessage = request.InitialMessage,
                    conversation.StartedAt
                });

            _logger.LogInformation("Conversation started: {ConversationId}", conversation.Id);
            return conversation;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting conversation");
            throw;
        }
    }

    public async Task<Message> SendMessageAsync(SendChatMessageRequest request, Guid tenantId)
    {
        try
        {
            var conversationId = request.ConversationId;
            var conversation = await _context.Conversations
                .FirstOrDefaultAsync(c => c.Id == conversationId && c.TenantId == tenantId);

            if (conversation == null)
                throw new InvalidOperationException("Conversation not found");

            var message = new Message
            {
                Id = Guid.NewGuid(),
                ConversationId = conversationId,
                Content = request.Content,
                Type = MessageType.Text,
                Sender = MessageSender.Customer,
                SenderName = request.SenderName ?? "Customer",
                TenantId = tenantId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Messages.Add(message);
            conversation.LastMessageAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Publish new message event for notifications
            var newMessageEvent = new NewMessageEvent
            {
                MessageId = message.Id,
                ConversationId = conversationId,
                SenderName = message.SenderName,
                Content = request.Content,
                MessageType = request.MessageType,
                TenantId = tenantId
            };

            await _notificationService.PublishNewMessageAsync(newMessageEvent);

            // Send to SignalR hub
            await _hubContext.Clients.Group($"conversation_{conversationId}")
                .SendAsync("MessageReceived", new
                {
                    message.Id,
                    message.ConversationId,
                    message.Content,
                    MessageType = message.Type.ToString(),
                    Sender = message.Sender.ToString(),
                    message.SenderId,
                    message.SenderName,
                    message.CreatedAt
                });

            _logger.LogInformation("Message sent: {MessageId} in conversation {ConversationId}", 
                message.Id, conversationId);
            return message;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending message to conversation {ConversationId}", request.ConversationId);
            throw;
        }
    }

    public async Task<bool> AssignConversationAsync(Guid conversationId, Guid agentId)
    {
        try
        {
            var tenantId = _tenantService.GetCurrentTenantId();
            var conversation = await _context.Conversations
                .FirstOrDefaultAsync(c => c.Id == conversationId && c.TenantId == tenantId);

            if (conversation == null)
                return false;

            conversation.AssignedAgentId = agentId;
            conversation.AssignedAt = DateTime.UtcNow;
            conversation.UpdatedAt = DateTime.UtcNow;
            
            await _context.SaveChangesAsync();

            // Publish conversation assigned event for notifications
            var assignedEvent = new ConversationAssignedEvent
            {
                ConversationId = conversationId,
                AgentId = agentId,
                TenantId = tenantId,
                CustomerName = conversation.CustomerName
            };

            await _notificationService.PublishConversationAssignedAsync(assignedEvent);

            // Send to SignalR hub
            await _hubContext.Clients.Group($"conversation_{conversationId}")
                .SendAsync("ConversationAssigned", new
                {
                    conversationId,
                    agentId,
                    AssignedAt = conversation.AssignedAt
                });

            _logger.LogInformation("Conversation {ConversationId} assigned to agent {AgentId}", 
                conversationId, agentId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning conversation {ConversationId} to agent {AgentId}", 
                conversationId, agentId);
            return false;
        }
    }

    public async Task<bool> EscalateConversationAsync(Guid conversationId, Guid toAgentId)
    {
        return await EscalateConversationAsync(conversationId, toAgentId, "Manual escalation");
    }

    public async Task<bool> EscalateConversationAsync(Guid conversationId, Guid toAgentId, string reason)
    {
        try
        {
            var tenantId = _tenantService.GetCurrentTenantId();
            var conversation = await _context.Conversations
                .FirstOrDefaultAsync(c => c.Id == conversationId && c.TenantId == tenantId);

            if (conversation == null)
                return false;

            var fromAgentId = conversation.AssignedAgentId;
            conversation.AssignedAgentId = toAgentId;
            conversation.EscalatedAt = DateTime.UtcNow;
            conversation.UpdatedAt = DateTime.UtcNow;
            
            await _context.SaveChangesAsync();

            // Publish conversation escalated event for notifications
            var escalatedEvent = new ConversationEscalatedEvent
            {
                ConversationId = conversationId,
                FromAgentId = fromAgentId,
                ToAgentId = toAgentId,
                Reason = reason,
                TenantId = tenantId
            };

            await _notificationService.PublishConversationEscalatedAsync(escalatedEvent);

            // Send to SignalR hub
            await _hubContext.Clients.Group($"conversation_{conversationId}")
                .SendAsync("ConversationEscalated", new
                {
                    conversationId,
                    fromAgentId,
                    toAgentId,
                    reason,
                    EscalatedAt = conversation.EscalatedAt
                });

            _logger.LogInformation("Conversation {ConversationId} escalated from agent {FromAgentId} to agent {ToAgentId}", 
                conversationId, fromAgentId, toAgentId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error escalating conversation {ConversationId}", conversationId);
            return false;
        }
    }

    public async Task<List<Conversation>> GetActiveConversationsAsync()
    {
        try
        {
            var tenantId = _tenantService.GetCurrentTenantId();
            return await _context.Conversations
                .Where(c => c.TenantId == tenantId && c.Status == ConversationStatus.Active)
                .OrderByDescending(c => c.LastMessageAt ?? c.StartedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active conversations");
            throw;
        }
    }

    public async Task<List<Message>> GetConversationMessagesAsync(Guid conversationId)
    {
        try
        {
            var tenantId = _tenantService.GetCurrentTenantId();
            return await _context.Messages
                .Where(m => m.ConversationId == conversationId && m.TenantId == tenantId)
                .OrderBy(m => m.CreatedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting messages for conversation {ConversationId}", conversationId);
            throw;
        }
    }

    public async Task<bool> CloseConversationAsync(Guid conversationId)
    {
        try
        {
            var tenantId = _tenantService.GetCurrentTenantId();
            var conversation = await _context.Conversations
                .FirstOrDefaultAsync(c => c.Id == conversationId && c.TenantId == tenantId);

            if (conversation == null)
                return false;

            conversation.Status = ConversationStatus.Closed;
            conversation.EndedAt = DateTime.UtcNow;
            conversation.UpdatedAt = DateTime.UtcNow;
            
            await _context.SaveChangesAsync();

            // Send to SignalR hub
            await _hubContext.Clients.Group($"conversation_{conversationId}")
                .SendAsync("ConversationClosed", new
                {
                    conversationId,
                    EndedAt = conversation.EndedAt
                });

            _logger.LogInformation("Conversation closed: {ConversationId}", conversationId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error closing conversation {ConversationId}", conversationId);
            return false;
        }
    }

    public Task<Models.MessageDto> SendMessageAsync(Models.SendChatMessageRequest request, Guid tenantId)
    {
        throw new NotImplementedException();
    }

    public Task<Models.ConversationDto> StartConversationAsync(Models.StartConversationRequest request, Guid tenantId)
    {
        throw new NotImplementedException();
    }
}
