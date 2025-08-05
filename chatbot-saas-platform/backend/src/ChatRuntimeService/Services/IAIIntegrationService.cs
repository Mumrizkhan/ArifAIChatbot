using ChatRuntimeService.Models;

namespace ChatRuntimeService.Services;

public interface IAIIntegrationService
{
    Task<string> GetBotResponseAsync(string message, string conversationId,string tenantId, string language = "en");
    Task<bool> ShouldTransferToAgentAsync(string message, string conversationId, string tenantId);
    Task<string> AnalyzeSentimentAsync(string message, string tenantId);
    Task<string[]> ExtractIntentsAsync(string message, string tenantId);
    Task<bool> IsSpamAsync(string message, string tenantId);
}
