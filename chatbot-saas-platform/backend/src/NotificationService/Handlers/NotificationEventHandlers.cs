using Microsoft.Extensions.Logging;
using Shared.Infrastructure.Messaging;
using Shared.Infrastructure.Messaging.Events;
using Shared.Domain.Events;
using NotificationService.Services;
using NotificationService.Models;
using Shared.Domain.Entities;

namespace NotificationService.Handlers;

public class NotificationEventHandlers
{
    private readonly INotificationService _notificationService;
    private readonly ILogger<NotificationEventHandlers> _logger;

    public NotificationEventHandlers(
        INotificationService notificationService,
        ILogger<NotificationEventHandlers> logger)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task<bool> HandleNotificationCreatedAsync(NotificationCreatedEvent notificationEvent)
    {
        try
        {
            var request = new SendNotificationRequest
            {
                Title = notificationEvent.Title,
                Content = notificationEvent.Content,
                Type = notificationEvent.Type,
                Channels = notificationEvent.Channels,
                UserId = notificationEvent.UserId,
                RecipientEmail = notificationEvent.RecipientEmail,
                RecipientPhone = notificationEvent.RecipientPhone,
                RecipientDeviceToken = notificationEvent.RecipientDeviceToken,
                Data = notificationEvent.Data,
                TemplateId = notificationEvent.TemplateId,
                TemplateData = notificationEvent.TemplateData,
                ScheduledAt = notificationEvent.ScheduledAt,
                Language = notificationEvent.Language
            };

            await _notificationService.SendNotificationAsync(request, notificationEvent.TenantId);
            
            _logger.LogInformation("Processed notification created event: {NotificationId}", 
                notificationEvent.NotificationId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process notification created event: {NotificationId}", 
                notificationEvent.NotificationId);
            return false;
        }
    }

    public async Task<bool> HandleBulkNotificationAsync(BulkNotificationEvent bulkEvent)
    {
        try
        {
            var request = new BulkNotificationRequest
            {
                Title = bulkEvent.Title,
                Content = bulkEvent.Content,
                Type = bulkEvent.Type,
                Channels = bulkEvent.Channels,
                UserIds = bulkEvent.UserIds,
                RecipientEmails = bulkEvent.RecipientEmails,
                RecipientPhones = bulkEvent.RecipientPhones,
                Data = bulkEvent.Data,
                TemplateId = bulkEvent.TemplateId,
                TemplateData = bulkEvent.TemplateData,
                ScheduledAt = bulkEvent.ScheduledAt,
                Language = bulkEvent.Language
            };

            await _notificationService.SendBulkNotificationAsync(request, bulkEvent.TenantId);
            
            _logger.LogInformation("Processed bulk notification event for tenant: {TenantId}", 
                bulkEvent.TenantId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process bulk notification event for tenant: {TenantId}", 
                bulkEvent.TenantId);
            return false;
        }
    }

    public async Task<bool> HandleConversationAssignedAsync(ConversationAssignedEvent assignedEvent)
    {
        try
        {
            var request = new SendNotificationRequest
            {
                Title = "Conversation Assigned",
                Content = $"You have been assigned to a conversation with {assignedEvent.CustomerName}",
                Type = NotificationType.ConversationAssigned,
                Channels = new List<NotificationChannel> 
                { 
                    NotificationChannel.InApp, 
                    NotificationChannel.Email 
                },
                UserId = assignedEvent.AgentId,
                Data = new Dictionary<string, object>
                {
                    ["conversationId"] = assignedEvent.ConversationId,
                    ["customerName"] = assignedEvent.CustomerName,
                    ["assignedAt"] = assignedEvent.AssignedAt
                },
                Language = "en"
            };

            await _notificationService.SendNotificationAsync(request, assignedEvent.TenantId);
            
            _logger.LogInformation("Sent conversation assigned notification for conversation: {ConversationId}", 
                assignedEvent.ConversationId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send conversation assigned notification for conversation: {ConversationId}", 
                assignedEvent.ConversationId);
            return false;
        }
    }

