using System.Text.Json;
using Shared.Application.Common.Interfaces;
using LiveAgentService.Models;

namespace LiveAgentService.Services;

public class ServiceResult<T>
{
    public bool IsSuccess { get; set; }
    public T? Data { get; set; }
    public string? ErrorMessage { get; set; }
    
    public static ServiceResult<T> Success(T data) => new() { IsSuccess = true, Data = data };
    public static ServiceResult<T> Failure(string error) => new() { IsSuccess = false, ErrorMessage = error };
}

public interface IChatRuntimeIntegrationService
{
    Task<bool> SendMessageAsync(Guid conversationId, string content, string type = "text", Guid? senderId = null);
    Task<ConversationDetailsDto?> GetConversationDetailsAsync(Guid conversationId);
    Task<List<MessageDto>> GetConversationMessagesAsync(Guid conversationId);
    Task<bool> MarkMessageAsReadAsync(Guid messageId, string readerId, string readerType);
    Task<ServiceResult<object>> SendFeedbackMessageAsync(Guid conversationId, SendFeedbackMessageRequest request);
}

public class ChatRuntimeIntegrationService : IChatRuntimeIntegrationService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ChatRuntimeIntegrationService> _logger;
    private readonly ITenantService _tenantService;
    private readonly string _chatRuntimeServiceUrl;

    public ChatRuntimeIntegrationService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<ChatRuntimeIntegrationService> logger,
        ITenantService tenantService)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
        _tenantService = tenantService;
        _chatRuntimeServiceUrl = _configuration["Services:ChatRuntime"] ?? "http://localhost:5002";
    }

    private void SetTenantHeader()
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        _httpClient.DefaultRequestHeaders.Remove("X-Tenant-ID");
        _httpClient.DefaultRequestHeaders.Add("X-Tenant-ID", tenantId.ToString());
    }

    public async Task<bool> SendMessageAsync(Guid conversationId, string content, string type = "text", Guid? senderId = null)
    {
        try
        {
            SetTenantHeader();
            
            var request = new
            {
                ConversationId = conversationId.ToString(),
                Content = content,
                Type = type,
                SenderId = senderId
            };
            
            var response = await _httpClient.PostAsJsonAsync($"{_chatRuntimeServiceUrl}/api/chat/agent-messages", request);
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Agent message sent successfully to conversation {ConversationId}", conversationId);
                return true;
            }
            
            _logger.LogWarning("Failed to send agent message to conversation {ConversationId}. Status: {StatusCode}", 
                conversationId, response.StatusCode);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending agent message to conversation {ConversationId}", conversationId);
            return false;
        }
    }

    public async Task<ConversationDetailsDto?> GetConversationDetailsAsync(Guid conversationId)
    {
        try
        {
            SetTenantHeader();
            
            var response = await _httpClient.GetAsync($"{_chatRuntimeServiceUrl}/api/conversations/internal/{conversationId}");
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<ConversationDetailsDto>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                return result;
            }
            
            _logger.LogWarning("Failed to get conversation details for {ConversationId}. Status: {StatusCode}", 
                conversationId, response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting conversation details for {ConversationId}", conversationId);
            return null;
        }
    }

    public async Task<List<MessageDto>> GetConversationMessagesAsync(Guid conversationId)
    {
        try
        {
            SetTenantHeader();
            
            var response = await _httpClient.GetAsync($"{_chatRuntimeServiceUrl}/api/conversations/internal/{conversationId}/messages");
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<List<MessageDto>>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                return result ?? new List<MessageDto>();
            }
            
            _logger.LogWarning("Failed to get messages for conversation {ConversationId}. Status: {StatusCode}", 
                conversationId, response.StatusCode);
            return new List<MessageDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting messages for conversation {ConversationId}", conversationId);
            return new List<MessageDto>();
        }
    }

    public async Task<bool> MarkMessageAsReadAsync(Guid messageId, string readerId, string readerType)
    {
        try
        {
            SetTenantHeader();
            
            var request = new
            {
                ReaderId = readerId,
                ReaderType = readerType
            };
            
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PutAsync($"{_chatRuntimeServiceUrl}/api/chat/messages/{messageId}/mark-read", content);
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Successfully marked message {MessageId} as read by {ReaderType} {ReaderId}", 
                    messageId, readerType, readerId);
                return true;
            }
            
            _logger.LogWarning("Failed to mark message {MessageId} as read. Status: {StatusCode}", 
                messageId, response.StatusCode);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking message {MessageId} as read", messageId);
            return false;
        }
    }

    public async Task<ServiceResult<object>> SendFeedbackMessageAsync(Guid conversationId, SendFeedbackMessageRequest request)
    {
        try
        {
            SetTenantHeader();
            
            var messageData = new
            {
                conversationId,
                content = request.Content,
                type = request.Type,
                metadata = request.Metadata ?? new Dictionary<string, object>
                {
                    { "systemMessage", true },
                    { "feedbackRequest", true },
                    { "ratingScale", new { min = 1, max = 5, labels = new[] { "Poor", "Fair", "Good", "Very Good", "Excellent" } } },
                    { "feedbackPrompt", "How would you rate our service today?" }
                }
            };

            var json = JsonSerializer.Serialize(messageData);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_chatRuntimeServiceUrl}/api/conversations/{conversationId}/messages", content);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<object>(responseContent) ?? new { message = "Feedback message sent successfully" };
                _logger.LogInformation("Successfully sent feedback message to conversation {ConversationId}", conversationId);
                return ServiceResult<object>.Success(result);
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Failed to send feedback message to conversation {ConversationId}. Status: {StatusCode}, Error: {Error}", 
                    conversationId, response.StatusCode, errorContent);
                return ServiceResult<object>.Failure($"Failed to send feedback message: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending feedback message to conversation {ConversationId}", conversationId);
            return ServiceResult<object>.Failure($"Error sending feedback message: {ex.Message}");
        }
    }
}
