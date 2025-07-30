using ChatRuntimeService.Models;
using System.Text.Json;

namespace ChatRuntimeService.Services;

public class AIIntegrationService : IAIIntegrationService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AIIntegrationService> _logger;
    private readonly string _aiServiceUrl;

    public AIIntegrationService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<AIIntegrationService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
        _aiServiceUrl = _configuration["Services:AIOrchestration"] ?? "http://localhost:5002";
    }

    public async Task<string> GetBotResponseAsync(string message, string conversationId, string language = "en")
    {
        try
        {
            var request = new
            {
                Message = message,
                ConversationId = conversationId,
                Language = language
            };

            var response = await _httpClient.PostAsJsonAsync($"{_aiServiceUrl}/api/ai/chat", request);
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadAsStringAsync();
                var aiResponse = JsonSerializer.Deserialize<AIResponse>(result);
                return aiResponse?.Response ?? "I'm sorry, I couldn't process your request.";
            }
            
            _logger.LogWarning($"AI service returned {response.StatusCode} for conversation {conversationId}");
            return "I'm sorry, I'm having trouble understanding. Let me connect you with a human agent.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting AI response for conversation {conversationId}");
            return "I'm experiencing technical difficulties. Please wait while I connect you with a human agent.";
        }
    }

    public async Task<bool> ShouldTransferToAgentAsync(string message, string conversationId)
    {
        try
        {
            var request = new
            {
                Message = message,
                ConversationId = conversationId
            };

            var response = await _httpClient.PostAsJsonAsync($"{_aiServiceUrl}/api/ai/should-transfer", request);
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadAsStringAsync();
                var transferResponse = JsonSerializer.Deserialize<TransferResponse>(result);
                return transferResponse?.ShouldTransfer ?? false;
            }
            
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error checking transfer requirement for conversation {conversationId}");
            return false;
        }
    }

    public async Task<string> AnalyzeSentimentAsync(string message)
    {
        try
        {
            var request = new { Message = message };
            var response = await _httpClient.PostAsJsonAsync($"{_aiServiceUrl}/api/ai/sentiment", request);
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadAsStringAsync();
                var sentimentResponse = JsonSerializer.Deserialize<SentimentResponse>(result);
                return sentimentResponse?.Sentiment ?? "neutral";
            }
            
            return "neutral";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error analyzing sentiment: {ex.Message}");
            return "neutral";
        }
    }

    public async Task<string[]> ExtractIntentsAsync(string message)
    {
        try
        {
            var request = new { Message = message };
            var response = await _httpClient.PostAsJsonAsync($"{_aiServiceUrl}/api/ai/intents", request);
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadAsStringAsync();
                var intentResponse = JsonSerializer.Deserialize<IntentResponse>(result);
                return intentResponse?.Intents ?? Array.Empty<string>();
            }
            
            return Array.Empty<string>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error extracting intents: {ex.Message}");
            return Array.Empty<string>();
        }
    }

    public async Task<bool> IsSpamAsync(string message)
    {
        try
        {
            var request = new { Message = message };
            var response = await _httpClient.PostAsJsonAsync($"{_aiServiceUrl}/api/ai/spam-detection", request);
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadAsStringAsync();
                var spamResponse = JsonSerializer.Deserialize<SpamResponse>(result);
                return spamResponse?.IsSpam ?? false;
            }
            
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error checking spam: {ex.Message}");
            return false;
        }
    }
}
