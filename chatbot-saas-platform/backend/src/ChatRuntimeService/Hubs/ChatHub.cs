using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Shared.Application.Common.Interfaces;
using Shared.Domain.Entities;
using Shared.Domain.Enums;

namespace ChatRuntimeService.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ITenantService _tenantService;
    private readonly ILogger<ChatHub> _logger;

    public ChatHub(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        ITenantService tenantService,
        ILogger<ChatHub> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _tenantService = tenantService;
        _logger = logger;
    }

    public async Task JoinConversation(string conversationId)
    {
        try
        {
            if (Guid.TryParse(conversationId, out var convId))
            {
                var conversation = await _context.Conversations
                    .FirstOrDefaultAsync(c => c.Id == convId && c.TenantId == _tenantService.GetCurrentTenantId());

                if (conversation != null)
                {
                    await Groups.AddToGroupAsync(Context.ConnectionId, $"conversation_{conversationId}");
                    _logger.LogInformation($"User {_currentUserService.UserId} joined conversation {conversationId}");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error joining conversation {conversationId}");
        }
    }

    public async Task LeaveConversation(string conversationId)
    {
        try
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"conversation_{conversationId}");
            _logger.LogInformation($"User {_currentUserService.UserId} left conversation {conversationId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error leaving conversation {conversationId}");
        }
    }

    public async Task SendMessage(string conversationId, string content, string messageType = "Text")
    {
        try
        {
            if (!Guid.TryParse(conversationId, out var convId))
                return;

            var conversation = await _context.Conversations
                .FirstOrDefaultAsync(c => c.Id == convId && c.TenantId == _tenantService.GetCurrentTenantId());

            if (conversation == null)
                return;

            var message = new Message
            {
                ConversationId = convId,
                Content = content,
                Type = Enum.Parse<MessageType>(messageType),
                Sender = _currentUserService.UserId.HasValue ? MessageSender.Agent : MessageSender.Customer,
                SenderId = _currentUserService.UserId,
                CreatedAt = DateTime.UtcNow,
                TenantId = _tenantService.GetCurrentTenantId()
            };

            _context.Messages.Add(message);
            conversation.MessageCount++;
            conversation.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var messageDto = new
            {
                Id = message.Id,
                ConversationId = message.ConversationId,
                Content = message.Content,
                Type = message.Type.ToString(),
                Sender = message.Sender.ToString(),
                SenderId = message.SenderId,
                CreatedAt = message.CreatedAt,
                SenderName = _currentUserService.UserId.HasValue ? 
                    $"{_currentUserService.Email}" : conversation.CustomerName ?? "Customer"
            };

            await Clients.Group($"conversation_{conversationId}").SendAsync("ReceiveMessage", messageDto);
            _logger.LogInformation($"Message sent to conversation {conversationId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error sending message to conversation {conversationId}");
        }
    }

    public async Task StartTyping(string conversationId)
    {
        try
        {
            var typingInfo = new
            {
                UserId = _currentUserService.UserId,
                UserName = _currentUserService.Email ?? "User",
                ConversationId = conversationId
            };

            await Clients.OthersInGroup($"conversation_{conversationId}")
                .SendAsync("UserStartedTyping", typingInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error broadcasting typing status for conversation {conversationId}");
        }
    }

    public async Task StopTyping(string conversationId)
    {
        try
        {
            var typingInfo = new
            {
                UserId = _currentUserService.UserId,
                ConversationId = conversationId
            };

            await Clients.OthersInGroup($"conversation_{conversationId}")
                .SendAsync("UserStoppedTyping", typingInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error broadcasting stop typing status for conversation {conversationId}");
        }
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation($"User {_currentUserService.UserId} connected to chat hub");
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation($"User {_currentUserService.UserId} disconnected from chat hub");
        await base.OnDisconnectedAsync(exception);
    }
}
