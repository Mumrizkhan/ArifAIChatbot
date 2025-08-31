using Shared.Application.Common.Interfaces;
using Shared.Domain.Events;
using Shared.Domain.Entities;

namespace Shared.Infrastructure.Services;

/// <summary>
/// Example service demonstrating how to use the message bus notification system
/// </summary>
public class NotificationExampleService
{
    private readonly IMessageBusNotificationService _notificationService;

    public NotificationExampleService(IMessageBusNotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    /// <summary>
    /// Example: Send a welcome notification to a new user
    /// </summary>
    public async Task SendWelcomeNotificationAsync(Guid userId, Guid tenantId, string userName)
    {
        var welcomeNotification = new NotificationCreatedEvent
        {
            NotificationId = Guid.NewGuid(),
            UserId = userId,
            TenantId = tenantId,
            Title = "Welcome to ChatBot SaaS!",
            Content = $"Welcome {userName}! Thank you for joining our platform.",
            Type = NotificationType.Welcome,
            Channels = new List<NotificationChannel> 
            { 
                NotificationChannel.InApp, 
                NotificationChannel.Email 
            },
            Data = new Dictionary<string, object>
            {
                ["userName"] = userName,
                ["welcomeDate"] = DateTime.UtcNow
            },
            Language = "en"
        };

        await _notificationService.PublishNotificationAsync(welcomeNotification);
    }

    /// <summary>
    /// Example: Send a billing alert to all administrators
    /// </summary>
    public async Task SendBillingAlertAsync(Guid tenantId, List<Guid> adminUserIds, 
        string alertMessage, decimal currentUsage, decimal limit)
    {
        var billingAlert = new BulkNotificationEvent
        {
            TenantId = tenantId,
            UserIds = adminUserIds,
            Title = "Billing Alert",
            Content = alertMessage,
            Type = NotificationType.BillingAlert,
            Channels = new List<NotificationChannel> 
            { 
                NotificationChannel.InApp, 
                NotificationChannel.Email,
                NotificationChannel.SMS 
            },
            Data = new Dictionary<string, object>
            {
                ["currentUsage"] = currentUsage,
                ["usageLimit"] = limit,
                ["usagePercentage"] = (currentUsage / limit) * 100,
                ["alertDate"] = DateTime.UtcNow
            },
            Language = "en"
        };

        await _notificationService.PublishBulkNotificationAsync(billingAlert);
    }

    /// <summary>
    /// Example: Send a security alert
    /// </summary>
    public async Task SendSecurityAlertAsync(Guid tenantId, string alertType, 
        string description, Dictionary<string, object> securityData)
    {
        var securityAlert = new SystemAlertEvent
        {
            AlertType = alertType,
            Title = "Security Alert",
            Description = description,
            Severity = "Critical",
            TenantId = tenantId,
            Metadata = securityData
        };

        await _notificationService.PublishSystemAlertAsync(securityAlert);
    }

    /// <summary>
    /// Example: Notify when an agent goes offline
    /// </summary>
    public async Task SendAgentOfflineNotificationAsync(Guid tenantId, List<Guid> supervisorIds, 
        string agentName, Guid agentId)
    {
        var agentOfflineEvent = new BulkNotificationEvent
        {
            TenantId = tenantId,
            UserIds = supervisorIds,
            Title = "Agent Offline",
            Content = $"Agent {agentName} has gone offline and may have active conversations that need reassignment.",
            Type = NotificationType.AgentOffline,
            Channels = new List<NotificationChannel> 
            { 
                NotificationChannel.InApp,
                NotificationChannel.Push 
            },
            Data = new Dictionary<string, object>
            {
                ["agentId"] = agentId,
                ["agentName"] = agentName,
                ["offlineTime"] = DateTime.UtcNow
            },
            Language = "en"
        };

        await _notificationService.PublishBulkNotificationAsync(agentOfflineEvent);
    }
}