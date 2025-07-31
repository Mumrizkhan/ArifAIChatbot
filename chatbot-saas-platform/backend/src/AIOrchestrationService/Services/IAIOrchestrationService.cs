using AIOrchestrationService.Models;

namespace AIOrchestrationService.Services;

public interface IAIOrchestrationService
{
    Task<AIResponse> ProcessChatAsync(ChatRequest request, Guid tenantId);
    Task<SentimentAnalysisResponse> AnalyzeSentimentAsync(SentimentAnalysisRequest request, Guid tenantId);
    Task<IntentRecognitionResponse> RecognizeIntentAsync(IntentRecognitionRequest request, Guid tenantId);
    Task<List<SearchResult>> SearchKnowledgeBaseAsync(SearchRequest request, Guid tenantId);
}
