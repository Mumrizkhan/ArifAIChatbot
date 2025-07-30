using NotificationService.Models;

namespace NotificationService.Services;

public interface IEmailService
{
    Task<bool> SendEmailAsync(string to, string subject, string content, string? templateId = null, Dictionary<string, object>? templateData = null);
    Task<bool> SendBulkEmailAsync(List<string> recipients, string subject, string content, string? templateId = null, Dictionary<string, object>? templateData = null);
    Task<bool> ValidateEmailAsync(string email);
    Task<EmailDeliveryStatus> GetDeliveryStatusAsync(string messageId);
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
