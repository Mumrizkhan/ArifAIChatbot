using ChatRuntimeService.Hubs;
using ChatRuntimeService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Shared.Application.Common.Interfaces;
using Shared.Domain.Entities;
using Shared.Domain.Enums;
using Shared.Infrastructure.Services;

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
                startedAt = conversation.CreatedAt.ToString("O"), // ISO 8601 format
                endedAt = (string?)null // Changed to string to match the format
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
                CreatedAt = userMessage.CreatedAt.ToString("O"), // ISO 8601 format
                SenderName = conversation.CustomerName ?? "Customer"
            };

            await _hubContext.Clients.Group($"conversation_{conversationId}")
                .SendAsync("ReceiveMessage", userMessageDto);

            if (conversation.Status == ConversationStatus.WaitingForAgent || 
                conversation.Status == ConversationStatus.Queued ||
                conversation.AssignedAgentId.HasValue)
            {
                // Notify agents via LiveAgentService when customer sends message to assigned conversation
                if (conversation.AssignedAgentId.HasValue)
                {
                    await _liveAgentIntegrationService.NotifyMessageAsync(
                        conversationId,
                        userMessage.Id,
                        userMessage.Content,
                        userMessage.Type.ToString(),
                        userMessage.Sender.ToString(),
                        userMessage.SenderId,
                        conversation.CustomerName ?? "Customer",
                        userMessage.CreatedAt
                    );
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
                        createdAt = userMessage.CreatedAt.ToString("O") // ISO 8601 format
                    },
                    routedToAgent = true,
                    success = true
                });
            }

            var botResponse = await _aiIntegrationService.GetBotResponseAsync(
                request.Content, 
                conversationId.ToString(),
                _tenantService.GetCurrentTenantId().ToString(),
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
                CreatedAt = botMessage.CreatedAt.ToString("O"), // ISO 8601 format
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
                    createdAt = userMessage.CreatedAt.ToString("O") // ISO 8601 format
                },
                botMessage = new
                {
                    id = botMessage.Id,
                    conversationId = botMessage.ConversationId,
                    content = botMessage.Content,
                    type = botMessage.Type.ToString(),
                    sender = botMessage.Sender.ToString(),
                    createdAt = botMessage.CreatedAt.ToString("O") // ISO 8601 format
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

    [HttpPost("agent-messages")]
    [AllowAnonymous] 
    public async Task<IActionResult> SendAgentMessage([FromBody] SendAgentMessageRequest request)
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

            var agentMessage = new Message
            {
                Id = Guid.NewGuid(),
                ConversationId = conversationId,
                Content = request.Content,
                Type = messageType,
                Sender = MessageSender.Agent,
                SenderId = request.SenderId,
                CreatedAt = DateTime.UtcNow,
                TenantId = conversation.TenantId,
                IsRead = false
            };

            _context.Messages.Add(agentMessage);
            conversation.MessageCount++;
            conversation.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var agentMessageDto = new
            {
                Id = agentMessage.Id,
                ConversationId = agentMessage.ConversationId,
                Content = agentMessage.Content,
                Type = agentMessage.Type.ToString(),
                Sender = agentMessage.Sender.ToString(),
                SenderId = agentMessage.SenderId,
                CreatedAt = agentMessage.CreatedAt.ToString("O"), // ISO 8601 format
                SenderName = "Agent"
            };

            // Send to conversation participants (customers via ChatHub)
            var groupName = $"conversation_{conversationId}";
            _logger.LogInformation("🔥 Sending agent message to SignalR group: {GroupName}", groupName);
            _logger.LogInformation("🔥 Agent message content: {Content}", agentMessage.Content);
            _logger.LogInformation("🔥 Agent message DTO: {@AgentMessageDto}", agentMessageDto);
            
            await _hubContext.Clients.Group(groupName)
                .SendAsync("ReceiveMessage", agentMessageDto);
            
            _logger.LogInformation("✅ Agent message sent to SignalR group: {GroupName}", groupName);

            // Also notify agents via LiveAgentService if the conversation has an assigned agent
            if (conversation.AssignedAgentId.HasValue)
            {
                await _liveAgentIntegrationService.NotifyMessageAsync(
                    conversationId,
                    agentMessage.Id,
                    agentMessage.Content,
                    agentMessage.Type.ToString(),
                    agentMessage.Sender.ToString(),
                    agentMessage.SenderId,
                    "Agent",
                    agentMessage.CreatedAt
                );
            }

            return Ok(new
            {
                id = agentMessage.Id,
                conversationId = agentMessage.ConversationId,
                content = agentMessage.Content,
                type = agentMessage.Type.ToString(),
                sender = agentMessage.Sender.ToString(),
                senderId = agentMessage.SenderId,
                createdAt = agentMessage.CreatedAt.ToString("O"), // ISO 8601 format
                success = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing agent message");
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

                    var assignmentPayload = new
                    {
                        ConversationId = id,
                        AgentId = availableAgentId.Value,
                        AgentName = "Agent", // Default agent name for now
                        CustomerName = conversation.CustomerName,
                        CustomerEmail = conversation.CustomerEmail,
                        Subject = conversation.Subject,
                        Language = conversation.Language,
                        Timestamp = DateTime.UtcNow.ToString("O"), // ISO 8601 format
                        Status = "active"
                    };

                    // Send to conversation participants (customers)
                    await _hubContext.Clients.Group($"conversation_{id}")
                        .SendAsync("ConversationAssigned", assignmentPayload);

                    // Send escalation notification to agents via LiveAgentService
                    await _liveAgentIntegrationService.NotifyEscalationAsync(
                        id,
                        conversation.CustomerName,
                        conversation.CustomerEmail,
                        conversation.Subject,
                        conversation.Language
                    );

                    return Ok(new { 
                        success = true, 
                        message = "Conversation assigned to human agent",
                        assignedAgentId = availableAgentId.Value
                    });
                }
            }

            // No available agent - add to queue and notify agents via LiveAgentService
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

    [HttpPut("messages/{messageId}/mark-read")]
    [AllowAnonymous]
    public async Task<IActionResult> MarkMessageAsRead(string messageId, [FromBody] MarkMessageAsReadRequest request)
    {
        try
        {
            var tenantId = _tenantService.GetCurrentTenantId();
            _logger.LogInformation("Marking message as read: {MessageId}, Reader: {ReaderId}", messageId, request.ReaderId);

            if (!Guid.TryParse(messageId, out var msgId))
            {
                return BadRequest(new { message = "Invalid message ID format" });
            }

            var message = await _context.Messages
                .FirstOrDefaultAsync(m => m.Id == msgId && m.TenantId == tenantId);

            if (message == null)
            {
                return NotFound(new { message = "Message not found" });
            }

            // Update message read status
            message.IsRead = true;
            message.ReadAt = DateTime.UtcNow;
            message.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Notify via SignalR that message was read
            var readNotification = new
            {
                MessageId = messageId,
                ConversationId = message.ConversationId.ToString(),
                ReaderId = request.ReaderId,
                ReaderType = request.ReaderType, // "customer" or "agent"
                ReadAt = message.ReadAt?.ToString("O")
            };

            await _hubContext.Clients.Group($"conversation_{message.ConversationId}")
                .SendAsync("MessageMarkedAsRead", readNotification);

            _logger.LogInformation("Message {MessageId} marked as read by {ReaderType} {ReaderId}", 
                messageId, request.ReaderType, request.ReaderId);

            return Ok(new { success = true, message = "Message marked as read" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking message as read: {MessageId}", messageId);
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

public class SendAgentMessageRequest
{
    public string ConversationId { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Type { get; set; } = "text";
    public Guid? SenderId { get; set; }
}

public class MarkMessageAsReadRequest
{
    public string ReaderId { get; set; } = string.Empty; // User/Agent ID who read the message
    public string ReaderType { get; set; } = string.Empty; // "customer" or "agent"
}
