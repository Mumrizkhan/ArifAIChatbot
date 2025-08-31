using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Application.Common.Interfaces;
using NotificationService.Services;
using NotificationService.Models;
using Shared.Domain.Entities;

namespace NotificationService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;
    private readonly ITemplateService _templateService;
    private readonly IEmailService _emailService;
    private readonly ITenantService _tenantService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<NotificationsController> _logger;

    public NotificationsController(
        INotificationService notificationService,
        ITemplateService templateService,
        IEmailService emailService,
        ITenantService tenantService,
        ICurrentUserService currentUserService,
        ILogger<NotificationsController> logger)
    {
        _notificationService = notificationService;
        _templateService = templateService;
        _emailService = emailService;
        _tenantService = tenantService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    [HttpPost("send")]
    public async Task<IActionResult> SendNotification([FromBody] SendNotificationRequest request)
    {
        try
        {
            var tenantId = _tenantService.GetCurrentTenantId();
            var notificationId = await _notificationService.SendNotificationAsync(request, tenantId);

            return Ok(new
            {
                NotificationId = notificationId,
                Message = "Notification sent successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending notification");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost("send-email")]
    public async Task<IActionResult> SendEmailNotification([FromBody] SendEmailNotificationRequest request)
    {
        try
        {
            var tenantId = _tenantService.GetCurrentTenantId();
            
            // Send real-time notification first
            var notificationId = await _notificationService.SendNotificationAsync(new SendNotificationRequest
            {
                Title = request.Subject,
                Content = request.Message,
                Type = request.Type,
                RecipientEmail = string.Join(",", request.Recipients),
                Channels = new List<NotificationChannel> { NotificationChannel.Email }
            }, tenantId);

            // Send email notifications
            var emailTasks = request.Recipients.Select(recipient => 
                _emailService.SendEmailAsync(recipient, request.Subject, request.Message)
            );
            
            await Task.WhenAll(emailTasks);

            return Ok(new
            {
                NotificationId = notificationId,
                EmailsSent = request.Recipients.Count,
                Message = "Email notifications sent successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending email notification");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost("test-system")]
    public async Task<IActionResult> TestNotificationSystem([FromBody] TestSystemRequest request)
    {
        try
        {
            var tenantId = _tenantService.GetCurrentTenantId();
            var results = new List<string>();

            // Test real-time notification
            try
            {
                var notificationId = await _notificationService.SendNotificationAsync(new SendNotificationRequest
                {
                    Title = "🧪 Test Notification",
                    Content = "This is a test notification to verify the real-time notification system is working properly.",
                    Type = NotificationType.SystemAlert,
                    UserId = _currentUserService.UserId,
                    Channels = new List<NotificationChannel> { NotificationChannel.InApp }
                }, tenantId);
                
                results.Add($"✅ Real-time notification sent successfully (ID: {notificationId})");
            }
            catch (Exception ex)
            {
                results.Add($"❌ Real-time notification failed: {ex.Message}");
            }

            // Test email notification (if email provided)
            if (!string.IsNullOrEmpty(request.TestEmail))
            {
                try
                {
                    await _emailService.SendEmailAsync(
                        request.TestEmail,
                        "🧪 Test Email Notification",
                        "This is a test email to verify the email notification system is working properly."
                        
                    );
                    results.Add($"✅ Email notification sent successfully to {request.TestEmail}");
                }
                catch (Exception ex)
                {
                    results.Add($"❌ Email notification failed: {ex.Message}");
                }
            }

            return Ok(new
            {
                Message = "Notification system test completed",
                Results = results,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing notification system");
            return StatusCode(500, new { message = "Internal server error during notification test" });
        }
    }

    [HttpPost("send-bulk")]
    public async Task<IActionResult> SendBulkNotification([FromBody] BulkNotificationRequest request)
    {
        try
        {
            var tenantId = _tenantService.GetCurrentTenantId();
            var notificationIds = await _notificationService.SendBulkNotificationAsync(request, tenantId);

            return Ok(new
            {
                NotificationIds = notificationIds,
                Count = notificationIds.Count,
                Message = "Bulk notifications sent successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending bulk notification");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetNotifications([FromQuery] Guid? userId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        try
        {
            var tenantId = _tenantService.GetCurrentTenantId();
            var currentUserId = _currentUserService.UserId;

            var targetUserId = userId ?? currentUserId;

            var notifications = await _notificationService.GetNotificationsAsync(tenantId, targetUserId, page, pageSize);

            return Ok(notifications.Select(n => new
            {
                n.Id,
                n.Title,
                n.Content,
                Type = n.Type.ToString(),
                Channel = n.Channel.ToString(),
                Status = n.Status.ToString(),
                n.Data,
                n.CreatedAt,
                n.SentAt,
                n.DeliveredAt,
                n.ReadAt,
                IsRead = n.ReadAt.HasValue,
                n.Language
            }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting notifications");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("{notificationId}")]
    public async Task<IActionResult> GetNotification(Guid notificationId)
    {
        try
        {
            var tenantId = _tenantService.GetCurrentTenantId();
            var notification = await _notificationService.GetNotificationAsync(notificationId, tenantId);

            if (notification == null)
            {
                return NotFound(new { message = "Notification not found" });
            }

            return Ok(new
            {
                notification.Id,
                notification.Title,
                notification.Content,
                Type = notification.Type.ToString(),
                Channel = notification.Channel.ToString(),
                Status = notification.Status.ToString(),
                notification.UserId,
                notification.RecipientEmail,
                notification.RecipientPhone,
                notification.Data,
                notification.TemplateId,
                notification.TemplateData,
                notification.CreatedAt,
                notification.SentAt,
                notification.DeliveredAt,
                notification.ReadAt,
                IsRead = notification.ReadAt.HasValue,
                notification.ErrorMessage,
                notification.RetryCount,
                notification.Language
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting notification {NotificationId}", notificationId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost("{notificationId}/mark-read")]
    public async Task<IActionResult> MarkAsRead(Guid notificationId)
    {
        try
        {
            var tenantId = _tenantService.GetCurrentTenantId();
            var marked = await _notificationService.MarkAsReadAsync(notificationId, tenantId);

            if (!marked)
            {
                return NotFound(new { message = "Notification not found" });
            }

            return Ok(new { message = "Notification marked as read" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking notification {NotificationId} as read", notificationId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost("mark-all-read")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        try
        {
            var tenantId = _tenantService.GetCurrentTenantId();
            var userId = _currentUserService.UserId;

            if (!userId.HasValue)
            {
                return Unauthorized();
            }

            var marked = await _notificationService.MarkAllAsReadAsync(userId.Value, tenantId);

            if (!marked)
            {
                return BadRequest(new { message = "Failed to mark notifications as read" });
            }

            return Ok(new { message = "All notifications marked as read" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking all notifications as read");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("statistics")]
    public async Task<IActionResult> GetStatistics([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
    {
        try
        {
            var tenantId = _tenantService.GetCurrentTenantId();
            var statistics = await _notificationService.GetStatisticsAsync(tenantId, startDate, endDate);

            return Ok(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting notification statistics");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost("{notificationId}/cancel")]
    public async Task<IActionResult> CancelNotification(Guid notificationId)
    {
        try
        {
            var tenantId = _tenantService.GetCurrentTenantId();
            var cancelled = await _notificationService.CancelNotificationAsync(notificationId, tenantId);

            if (!cancelled)
            {
                return NotFound(new { message = "Notification not found or cannot be cancelled" });
            }

            return Ok(new { message = "Notification cancelled successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling notification {NotificationId}", notificationId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost("{notificationId}/retry")]
    public async Task<IActionResult> RetryNotification(Guid notificationId)
    {
        try
        {
            var tenantId = _tenantService.GetCurrentTenantId();
            var retried = await _notificationService.RetryFailedNotificationAsync(notificationId, tenantId);

            if (!retried)
            {
                return NotFound(new { message = "Notification not found or cannot be retried" });
            }

            return Ok(new { message = "Notification retry initiated" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrying notification {NotificationId}", notificationId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("templates")]
    public async Task<IActionResult> GetTemplates([FromQuery] NotificationType? type, [FromQuery] NotificationChannel? channel)
    {
        try
        {
            var tenantId = _tenantService.GetCurrentTenantId();
            var templates = await _templateService.GetTemplatesAsync(tenantId, type, channel);

            return Ok(templates.Select(t => new
            {
                t.Id,
                t.Name,
                t.Subject,
                t.Content,
                Type = t.Type.ToString(),
                Channel = t.Channel.ToString(),
                t.Language,
                t.IsActive,
                t.DefaultData,
                t.CreatedAt,
                t.UpdatedAt
            }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting notification templates");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost("templates")]
    public async Task<IActionResult> CreateTemplate([FromBody] NotificationTemplate template)
    {
        try
        {
            var tenantId = _tenantService.GetCurrentTenantId();
            template.TenantId = tenantId;

            var createdTemplate = await _templateService.CreateTemplateAsync(template);

            return Ok(new
            {
                createdTemplate.Id,
                createdTemplate.Name,
                Message = "Template created successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating notification template");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPut("templates/{templateId}")]
    public async Task<IActionResult> UpdateTemplate(Guid templateId, [FromBody] NotificationTemplate template)
    {
        try
        {
            var tenantId = _tenantService.GetCurrentTenantId();
            template.Id = templateId;
            template.TenantId = tenantId;

            var updated = await _templateService.UpdateTemplateAsync(template,tenantId);

            if (!updated)
            {
                return NotFound(new { message = "Template not found" });
            }

            return Ok(new { message = "Template updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating notification template {TemplateId}", templateId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpDelete("templates/{templateName}")]
    public async Task<IActionResult> DeleteTemplate(string templateName)
    {
        try
        {
            var tenantId = _tenantService.GetCurrentTenantId();
            var deleted = await _templateService.DeleteTemplateAsync(templateName, tenantId);

            if (!deleted)
            {
                return NotFound(new { message = "Template not found" });
            }

            return Ok(new { message = "Template deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting notification template {TemplateName}", templateName);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }
}

public class TestSystemRequest
{
    public string? TestEmail { get; set; }
}
