using System.Text.Json;

namespace LiveAgentService.Services;

public interface IChatRuntimeIntegrationService
{
    Task<bool> SendMessageAsync(Guid conversationId, string content, string type = "text", Guid? senderId = null);
}

public class ChatRuntimeIntegrationService : IChatRuntimeIntegrationService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ChatRuntimeIntegrationService> _logger;
    private readonly string _chatRuntimeServiceUrl;

    public ChatRuntimeIntegrationService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<ChatRuntimeIntegrationService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
        _chatRuntimeServiceUrl = _configuration["Services:ChatRuntime"] ?? "http://localhost:5002";
    }

    public async Task<bool> SendMessageAsync(Guid conversationId, string content, string type = "text", Guid? senderId = null)
    {
        try
        {
            var request = new
            {
                ConversationId = conversationId.ToString(),
                Content = content,
                Type = type,
                SenderId = senderId
            };
            
            var response = await _httpClient.PostAsJsonAsync($"{_chatRuntimeServiceUrl}/api/chat/messages", request);
            
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
}
