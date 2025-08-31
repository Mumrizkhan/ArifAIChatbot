using NotificationService.Models;
using Shared.Domain.Entities;

namespace NotificationService.Services;

public interface IEmailService
{
    Task<bool> SendEmailAsync(string to, string subject, string content, string? templateId = null, Dictionary<string, object>? templateData = null);
    Task<bool> SendBulkEmailAsync(List<string> recipients, string subject, string content, string? templateId = null, Dictionary<string, object>? templateData = null);
    Task<bool> ValidateEmailAsync(string email);
    Task<EmailDeliveryStatus> GetDeliveryStatusAsync(string messageId);
    
    // Enhanced notification-specific methods
    Task<bool> SendNotificationEmailAsync(Notification notification);
    Task TrackEmailDeliveryAsync(string? messageId, string recipientEmail);
    Task UpdateNotificationStatusAsync(Guid notificationId, string status, string? errorMessage = null);
    Task UpdateNotificationByEmailAsync(string recipientEmail, string messageId, string status);
    Task ProcessSendGridWebhookAsync(string webhookData);
}

public class EmailDeliveryStatus
{
    public string MessageId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? DeliveredAt { get; set; }
    public DateTime? OpenedAt { get; set; }
    public DateTime? ClickedAt { get; set; }
    public string? ErrorMessage { get; set; }
}
