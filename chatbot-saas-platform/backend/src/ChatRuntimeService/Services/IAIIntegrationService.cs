using ChatRuntimeService.Models;

namespace ChatRuntimeService.Services;

public interface IAIIntegrationService
{
    Task<string> GetBotResponseAsync(string message, string conversationId, string language = "en");
    Task<bool> ShouldTransferToAgentAsync(string message, string conversationId);
    Task<string> AnalyzeSentimentAsync(string message);
    Task<string[]> ExtractIntentsAsync(string message);
    Task<bool> IsSpamAsync(string message);
}
