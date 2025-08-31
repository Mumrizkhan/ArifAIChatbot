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
        _aiServiceUrl = _configuration["Services:AIOrchestration"] ?? "http://localhost:5001";
    }

    public async Task<string> GetBotResponseAsync(string message, string conversationId,string tenantId, string language = "en")
    {
        try
        {
            var request = new
            {
                Message = message,
                ConversationId = conversationId,
                Language = language
            };
            SetTenantId(tenantId);
            var response = await _httpClient.PostAsJsonAsync($"{_aiServiceUrl}/api/ai/chat", request);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadAsStringAsync();
                var aiResponse = JsonSerializer.Deserialize<AIResponse>(result);
                return aiResponse?.Content ?? "I'm sorry, I couldn't process your request.";
            }

            _logger.LogWarning($"AI service returned {response.StatusCode} for conversation {conversationId}");
            
            // Trigger escalation when AI service is unavailable or returns error
            await TriggerEscalationAsync(conversationId, tenantId);
            
            return "I'm sorry, I'm having trouble understanding. Let me connect you with a human agent.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting AI response for conversation {conversationId}");
            
            // Trigger escalation to human agent when experiencing technical difficulties
            await TriggerEscalationAsync(conversationId, tenantId);
            
            return "I'm experiencing technical difficulties. Please wait while I connect you with a human agent.";
        }
    }

    private void SetTenantId(string tenantId)
    {
        _httpClient.DefaultRequestHeaders.Remove("X-Tenant-ID");
        _httpClient.DefaultRequestHeaders.Add("X-Tenant-ID", tenantId);
    }

    private async Task TriggerEscalationAsync(string conversationId, string tenantId)
    {
        try
        {
            _logger.LogInformation($"Triggering escalation for conversation {conversationId} due to AI technical difficulties");
            
            // Set tenant header for the escalation request
            SetTenantId(tenantId);
            
            // Call the escalation endpoint on the same service (ChatRuntimeService)
           
            var response = await _httpClient.PostAsync($"{_aiServiceUrl}/chat/chat/conversations/{conversationId}/escalate", null);
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation($"Successfully triggered escalation for conversation {conversationId}");
            }
            else
            {
                _logger.LogWarning($"Failed to trigger escalation for conversation {conversationId}. Status: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error triggering escalation for conversation {conversationId}");
            // Don't rethrow - escalation failure shouldn't prevent the error message from being returned
        }
    }

    public async Task<bool> ShouldTransferToAgentAsync(string message, string conversationId,string tenantId)
    {
        try
        {
            var request = new
            {
                Message = message,
                ConversationId = conversationId
            };
            SetTenantId(tenantId);
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

    public async Task<string> AnalyzeSentimentAsync(string message,string tenantId)
    {
        try
        {
            var request = new { Message = message };
            SetTenantId(tenantId);
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

    public async Task<string[]> ExtractIntentsAsync(string message,string tenantId)
    {
        try
        {
            var request = new { Message = message };
            SetTenantId(tenantId);
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

    public async Task<bool> IsSpamAsync(string message,string tenantid)
    {
        try
        {
            var request = new { Message = message };
            SetTenantId(tenantid);
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
