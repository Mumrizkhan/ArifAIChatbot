using AIOrchestrationService.Models;

namespace AIOrchestrationService.Services;

public interface IAIService
{
    Task<AIResponse> GenerateResponseAsync(AIRequest request);
    Task<AIResponse> GenerateResponseWithContextAsync(AIRequest request, List<string> contextDocuments);
    Task<string> GenerateEmbeddingAsync(string text);
    Task<List<string>> ExtractIntentsAsync(string message);
    Task<string> TranslateTextAsync(string text, string targetLanguage);
    Task<string> SummarizeConversationAsync(List<string> messages);
    Task<string> AnalyzeSentimentAsync(string message);
}
