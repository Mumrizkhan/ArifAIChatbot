using AIOrchestrationService.Models;
using AIOrchestrationService.Services;
using Shared.Infrastructure.Services;
using Shared.Infrastructure.Messaging;
using Shared.Infrastructure.Messaging.Events;
using System.Security.Cryptography;
using System.Text;

namespace AIOrchestrationService.Services;

public class CachedOpenAIService : IAIService
{
    private readonly OpenAIService _openAIService;
    private readonly ICacheService _cacheService;
    private readonly IMessageBus _messageBus;
    private readonly ILogger<CachedOpenAIService> _logger;

    public CachedOpenAIService(
        OpenAIService openAIService,
        ICacheService cacheService,
        IMessageBus messageBus,
        ILogger<CachedOpenAIService> logger)
    {
        _openAIService = openAIService;
        _cacheService = cacheService;
        _messageBus = messageBus;
        _logger = logger;
    }

    public async Task<AIResponse> GenerateResponseAsync(AIRequest request)
    {
        var cacheKey = GenerateCacheKey("ai_response", request.Message, request.Language, request.Model);
        
        var cachedResponse = await _cacheService.GetAsync<AIResponse>(cacheKey);
        if (cachedResponse != null)
        {
            _logger.LogDebug("Cache hit for AI response: {CacheKey}", cacheKey);
            return cachedResponse;
        }

        var response = await _openAIService.GenerateResponseAsync(request);
        
        if (response.IsSuccessful)
        {
            await _cacheService.SetAsync(cacheKey, response, TimeSpan.FromMinutes(30));
            
            await _messageBus.PublishAsync(new AIResponseGeneratedEvent(
                request.ConversationId,
                Guid.NewGuid(),
                request.TenantId,
                response.Content,
                response.ProcessingTime,
                false,
                DateTime.UtcNow));
        }

        return response;
    }

    public async Task<AIResponse> GenerateResponseWithContextAsync(AIRequest request, List<string> contextDocuments)
    {
        var contextHash = GenerateContextHash(contextDocuments);
        var cacheKey = GenerateCacheKey("ai_response_context", request.Message, request.Language, request.Model, contextHash);
        
        var cachedResponse = await _cacheService.GetAsync<AIResponse>(cacheKey);
        if (cachedResponse != null)
        {
            _logger.LogDebug("Cache hit for AI response with context: {CacheKey}", cacheKey);
            return cachedResponse;
        }

        var response = await _openAIService.GenerateResponseWithContextAsync(request, contextDocuments);
        
        if (response.IsSuccessful)
        {
            await _cacheService.SetAsync(cacheKey, response, TimeSpan.FromMinutes(15));
            
            await _messageBus.PublishAsync(new AIResponseGeneratedEvent(
                request.ConversationId,
                Guid.NewGuid(),
                request.TenantId,
                response.Content,
                response.ProcessingTime,
                false,
                DateTime.UtcNow));
        }

        return response;
    }

    public Task<string> GenerateEmbeddingAsync(string text) => _openAIService.GenerateEmbeddingAsync(text);
    public Task<List<string>> ExtractIntentsAsync(string message) => _openAIService.ExtractIntentsAsync(message);
    public Task<string> TranslateTextAsync(string text, string targetLanguage) => _openAIService.TranslateTextAsync(text, targetLanguage);
    public Task<string> SummarizeConversationAsync(List<string> messages) => _openAIService.SummarizeConversationAsync(messages);

    private string GenerateCacheKey(params string[] parts)
    {
        var combined = string.Join("|", parts);
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(combined));
        return Convert.ToBase64String(hash)[..16];
    }

    private string GenerateContextHash(List<string> contextDocuments)
    {
        var combined = string.Join("", contextDocuments.OrderBy(d => d));
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(combined));
        return Convert.ToBase64String(hash)[..8];
    }
}
