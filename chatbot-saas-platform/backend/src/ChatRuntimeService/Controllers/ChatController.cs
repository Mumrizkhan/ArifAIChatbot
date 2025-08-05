using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Shared.Application.Common.Interfaces;
using Shared.Domain.Entities;
using Shared.Domain.Enums;
using ChatRuntimeService.Services;
using ChatRuntimeService.Hubs;

namespace ChatRuntimeService.Controllers;

[ApiController]
[Route("api/chat")]
public class ChatController : ControllerBase
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly ILogger<ChatController> _logger;
    private readonly IAIIntegrationService _aiIntegrationService;
    private readonly IHubContext<ChatHub> _hubContext;
    private readonly ILiveAgentIntegrationService _liveAgentIntegrationService;

    public ChatController(
        IApplicationDbContext context,
        ITenantService tenantService,
        ILogger<ChatController> logger,
        IAIIntegrationService aiIntegrationService,
        IHubContext<ChatHub> hubContext,
        ILiveAgentIntegrationService liveAgentIntegrationService)
    {
        _context = context;
        _tenantService = tenantService;
        _logger = logger;
        _aiIntegrationService = aiIntegrationService;
        _hubContext = hubContext;
        _liveAgentIntegrationService = liveAgentIntegrationService;
    }

    [HttpPost("conversations")]
    [AllowAnonymous]
    public async Task<IActionResult> CreateConversation([FromBody] CreateChatConversationRequest request)
    {
        try
        {
            var conversation = new Conversation
            {
                CustomerName = request.CustomerName,
                CustomerEmail = request.CustomerEmail,
                CustomerPhone = request.CustomerPhone,
                Subject = request.Subject ?? "Chat Conversation",
                Channel = ConversationChannel.Widget,
                Language = request.Language ?? "en",
                Status = ConversationStatus.Active,
                TenantId = request.TenantId
            };

            _context.Conversations.Add(conversation);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                id = conversation.Id.ToString(),
                messages = new object[] { },
                status = "active",
                assignedAgent = (object?)null,
                startedAt = conversation.CreatedAt,
                endedAt = (DateTime?)null
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating chat conversation");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost("messages")]
    [AllowAnonymous]
    public async Task<IActionResult> SendMessage([FromBody] SendChatMessageRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Content))
            {
                return BadRequest(new { message = "Message content is required" });
            }

            if (!Guid.TryParse(request.ConversationId, out var conversationId))
            {
                return BadRequest(new { message = "Invalid conversation ID" });
            }

            var conversation = await _context.Conversations
                .FirstOrDefaultAsync(c => c.Id == conversationId);

            if (conversation == null)
            {
                return NotFound(new { message = "Conversation not found" });
            }

            if (!Enum.TryParse<MessageType>(request.Type, true, out var messageType))
            {
                messageType = MessageType.Text;
            }

            var userMessage = new Message
            {
                Id = Guid.NewGuid(),
                ConversationId = conversationId,
                Content = request.Content,
                Type = messageType,
                Sender = MessageSender.Customer,
                SenderId = null,
                CreatedAt = DateTime.UtcNow,
                TenantId = conversation.TenantId,
                IsRead = false
            };

            _context.Messages.Add(userMessage);
            conversation.MessageCount++;
            conversation.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var userMessageDto = new
            {
                Id = userMessage.Id,
                ConversationId = userMessage.ConversationId,
                Content = userMessage.Content,
                Type = userMessage.Type.ToString(),
                Sender = userMessage.Sender.ToString(),
                SenderId = userMessage.SenderId,
                CreatedAt = userMessage.CreatedAt,
                SenderName = conversation.CustomerName ?? "Customer"
            };

            await _hubContext.Clients.Group($"conversation_{conversationId}")
                .SendAsync("ReceiveMessage", userMessageDto);

            if (conversation.Status == ConversationStatus.WaitingForAgent || 
                conversation.Status == ConversationStatus.Queued ||
                conversation.AssignedAgentId.HasValue)
            {
                if (conversation.AssignedAgentId.HasValue)
                {
                    await _hubContext.Clients.Group($"agent_{conversation.AssignedAgentId}")
                        .SendAsync("ReceiveMessage", userMessageDto);
                }
                
                return Ok(new
                {
                    userMessage = new
                    {
                        id = userMessage.Id,
                        conversationId = userMessage.ConversationId,
                        content = userMessage.Content,
                        type = userMessage.Type.ToString(),
                        sender = userMessage.Sender.ToString(),
                        createdAt = userMessage.CreatedAt
                    },
                    routedToAgent = true,
                    success = true
                });
            }

            var botResponse = await _aiIntegrationService.GetBotResponseAsync(
                request.Content, 
                conversationId.ToString(), 
                conversation.Language ?? "en");

            var botMessage = new Message
            {
                Id = Guid.NewGuid(),
                ConversationId = conversationId,
                Content = botResponse,
                Type = MessageType.Text,
                Sender = MessageSender.Bot,
                SenderId = null,
                CreatedAt = DateTime.UtcNow,
                TenantId = conversation.TenantId,
                IsRead = false
            };

            _context.Messages.Add(botMessage);
            conversation.MessageCount++;
            conversation.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var botMessageDto = new
            {
                Id = botMessage.Id,
                ConversationId = botMessage.ConversationId,
                Content = botMessage.Content,
                Type = botMessage.Type.ToString(),
                Sender = botMessage.Sender.ToString(),
                SenderId = botMessage.SenderId,
                CreatedAt = botMessage.CreatedAt,
                SenderName = "AI Assistant"
            };

            await _hubContext.Clients.Group($"conversation_{conversationId}")
                .SendAsync("ReceiveMessage", botMessageDto);

            return Ok(new
            {
                userMessage = new
                {
                    id = userMessage.Id,
                    conversationId = userMessage.ConversationId,
                    content = userMessage.Content,
                    type = userMessage.Type.ToString(),
                    sender = userMessage.Sender.ToString(),
                    createdAt = userMessage.CreatedAt
                },
                botMessage = new
                {
                    id = botMessage.Id,
                    conversationId = botMessage.ConversationId,
                    content = botMessage.Content,
                    type = botMessage.Type.ToString(),
                    sender = botMessage.Sender.ToString(),
                    createdAt = botMessage.CreatedAt
                },
                success = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing chat message");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost("conversations/{conversationId}/escalate")]
    [AllowAnonymous]
    public async Task<IActionResult> EscalateConversation(string conversationId)
    {
        try
        {
            if (!Guid.TryParse(conversationId, out var id))
            {
                return BadRequest(new { message = "Invalid conversation ID" });
            }

            var conversation = await _context.Conversations
                .FirstOrDefaultAsync(c => c.Id == id);

            if (conversation == null)
            {
                return NotFound(new { message = "Conversation not found" });
            }

            conversation.Status = ConversationStatus.WaitingForAgent;
            conversation.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var availableAgentId = await _liveAgentIntegrationService.FindAvailableAgentAsync(
                conversation.TenantId, conversation.Language);

            if (availableAgentId.HasValue)
            {
                var assigned = await _liveAgentIntegrationService.AssignConversationToAgentAsync(
                    id, availableAgentId.Value);

                if (assigned)
                {
                    conversation.AssignedAgentId = availableAgentId.Value;
                    conversation.Status = ConversationStatus.Active;
                    await _context.SaveChangesAsync();

                    await _hubContext.Clients.Group($"agent_{availableAgentId.Value}")
                        .SendAsync("ConversationAssigned", new
                        {
                            ConversationId = id,
                            CustomerName = conversation.CustomerName,
                            Subject = conversation.Subject,
                            Language = conversation.Language
                        });

                    return Ok(new { 
                        success = true, 
                        message = "Conversation assigned to human agent",
                        assignedAgentId = availableAgentId.Value
                    });
                }
            }

            await _liveAgentIntegrationService.AddToQueueAsync(id);

            return Ok(new { 
                success = true, 
                message = "Conversation escalated to human agent queue" 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error escalating conversation {ConversationId}", conversationId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }
}

public class CreateChatConversationRequest
{
    public Guid TenantId { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerEmail { get; set; }
    public string? CustomerPhone { get; set; }
    public string? Subject { get; set; }
    public string? Language { get; set; }
}

public class SendChatMessageRequest
{
    public string ConversationId { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Type { get; set; } = "text";
}
