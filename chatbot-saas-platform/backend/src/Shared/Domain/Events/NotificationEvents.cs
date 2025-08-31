using Shared.Domain.Entities;

namespace Shared.Domain.Events;

public class NotificationCreatedEvent
{
    public Guid NotificationId { get; set; }
    public Guid TenantId { get; set; }
    public Guid? UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public NotificationType Type { get; set; }
    public List<NotificationChannel> Channels { get; set; } = new();
    public string? RecipientEmail { get; set; }
    public string? RecipientPhone { get; set; }
    public string? RecipientDeviceToken { get; set; }
    public Dictionary<string, object> Data { get; set; } = new();
    public string? TemplateId { get; set; }
    public Dictionary<string, object> TemplateData { get; set; } = new();
    public DateTime? ScheduledAt { get; set; }
    public string Language { get; set; } = "en";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class BulkNotificationEvent
{
    public Guid TenantId { get; set; }
    public List<Guid>? UserIds { get; set; }
    public List<string>? RecipientEmails { get; set; }
    public List<string>? RecipientPhones { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public NotificationType Type { get; set; }
    public List<NotificationChannel> Channels { get; set; } = new();
    public Dictionary<string, object> Data { get; set; } = new();
    public string? TemplateId { get; set; }
    public Dictionary<string, object> TemplateData { get; set; } = new();
    public DateTime? ScheduledAt { get; set; }
    public string Language { get; set; } = "en";
}

public class NotificationReadEvent
{
    public Guid NotificationId { get; set; }
    public Guid UserId { get; set; }
    public Guid TenantId { get; set; }
    public DateTime ReadAt { get; set; } = DateTime.UtcNow;
}

public class ConversationAssignedEvent
{
    public Guid ConversationId { get; set; }
    public Guid AgentId { get; set; }
    public Guid TenantId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
}

public class NewMessageEvent
{
    public Guid MessageId { get; set; }
    public Guid ConversationId { get; set; }
    public Guid? SenderId { get; set; }
    public string SenderName { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string MessageType { get; set; } = "text";
    public Guid TenantId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class ConversationEscalatedEvent
{
    public Guid ConversationId { get; set; }
    public Guid? FromAgentId { get; set; }
    public Guid ToAgentId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public Guid TenantId { get; set; }
    public DateTime EscalatedAt { get; set; } = DateTime.UtcNow;
}

public class SystemAlertEvent
{
    public string AlertType { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Severity { get; set; } = "Info"; // Info, Warning, Error, Critical
    public Guid TenantId { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}