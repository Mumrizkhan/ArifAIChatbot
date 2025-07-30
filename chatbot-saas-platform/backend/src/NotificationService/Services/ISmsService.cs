using NotificationService.Models;

namespace NotificationService.Services;

public interface ISmsService
{
    Task<bool> SendSmsAsync(string to, string content, string? templateId = null, Dictionary<string, object>? templateData = null);
    Task<bool> SendBulkSmsAsync(List<string> recipients, string content, string? templateId = null, Dictionary<string, object>? templateData = null);
    Task<bool> ValidatePhoneNumberAsync(string phoneNumber);
    Task<SmsDeliveryStatus> GetDeliveryStatusAsync(string messageId);
}

public class SmsDeliveryStatus
{
    public string MessageId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? DeliveredAt { get; set; }
    public string? ErrorMessage { get; set; }
}
