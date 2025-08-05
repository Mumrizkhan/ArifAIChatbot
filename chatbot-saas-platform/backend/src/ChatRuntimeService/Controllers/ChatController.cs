using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared.Application.Common.Interfaces;
using Shared.Domain.Entities;
using Shared.Domain.Enums;

namespace ChatRuntimeService.Controllers;

[ApiController]
[Route("api/chat")]
public class ChatController : ControllerBase
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly ILogger<ChatController> _logger;

    public ChatController(
        IApplicationDbContext context,
        ITenantService tenantService,
        ILogger<ChatController> logger)
    {
        _context = context;
        _tenantService = tenantService;
        _logger = logger;
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

            var message = new Message
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

            _context.Messages.Add(message);
            conversation.MessageCount++;
            conversation.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                id = message.Id,
                conversationId = message.ConversationId,
                content = message.Content,
                type = message.Type.ToString(),
                sender = message.Sender.ToString(),
                createdAt = message.CreatedAt,
                success = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending chat message");
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

            return Ok(new { success = true, message = "Conversation escalated to human agent" });
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