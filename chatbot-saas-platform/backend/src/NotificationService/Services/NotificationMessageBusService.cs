using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Shared.Infrastructure.Messaging;
using Shared.Domain.Events;
using NotificationService.Handlers;
using Hangfire;

namespace NotificationService.Services;

public class NotificationMessageBusService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IMessageBus _messageBus;
    private readonly ILogger<NotificationMessageBusService> _logger;

    public NotificationMessageBusService(
        IServiceProvider serviceProvider,
        IMessageBus messageBus,
        ILogger<NotificationMessageBusService> logger)
    {
        _serviceProvider = serviceProvider;
        _messageBus = messageBus;
        _logger = logger;
    }

    /// <summary>
    /// Initialize message bus subscriptions using Hangfire background jobs
    /// </summary>
    public void InitializeSubscriptions()
    {
        _logger.LogInformation("Initializing Notification Message Bus subscriptions with Hangfire");

        try
        {
            // Subscribe to notification events using Hangfire
            _messageBus.SubscribeAsync<NotificationCreatedEvent>(
                async (notificationEvent) =>
                {
                    BackgroundJob.Enqueue(() => ProcessNotificationCreatedAsync(notificationEvent));
                    return true;
                },
                "notification-service.notification-created",
                "notifications",
                "notification.created"
            );

            _messageBus.SubscribeAsync<BulkNotificationEvent>(
                async (bulkEvent) =>
                {
                    BackgroundJob.Enqueue(() => ProcessBulkNotificationAsync(bulkEvent));
                    return true;
                },
                "notification-service.bulk-notification",
                "notifications",
                "notification.bulk"
            );

            // Subscribe to conversation events using Hangfire
            _messageBus.SubscribeAsync<ConversationAssignedEvent>(
                async (assignedEvent) =>
                {
                    BackgroundJob.Enqueue(() => ProcessConversationAssignedAsync(assignedEvent));
                    return true;
                },
                "notification-service.conversation-assigned",
                "conversations",
                "conversation.assigned"
            );

            _messageBus.SubscribeAsync<NewMessageEvent>(
                async (messageEvent) =>
                {
                    BackgroundJob.Enqueue(() => ProcessNewMessageAsync(messageEvent));
                    return true;
                },
                "notification-service.new-message",
                "conversations",
                "message.new"
            );

            _messageBus.SubscribeAsync<ConversationEscalatedEvent>(
                async (escalatedEvent) =>
                {
                    BackgroundJob.Enqueue(() => ProcessConversationEscalatedAsync(escalatedEvent));
                    return true;
                },
                "notification-service.conversation-escalated",
                "conversations",
                "conversation.escalated"
            );

            // Subscribe to system events using Hangfire
            _messageBus.SubscribeAsync<SystemAlertEvent>(
                async (alertEvent) =>
                {
                    // Use high priority for system alerts
                    BackgroundJob.Enqueue(() => ProcessSystemAlertAsync(alertEvent));
                    return true;
                },
                "notification-service.system-alert",
                "system",
                "system.alert"
            );

            // Set up recurring jobs for maintenance tasks
            RecurringJob.AddOrUpdate(
                "cleanup-old-notifications",
                () => CleanupOldNotificationsAsync(),
                Cron.Daily(2, 0) // Run daily at 2 AM
            );

            RecurringJob.AddOrUpdate(
                "retry-failed-notifications",
                () => RetryFailedNotificationsAsync(),
                Cron.Hourly // Run every hour
            );

            RecurringJob.AddOrUpdate(
                "process-scheduled-notifications",
                () => ProcessScheduledNotificationsAsync(),
                Cron.Minutely // Run every minute
            );

            _logger.LogInformation("Message bus subscriptions initialized successfully with Hangfire");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize message bus subscriptions");
            throw;
        }
    }

    /// <summary>
    /// Process notification created event using Hangfire
    /// </summary>
    [Queue("notifications")]
    public async Task ProcessNotificationCreatedAsync(NotificationCreatedEvent notificationEvent)
    {
        try
        {
            _logger.LogInformation("Processing notification created event: {NotificationId}", notificationEvent.NotificationId);
            
            using var scope = _serviceProvider.CreateScope();
            var handler = scope.ServiceProvider.GetRequiredService<NotificationEventHandlers>();
            await handler.HandleNotificationCreatedAsync(notificationEvent);
            
            _logger.LogInformation("Successfully processed notification created event: {NotificationId}", notificationEvent.NotificationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process notification created event: {NotificationId}", notificationEvent.NotificationId);
            throw; // Hangfire will handle retries
        }
    }

    /// <summary>
    /// Process bulk notification event using Hangfire
    /// </summary>
    [Queue("bulk-notifications")]
    public async Task ProcessBulkNotificationAsync(BulkNotificationEvent bulkEvent)
    {
        try
        {
            _logger.LogInformation("Processing bulk notification event for tenant: {TenantId}", bulkEvent.TenantId);
            
            using var scope = _serviceProvider.CreateScope();
            var handler = scope.ServiceProvider.GetRequiredService<NotificationEventHandlers>();
            await handler.HandleBulkNotificationAsync(bulkEvent);
            
            _logger.LogInformation("Successfully processed bulk notification event for tenant: {TenantId}", bulkEvent.TenantId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process bulk notification event for tenant: {TenantId}", bulkEvent.TenantId);
            throw;
        }
    }

    /// <summary>
    /// Process conversation assigned event using Hangfire
    /// </summary>
    [Queue("conversations")]
    public async Task ProcessConversationAssignedAsync(ConversationAssignedEvent assignedEvent)
    {
        try
        {
            _logger.LogInformation("Processing conversation assigned event: {ConversationId}", assignedEvent.ConversationId);
            
            using var scope = _serviceProvider.CreateScope();
            var handler = scope.ServiceProvider.GetRequiredService<NotificationEventHandlers>();
            await handler.HandleConversationAssignedAsync(assignedEvent);
            
            _logger.LogInformation("Successfully processed conversation assigned event: {ConversationId}", assignedEvent.ConversationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process conversation assigned event: {ConversationId}", assignedEvent.ConversationId);
            throw;
        }
    }

    /// <summary>
    /// Process new message event using Hangfire
    /// </summary>
    [Queue("messages")]
    public async Task ProcessNewMessageAsync(NewMessageEvent messageEvent)
    {
        try
        {
            _logger.LogInformation("Processing new message event: {MessageId}", messageEvent.MessageId);
            
            using var scope = _serviceProvider.CreateScope();
            var handler = scope.ServiceProvider.GetRequiredService<NotificationEventHandlers>();
            await handler.HandleNewMessageAsync(messageEvent);
            
            _logger.LogInformation("Successfully processed new message event: {MessageId}", messageEvent.MessageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process new message event: {MessageId}", messageEvent.MessageId);
            throw;
        }
    }

    /// <summary>
    /// Process conversation escalated event using Hangfire
    /// </summary>
    [Queue("conversations")]
    public async Task ProcessConversationEscalatedAsync(ConversationEscalatedEvent escalatedEvent)
    {
        try
        {
            _logger.LogInformation("Processing conversation escalated event: {ConversationId}", escalatedEvent.ConversationId);
            
            using var scope = _serviceProvider.CreateScope();
            var handler = scope.ServiceProvider.GetRequiredService<NotificationEventHandlers>();
            await handler.HandleConversationEscalatedAsync(escalatedEvent);
            
            _logger.LogInformation("Successfully processed conversation escalated event: {ConversationId}", escalatedEvent.ConversationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process conversation escalated event: {ConversationId}", escalatedEvent.ConversationId);
            throw;
        }
    }

    /// <summary>
    /// Process system alert event using Hangfire (high priority)
    /// </summary>
    [Queue("system-alerts")]
    public async Task ProcessSystemAlertAsync(SystemAlertEvent alertEvent)
    {
        try
        {
            _logger.LogInformation("Processing system alert event: {AlertType}", alertEvent.AlertType);
            
            using var scope = _serviceProvider.CreateScope();
            var handler = scope.ServiceProvider.GetRequiredService<NotificationEventHandlers>();
            await handler.HandleSystemAlertAsync(alertEvent);
            
            _logger.LogInformation("Successfully processed system alert event: {AlertType}", alertEvent.AlertType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process system alert event: {AlertType}", alertEvent.AlertType);
            throw;
        }
    }

    /// <summary>
    /// Cleanup old notifications (recurring job)
    /// </summary>
    [Queue("maintenance")]
    public async Task CleanupOldNotificationsAsync()
    {
        try
        {
            _logger.LogInformation("Starting cleanup of old notifications");
            
            using var scope = _serviceProvider.CreateScope();
            var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
            
            // Clean up notifications older than 90 days
            var cutoffDate = DateTime.UtcNow.AddDays(-90);
            await notificationService.CleanupOldNotificationsAsync(cutoffDate);
            
            _logger.LogInformation("Completed cleanup of old notifications");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup old notifications");
            throw;
        }
    }

    /// <summary>
    /// Retry failed notifications (recurring job)
    /// </summary>
    [Queue("retry")]
    public async Task RetryFailedNotificationsAsync()
    {
        try
        {
            _logger.LogInformation("Starting retry of failed notifications");
            
            using var scope = _serviceProvider.CreateScope();
            var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
            
            // Retry failed notifications that haven't exceeded max retry count
            await notificationService.RetryFailedNotificationsAsync();
            
            _logger.LogInformation("Completed retry of failed notifications");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retry failed notifications");
            throw;
        }
    }

    /// <summary>
    /// Process scheduled notifications (recurring job)
    /// </summary>
    [Queue("scheduled")]
    public async Task ProcessScheduledNotificationsAsync()
    {
        try
        {
            _logger.LogDebug("Processing scheduled notifications");
            
            using var scope = _serviceProvider.CreateScope();
            var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
            
            // Process notifications that are scheduled to be sent now
            await notificationService.ProcessScheduledNotificationsAsync();
            
            _logger.LogDebug("Completed processing scheduled notifications");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process scheduled notifications");
            throw;
        }
    }
}