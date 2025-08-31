using Microsoft.Extensions.Logging;
using Shared.Application.Common.Interfaces;
using Shared.Domain.Events;
using Shared.Domain.Entities;
using Shared.Infrastructure.Messaging;

namespace Shared.Infrastructure.Services;

public class MessageBusNotificationService : IMessageBusNotificationService
{
    private readonly IMessageBus _messageBus;
    private readonly ILogger<MessageBusNotificationService> _logger;

    public MessageBusNotificationService(
        IMessageBus messageBus,
        ILogger<MessageBusNotificationService> logger)
    {
        _messageBus = messageBus;
        _logger = logger;
    }

    public async Task PublishNotificationAsync(NotificationCreatedEvent notificationEvent)
    {
        try
        {
            await _messageBus.PublishAsync(
                notificationEvent, 
                "notification.created", 
                "notifications"
            );

            _logger.LogInformation("Published notification created event for notification {NotificationId}", 
                notificationEvent.NotificationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish notification created event for notification {NotificationId}", 
                notificationEvent.NotificationId);
            throw;
        }
    }

    public async Task PublishBulkNotificationAsync(BulkNotificationEvent bulkEvent)
    {
        try
        {
            await _messageBus.PublishAsync(
                bulkEvent, 
                "notification.bulk", 
                "notifications"
            );

            _logger.LogInformation("Published bulk notification event for tenant {TenantId}", 
                bulkEvent.TenantId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish bulk notification event for tenant {TenantId}", 
                bulkEvent.TenantId);
            throw;
        }
    }

    public async Task PublishNotificationReadAsync(NotificationReadEvent readEvent)
    {
        try
        {
            await _messageBus.PublishAsync(
                readEvent, 
                "notification.read", 
                "notifications"
            );

            _logger.LogDebug("Published notification read event for notification {NotificationId}", 
                readEvent.NotificationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish notification read event for notification {NotificationId}", 
                readEvent.NotificationId);
            throw;
        }
    }

    public async Task PublishConversationAssignedAsync(ConversationAssignedEvent assignedEvent)
    {
        try
        {
            await _messageBus.PublishAsync(
                assignedEvent, 
                "conversation.assigned", 
                "conversations"
            );

            _logger.LogInformation("Published conversation assigned event for conversation {ConversationId}", 
                assignedEvent.ConversationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish conversation assigned event for conversation {ConversationId}", 
                assignedEvent.ConversationId);
            throw;
        }
    }

    public async Task PublishNewMessageAsync(NewMessageEvent messageEvent)
    {
        try
        {
            await _messageBus.PublishAsync(
                messageEvent, 
                "message.new", 
                "conversations"
            );

            _logger.LogDebug("Published new message event for message {MessageId}", 
                messageEvent.MessageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish new message event for message {MessageId}", 
                messageEvent.MessageId);
            throw;
        }
    }

    public async Task PublishConversationEscalatedAsync(ConversationEscalatedEvent escalatedEvent)
    {
        try
        {
            await _messageBus.PublishAsync(
                escalatedEvent, 
                "conversation.escalated", 
                "conversations"
            );

            _logger.LogInformation("Published conversation escalated event for conversation {ConversationId}", 
                escalatedEvent.ConversationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish conversation escalated event for conversation {ConversationId}", 
                escalatedEvent.ConversationId);
            throw;
        }
    }

    public async Task PublishSystemAlertAsync(SystemAlertEvent alertEvent)
    {
        try
        {
            await _messageBus.PublishAsync(
                alertEvent, 
                "system.alert", 
                "system"
            );

            _logger.LogInformation("Published system alert event: {AlertType}", 
                alertEvent.AlertType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish system alert event: {AlertType}", 
                alertEvent.AlertType);
            throw;
        }
    }
}