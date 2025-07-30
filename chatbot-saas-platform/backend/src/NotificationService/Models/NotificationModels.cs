using Shared.Domain.Common;
using Shared.Domain.Entities;

namespace NotificationService.Models;

//public class Notification : AuditableEntity
//{
//    public Guid Id { get; set; }
//    public string Title { get; set; } = string.Empty;
//    public string Content { get; set; } = string.Empty;
//    public NotificationType Type { get; set; }
//    public NotificationChannel Channel { get; set; }
//    public NotificationStatus Status { get; set; } = NotificationStatus.Pending;
//    public Guid TenantId { get; set; }
//    public Guid? UserId { get; set; }
//    public string? RecipientEmail { get; set; }
//    public string? RecipientPhone { get; set; }
//    public string? RecipientDeviceToken { get; set; }
//    public Dictionary<string, object> Data { get; set; } = new();
//    public string? TemplateId { get; set; }
//    public Dictionary<string, object> TemplateData { get; set; } = new();
//    public DateTime? ScheduledAt { get; set; }
//    public DateTime? SentAt { get; set; }
//    public DateTime? DeliveredAt { get; set; }
//    public DateTime? ReadAt { get; set; }
//    public string? ErrorMessage { get; set; }
//    public int RetryCount { get; set; }
//    public int MaxRetries { get; set; } = 3;
//    public string? ExternalId { get; set; }
//    public string Language { get; set; } = "en";
//}

//public class NotificationTemplate : AuditableEntity
//{
//    public Guid Id { get; set; }
//    public string Name { get; set; } = string.Empty;
//    public string Subject { get; set; } = string.Empty;
//    public string Content { get; set; } = string.Empty;
//    public NotificationType Type { get; set; }
//    public NotificationChannel Channel { get; set; }
//    public Guid TenantId { get; set; }
//    public string Language { get; set; } = "en";
//    public bool IsActive { get; set; } = true;
//    public Dictionary<string, object> DefaultData { get; set; } = new();
//}

//public class NotificationPreference : AuditableEntity
//{
//    public Guid Id { get; set; }
//    public Guid UserId { get; set; }
//    public Guid TenantId { get; set; }
//    public NotificationType NotificationType { get; set; }
//    public List<NotificationChannel> EnabledChannels { get; set; } = new();
//    public bool IsEnabled { get; set; } = true;
//    public Dictionary<string, object> Settings { get; set; } = new();
//}

//public enum NotificationType
//{
//    Welcome,
//    ConversationAssigned,
//    ConversationEscalated,
//    ConversationResolved,
//    NewMessage,
//    AgentOffline,
//    SystemAlert,
//    BillingAlert,
//    SecurityAlert,
//    Custom
//}

//public enum NotificationChannel
//{
//    Email,
//    SMS,
//    Push,
//    InApp,
//    Webhook
//}

//public enum NotificationStatus
//{
//    Pending,
//    Scheduled,
//    Sending,
//    Sent,
//    Delivered,
//    Read,
//    Failed,
//    Cancelled
//}

public class SendNotificationRequest
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public NotificationType Type { get; set; }
    public List<NotificationChannel> Channels { get; set; } = new();
    public Guid? UserId { get; set; }
    public string? RecipientEmail { get; set; }
    public string? RecipientPhone { get; set; }
    public string? RecipientDeviceToken { get; set; }
    public Dictionary<string, object> Data { get; set; } = new();
    public string? TemplateId { get; set; }
    public Dictionary<string, object> TemplateData { get; set; } = new();
    public DateTime? ScheduledAt { get; set; }
    public string Language { get; set; } = "en";
}

public class BulkNotificationRequest
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public NotificationType Type { get; set; }
    public List<NotificationChannel> Channels { get; set; } = new();
    public List<Guid>? UserIds { get; set; }
    public List<string>? RecipientEmails { get; set; }
    public List<string>? RecipientPhones { get; set; }
    public Dictionary<string, object> Data { get; set; } = new();
    public string? TemplateId { get; set; }
    public Dictionary<string, object> TemplateData { get; set; } = new();
    public DateTime? ScheduledAt { get; set; }
    public string Language { get; set; } = "en";
}

public class NotificationStatistics
{
    public int TotalSent { get; set; }
    public int TotalDelivered { get; set; }
    public int TotalRead { get; set; }
    public int TotalFailed { get; set; }
    public double DeliveryRate { get; set; }
    public double ReadRate { get; set; }
    public Dictionary<NotificationChannel, int> ByChannel { get; set; } = new();
    public Dictionary<NotificationType, int> ByType { get; set; } = new();
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
}