    public async Task<bool> HandleNewMessageAsync(NewMessageEvent messageEvent)
    {
        try
        {
            var request = new SendNotificationRequest
            {
                Title = $"New message from {messageEvent.SenderName}",
                Content = messageEvent.Content.Length > 100 
                    ? messageEvent.Content.Substring(0, 100) + "..." 
                    : messageEvent.Content,
                Type = NotificationType.NewMessage,
                Channels = new List<NotificationChannel> 
                { 
                    NotificationChannel.InApp,
                    NotificationChannel.Push 
                },
                Data = new Dictionary<string, object>
                {
                    ["messageId"] = messageEvent.MessageId,
                    ["conversationId"] = messageEvent.ConversationId,
                    ["senderId"] = messageEvent.SenderId ?? Guid.Empty,
                    ["senderName"] = messageEvent.SenderName,
                    ["messageType"] = messageEvent.MessageType
                },
                Language = "en"
            };

            // TODO: Determine the recipient agent for this conversation
            // This would typically involve querying the conversation to find assigned agent
            
            _logger.LogInformation("Processed new message event for message: {MessageId}", 
                messageEvent.MessageId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process new message event for message: {MessageId}", 
                messageEvent.MessageId);
            return false;
        }
    }

    public async Task<bool> HandleConversationEscalatedAsync(ConversationEscalatedEvent escalatedEvent)
    {
        try
        {
            var request = new SendNotificationRequest
            {
                Title = "Conversation Escalated",
                Content = $"A conversation has been escalated to you. Reason: {escalatedEvent.Reason}",
                Type = NotificationType.ConversationEscalated,
                Channels = new List<NotificationChannel> 
                { 
                    NotificationChannel.InApp, 
                    NotificationChannel.Email,
                    NotificationChannel.SMS 
                },
                UserId = escalatedEvent.ToAgentId,
                Data = new Dictionary<string, object>
                {
                    ["conversationId"] = escalatedEvent.ConversationId,
                    ["fromAgentId"] = escalatedEvent.FromAgentId ?? Guid.Empty,
                    ["reason"] = escalatedEvent.Reason,
                    ["escalatedAt"] = escalatedEvent.EscalatedAt
                },
                Language = "en"
            };

            await _notificationService.SendNotificationAsync(request, escalatedEvent.TenantId);
            
            _logger.LogInformation("Sent conversation escalated notification for conversation: {ConversationId}", 
                escalatedEvent.ConversationId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send conversation escalated notification for conversation: {ConversationId}", 
                escalatedEvent.ConversationId);
            return false;
        }
    }

    public async Task<bool> HandleSystemAlertAsync(SystemAlertEvent alertEvent)
    {
        try
        {
            // Send to all admins of the tenant
            var bulkRequest = new BulkNotificationRequest
            {
                Title = alertEvent.Title,
                Content = alertEvent.Description,
                Type = NotificationType.SystemAlert,
                Channels = new List<NotificationChannel> 
                { 
                    NotificationChannel.InApp, 
                    NotificationChannel.Email 
                },
                Data = new Dictionary<string, object>
                {
                    ["alertType"] = alertEvent.AlertType,
                    ["severity"] = alertEvent.Severity,
                    ["metadata"] = alertEvent.Metadata
                },
                Language = "en"
            };

            // TODO: Get admin user IDs for the tenant
            // bulkRequest.UserIds = await GetTenantAdminIds(alertEvent.TenantId);

            await _notificationService.SendBulkNotificationAsync(bulkRequest, alertEvent.TenantId);
            
            _logger.LogInformation("Sent system alert notification: {AlertType}", 
                alertEvent.AlertType);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send system alert notification: {AlertType}", 
                alertEvent.AlertType);
            return false;
        }
    }

    // Legacy handlers for backward compatibility
    // Note: EmailQueuedEvent and SmsQueuedEvent handlers removed as these events don't exist
    // Individual email and SMS sending is handled through the main notification processing pipeline
}
