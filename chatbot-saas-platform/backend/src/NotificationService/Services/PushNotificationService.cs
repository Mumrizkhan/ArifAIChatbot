using System.Text.Json;
using NotificationService.Services;
using NotificationService.Models;

namespace NotificationService.Services;

public class PushNotificationService : IPushNotificationService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<PushNotificationService> _logger;
    private readonly IConfiguration _configuration;

    public PushNotificationService(
        HttpClient httpClient,
        ILogger<PushNotificationService> logger,
        IConfiguration configuration)
    {
        _httpClient = httpClient;
        _logger = logger;
        _configuration = configuration;
        
        var serverKey = _configuration["Firebase:ServerKey"];
        if (!string.IsNullOrEmpty(serverKey))
        {
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"key={serverKey}");
            _httpClient.DefaultRequestHeaders.Add("Content-Type", "application/json");
        }
    }

    public async Task<bool> SendPushNotificationAsync(string deviceToken, string title, string content, Dictionary<string, object>? data = null)
    {
        try
        {
            var payload = new
            {
                to = deviceToken,
                notification = new
                {
                    title = title,
                    body = content,
                    sound = "default"
                },
                data = data ?? new Dictionary<string, object>()
            };

            var json = JsonSerializer.Serialize(payload);
            var httpContent = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("https://fcm.googleapis.com/fcm/send", httpContent);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Push notification sent successfully to device {DeviceToken}", deviceToken);
                return true;
            }
            else
            {
                var responseBody = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to send push notification to device {DeviceToken}. Status: {Status}, Response: {Response}", 
                    deviceToken, response.StatusCode, responseBody);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending push notification to device {DeviceToken}", deviceToken);
            return false;
        }
    }

    public async Task<bool> SendBulkPushNotificationAsync(List<string> deviceTokens, string title, string content, Dictionary<string, object>? data = null)
    {
        try
        {
            var payload = new
            {
                registration_ids = deviceTokens,
                notification = new
                {
                    title = title,
                    body = content,
                    sound = "default"
                },
                data = data ?? new Dictionary<string, object>()
            };

            var json = JsonSerializer.Serialize(payload);
            var httpContent = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("https://fcm.googleapis.com/fcm/send", httpContent);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Bulk push notification sent successfully to {Count} devices", deviceTokens.Count);
                return true;
            }
            else
            {
                var responseBody = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to send bulk push notification. Status: {Status}, Response: {Response}", 
                    response.StatusCode, responseBody);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending bulk push notification to {Count} devices", deviceTokens.Count);
            return false;
        }
    }

    public async Task<bool> SendTopicNotificationAsync(string topic, string title, string content, Dictionary<string, object>? data = null)
    {
        try
        {
            var payload = new
            {
                to = $"/topics/{topic}",
                notification = new
                {
                    title = title,
                    body = content,
                    sound = "default"
                },
                data = data ?? new Dictionary<string, object>()
            };

            var json = JsonSerializer.Serialize(payload);
            var httpContent = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("https://fcm.googleapis.com/fcm/send", httpContent);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Topic notification sent successfully to topic {Topic}", topic);
                return true;
            }
            else
            {
                var responseBody = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to send topic notification to {Topic}. Status: {Status}, Response: {Response}", 
                    topic, response.StatusCode, responseBody);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending topic notification to {Topic}", topic);
            return false;
        }
    }

    public async Task<bool> SubscribeToTopicAsync(string deviceToken, string topic)
    {
        try
        {
            var payload = new
            {
                to = $"/topics/{topic}",
                registration_tokens = new[] { deviceToken }
            };

            var json = JsonSerializer.Serialize(payload);
            var httpContent = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("https://iid.googleapis.com/iid/v1:batchAdd", httpContent);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Device {DeviceToken} subscribed to topic {Topic}", deviceToken, topic);
                return true;
            }
            else
            {
                var responseBody = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to subscribe device {DeviceToken} to topic {Topic}. Status: {Status}, Response: {Response}", 
                    deviceToken, topic, response.StatusCode, responseBody);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error subscribing device {DeviceToken} to topic {Topic}", deviceToken, topic);
            return false;
        }
    }

    public async Task<bool> UnsubscribeFromTopicAsync(string deviceToken, string topic)
    {
        try
        {
            var payload = new
            {
                to = $"/topics/{topic}",
                registration_tokens = new[] { deviceToken }
            };

            var json = JsonSerializer.Serialize(payload);
            var httpContent = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("https://iid.googleapis.com/iid/v1:batchRemove", httpContent);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Device {DeviceToken} unsubscribed from topic {Topic}", deviceToken, topic);
                return true;
            }
            else
            {
                var responseBody = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to unsubscribe device {DeviceToken} from topic {Topic}. Status: {Status}, Response: {Response}", 
                    deviceToken, topic, response.StatusCode, responseBody);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unsubscribing device {DeviceToken} from topic {Topic}", deviceToken, topic);
            return false;
        }
    }
}
