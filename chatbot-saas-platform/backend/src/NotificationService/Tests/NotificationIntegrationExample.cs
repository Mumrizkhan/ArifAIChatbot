using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shared.Application.Common.Interfaces;
using Shared.Domain.Events;
using Shared.Domain.Entities;

namespace NotificationService.Tests;

/// <summary>
/// Integration test example for the notification system
/// This demonstrates how to test the complete notification flow
/// </summary>
public class NotificationIntegrationExample
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<NotificationIntegrationExample> _logger;

    public NotificationIntegrationExample(IServiceProvider serviceProvider, ILogger<NotificationIntegrationExample> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <summary>
    /// Test complete notification flow from event publishing to delivery
    /// </summary>
    public async Task TestCompleteNotificationFlowAsync()
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var notificationService = scope.ServiceProvider.GetRequiredService<IMessageBusNotificationService>();

            // Test 1: Welcome notification
            await TestWelcomeNotificationAsync(notificationService);

            // Test 2: Conversation assignment notification  
            await TestConversationAssignmentAsync(notificationService);

            // Test 3: Bulk notification
            await TestBulkNotificationAsync(notificationService);

            // Test 4: System alert
            await TestSystemAlertAsync(notificationService);

            _logger.LogInformation("All notification integration tests completed successfully!");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Notification integration test failed");
            throw;
        }
    }

    private async Task TestWelcomeNotificationAsync(IMessageBusNotificationService notificationService)
    {
        _logger.LogInformation("Testing welcome notification...");

        var welcomeEvent = new NotificationCreatedEvent
        {
            NotificationId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            Title = "Welcome to ChatBot SaaS!",
            Content = "Thank you for joining our platform. Get started by creating your first chatbot.",
            Type = NotificationType.Welcome,
            Channels = new List<NotificationChannel> 
            { 
                NotificationChannel.InApp, 
                NotificationChannel.Email 
            },
            Data = new Dictionary<string, object>
            {
                ["welcomeStep"] = "1",
                ["nextAction"] = "create_chatbot"
            },
            Language = "en"
        };

        await notificationService.PublishNotificationAsync(welcomeEvent);
        _logger.LogInformation("Welcome notification published successfully");
    }

    private async Task TestConversationAssignmentAsync(IMessageBusNotificationService notificationService)
    {
        _logger.LogInformation("Testing conversation assignment notification...");

        var assignmentEvent = new ConversationAssignedEvent
        {
            ConversationId = Guid.NewGuid(),
            AgentId = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            CustomerName = "John Doe",
            AssignedAt = DateTime.UtcNow
        };

        await notificationService.PublishConversationAssignedAsync(assignmentEvent);
        _logger.LogInformation("Conversation assignment notification published successfully");
    }

    private async Task TestBulkNotificationAsync(IMessageBusNotificationService notificationService)
    {
        _logger.LogInformation("Testing bulk notification...");

        var bulkEvent = new BulkNotificationEvent
        {
            TenantId = Guid.NewGuid(),
            UserIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() },
            Title = "System Maintenance Scheduled",
            Content = "Our system will undergo maintenance tonight from 2:00 AM to 4:00 AM EST.",
            Type = NotificationType.SystemAlert,
            Channels = new List<NotificationChannel> 
            { 
                NotificationChannel.InApp, 
                NotificationChannel.Email 
            },
            Data = new Dictionary<string, object>
            {
                ["maintenanceStart"] = "2024-01-15T02:00:00Z",
                ["maintenanceEnd"] = "2024-01-15T04:00:00Z",
                ["impact"] = "minimal"
            },
            Language = "en"
        };

        await notificationService.PublishBulkNotificationAsync(bulkEvent);
        _logger.LogInformation("Bulk notification published successfully");
    }

    private async Task TestSystemAlertAsync(IMessageBusNotificationService notificationService)
    {
        _logger.LogInformation("Testing system alert...");

        var systemAlert = new SystemAlertEvent
        {
            AlertType = "HighResourceUsage",
            Title = "High Resource Usage Alert",
            Description = "System resource usage has exceeded 85% threshold",
            Severity = "Warning",
            TenantId = Guid.NewGuid(),
            Metadata = new Dictionary<string, object>
            {
                ["cpuUsage"] = 87.5,
                ["memoryUsage"] = 82.3,
                ["alertThreshold"] = 85.0
            }
        };

        await notificationService.PublishSystemAlertAsync(systemAlert);
        _logger.LogInformation("System alert published successfully");
    }
}