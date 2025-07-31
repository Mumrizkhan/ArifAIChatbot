using Microsoft.EntityFrameworkCore;
using Shared.Application.Common.Interfaces;
using Shared.Domain.Entities;
using AIOrchestrationService.Models;

namespace AIOrchestrationService.Services;

public class AIOrchestrationService : IAIOrchestrationService
{
    private readonly IApplicationDbContext _context;
    private readonly IAIService _aiService;
    private readonly IVectorService _vectorService;
    private readonly ILogger<AIOrchestrationService> _logger;

    public AIOrchestrationService(
        IApplicationDbContext context,
        IAIService aiService,
        IVectorService vectorService,
        ILogger<AIOrchestrationService> logger)
    {
        _context = context;
        _aiService = aiService;
        _vectorService = vectorService;
        _logger = logger;
    }

    public async Task<AIResponse> ProcessChatAsync(ChatRequest request, Guid tenantId)
    {
        var conversation = await GetOrCreateConversationAsync(request.ConversationId, tenantId);
        
        var aiRequest = new AIRequest
        {
            Message = request.Message,
            ConversationId = conversation.Id.ToString(),
            Language = request.Language,
            Context = request.Context
        };

        var contextDocuments = await SearchKnowledgeBaseAsync(new SearchRequest 
        { 
            Query = request.Message,
            Limit = 3
        }, tenantId);

        var aiResponse = await _aiService.GenerateResponseWithContextAsync(
            aiRequest, 
            contextDocuments.Select(d => d.Content).ToList());

        var message = new Message
        {
            ConversationId = conversation.Id,
            Content = request.Message,
            SenderType = "User",
            CreatedAt = DateTime.UtcNow
        };

        var botMessage = new Message
        {
            ConversationId = conversation.Id,
            Content = aiResponse.Response,
            SenderType = "Bot",
            CreatedAt = DateTime.UtcNow
        };

        _context.Messages.Add(message);
        _context.Messages.Add(botMessage);
        await _context.SaveChangesAsync();

        return new AIResponse
        {
            Response = aiResponse.Response,
            ConversationId = conversation.Id.ToString(),
            Confidence = aiResponse.Confidence,
            Intents = aiResponse.Intents,
            Metadata = aiResponse.Metadata
        };
    }

    public async Task<SentimentAnalysisResponse> AnalyzeSentimentAsync(SentimentAnalysisRequest request, Guid tenantId)
    {
        return new SentimentAnalysisResponse
        {
            Sentiment = "neutral",
            Score = 0.5,
            Confidence = 0.8
        };
    }

    public async Task<IntentRecognitionResponse> RecognizeIntentAsync(IntentRecognitionRequest request, Guid tenantId)
    {
        return new IntentRecognitionResponse
        {
            Intent = "general_inquiry",
            Confidence = 0.7,
            Entities = new Dictionary<string, object>()
        };
    }

    public async Task<List<SearchResult>> SearchKnowledgeBaseAsync(SearchRequest request, Guid tenantId)
    {
        var collectionName = request.CollectionName ?? $"tenant_{tenantId}";
        var results = await _vectorService.SearchSimilarAsync(request.Query, collectionName, request.Limit);
        
        return results.Select(r => new SearchResult
        {
            Id = r.Id,
            Content = r.Content,
            Score = r.Score,
            Metadata = r.Metadata
        }).ToList();
    }

    private async Task<Conversation> GetOrCreateConversationAsync(string? conversationId, Guid tenantId)
    {
        if (!string.IsNullOrEmpty(conversationId) && Guid.TryParse(conversationId, out var id))
        {
            var existing = await _context.Conversations
                .FirstOrDefaultAsync(c => c.Id == id && c.TenantId == tenantId);
            
            if (existing != null) return existing;
        }

        var conversation = new Conversation
        {
            TenantId = tenantId,
            Status = "Active",
            CreatedAt = DateTime.UtcNow
        };

        _context.Conversations.Add(conversation);
        await _context.SaveChangesAsync();
        return conversation;
    }
}
