using NotificationService.Models;

namespace NotificationService.Services;

public interface IPushNotificationService
{
    Task<bool> SendPushNotificationAsync(string deviceToken, string title, string content, Dictionary<string, object>? data = null);
    Task<bool> SendBulkPushNotificationAsync(List<string> deviceTokens, string title, string content, Dictionary<string, object>? data = null);
    Task<bool> SendTopicNotificationAsync(string topic, string title, string content, Dictionary<string, object>? data = null);
    Task<bool> SubscribeToTopicAsync(string deviceToken, string topic);
    Task<bool> UnsubscribeFromTopicAsync(string deviceToken, string topic);
}
