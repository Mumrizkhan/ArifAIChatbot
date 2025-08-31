using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Application.Common.Interfaces;
using Shared.Domain.Events;
using Shared.Domain.Entities;

namespace NotificationService.Controllers;

[ApiController]
[Route("api/[controller]")]
[AllowAnonymous] // For testing purposes only
public class NotificationTestController : ControllerBase
{
    private readonly IMessageBusNotificationService _notificationService;
    private readonly ILogger<NotificationTestController> _logger;

    public NotificationTestController(
        IMessageBusNotificationService notificationService,
        ILogger<NotificationTestController> logger)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    [HttpPost("test-welcome")]
    public async Task<IActionResult> TestWelcomeNotification([FromBody] TestNotificationRequest request)
    {
        try
        {
            var welcomeEvent = new NotificationCreatedEvent
            {
                NotificationId = Guid.NewGuid(),
                UserId = request.UserId,
                TenantId = request.TenantId,
                Title = "Welcome to ChatBot SaaS!",
                Content = $"Welcome {request.UserName}! Thank you for joining our platform.",
                Type = NotificationType.Welcome,
                Channels = new List<NotificationChannel> 
                { 
                    NotificationChannel.InApp, 
                    NotificationChannel.Email 
                },
                Data = new Dictionary<string, object>
                {
                    ["userName"] = request.UserName,
                    ["testMode"] = true
                },
                Language = "en"
            };

            await _notificationService.PublishNotificationAsync(welcomeEvent);

            return Ok(new { 
                message = "Welcome notification sent successfully",
                notificationId = welcomeEvent.NotificationId,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending test welcome notification");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost("test-conversation-assignment")]
    public async Task<IActionResult> TestConversationAssignment([FromBody] TestConversationRequest request)
    {
        try
        {
            var assignmentEvent = new ConversationAssignedEvent
            {
                ConversationId = request.ConversationId,
                AgentId = request.AgentId,
                TenantId = request.TenantId,
                CustomerName = request.CustomerName,
                AssignedAt = DateTime.UtcNow
            };

            await _notificationService.PublishConversationAssignedAsync(assignmentEvent);

            return Ok(new { 
                message = "Conversation assignment notification sent successfully",
                conversationId = request.ConversationId,
                agentId = request.AgentId,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending test conversation assignment notification");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost("test-bulk-notification")]
    public async Task<IActionResult> TestBulkNotification([FromBody] TestBulkNotificationRequest request)
    {
        try
        {
            var bulkEvent = new BulkNotificationEvent
            {
                TenantId = request.TenantId,
                UserIds = request.UserIds,
                Title = request.Title,
                Content = request.Content,
                Type = NotificationType.SystemAlert,
                Channels = new List<NotificationChannel> 
                { 
                    NotificationChannel.InApp, 
                    NotificationChannel.Email 
                },
                Data = new Dictionary<string, object>
                {
                    ["testMode"] = true,
                    ["batchId"] = Guid.NewGuid()
                },
                Language = "en"
            };

            await _notificationService.PublishBulkNotificationAsync(bulkEvent);

            return Ok(new { 
                message = "Bulk notification sent successfully",
                recipientCount = request.UserIds.Count,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending test bulk notification");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost("test-system-alert")]
    public async Task<IActionResult> TestSystemAlert([FromBody] TestSystemAlertRequest request)
    {
        try
        {
            var systemAlert = new SystemAlertEvent
            {
                AlertType = request.AlertType,
                Title = request.Title,
                Description = request.Description,
                Severity = request.Severity,
                TenantId = request.TenantId,
                Metadata = new Dictionary<string, object>
                {
                    ["testMode"] = true,
                    ["alertId"] = Guid.NewGuid()
                }
            };

            await _notificationService.PublishSystemAlertAsync(systemAlert);

            return Ok(new { 
                message = "System alert sent successfully",
                alertType = request.AlertType,
                severity = request.Severity,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending test system alert");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }
}

public class TestNotificationRequest
{
    public Guid UserId { get; set; }
    public Guid TenantId { get; set; }
    public string UserName { get; set; } = string.Empty;
}

public class TestConversationRequest
{
    public Guid ConversationId { get; set; }
    public Guid AgentId { get; set; }
    public Guid TenantId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
}

public class TestBulkNotificationRequest
{
    public Guid TenantId { get; set; }
    public List<Guid> UserIds { get; set; } = new();
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}

public class TestSystemAlertRequest
{
    public Guid TenantId { get; set; }
    public string AlertType { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Severity { get; set; } = "Info";
}