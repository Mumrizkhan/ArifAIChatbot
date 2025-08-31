using KnowledgeBaseService.Models;
using KnowledgeBaseService.Services;
using Shared.Application.Common.Interfaces;
using Shared.Domain.Entities;
using Shared.Infrastructure.Services;
using Shared.Infrastructure.Messaging;
using Shared.Infrastructure.Messaging.Events;

namespace KnowledgeBaseService.Services;

public class AsyncDocumentProcessingService : IDocumentProcessingService
{
    private readonly DocumentProcessingService _documentProcessingService;
    private readonly ICacheService _cacheService;
    private readonly IMessageBus _messageBus;
    private readonly ILogger<AsyncDocumentProcessingService> _logger;

    public AsyncDocumentProcessingService(
        DocumentProcessingService documentProcessingService,
        ICacheService cacheService,
        IMessageBus messageBus,
        ILogger<AsyncDocumentProcessingService> logger)
    {
        _documentProcessingService = documentProcessingService;
        _cacheService = cacheService;
        _messageBus = messageBus;
        _logger = logger;
    }

    public async Task<DocumentProcessingResult> ProcessDocumentAsync(Document document, Stream fileStream)
    {
        await _messageBus.PublishAsync(new DocumentProcessingStartedEvent(
            document.Id,
            document.TenantId,
            "full_processing",
            DateTime.UtcNow));

        var result = await _documentProcessingService.ProcessDocumentAsync(document, fileStream);

        if (result.IsSuccessful)
        {
            var cacheKey = $"document_summary_{document.Id}";
            await _cacheService.SetAsync(cacheKey, result.Summary, TimeSpan.FromHours(24));

            var tagsCacheKey = $"document_tags_{document.Id}";
            await _cacheService.SetAsync(tagsCacheKey, result.ExtractedTags, TimeSpan.FromHours(24));
        }

        await _messageBus.PublishAsync(new DocumentProcessedEvent(
            document.Id,
            document.TenantId,
            result.IsSuccessful,
            result.ErrorMessage,
            result.ProcessingTime,
            result.ChunksCreated,
            DateTime.UtcNow));

        return result;
    }

    public async Task<string> ExtractTextFromFileAsync(Stream fileStream, string fileType)
    {
        return await _documentProcessingService.ExtractTextFromFileAsync(fileStream, fileType);
    }

    public async Task<List<DocumentChunk>> ChunkDocumentAsync(Document document, string content)
    {
        var cacheKey = $"document_chunks_{document.Id}";
        return await _cacheService.GetAsync(cacheKey, async () =>
        {
            var chunks = await _documentProcessingService.ChunkDocumentAsync(document, content);
            return chunks;
        }, TimeSpan.FromHours(4));
    }

    public async Task<string> GenerateDocumentSummaryAsync(string content)
    {
        var contentHash = content.GetHashCode().ToString();
        var cacheKey = $"document_summary_hash_{contentHash}";
        
        return await _cacheService.GetAsync(cacheKey, async () =>
        {
            return await _documentProcessingService.GenerateDocumentSummaryAsync(content);
        }, TimeSpan.FromHours(12));
    }

    public async Task<List<string>> ExtractTagsAsync(string content)
    {
        var contentHash = content.GetHashCode().ToString();
        var cacheKey = $"document_tags_hash_{contentHash}";
        
        return await _cacheService.GetAsync(cacheKey, async () =>
        {
            return await _documentProcessingService.ExtractTagsAsync(content);
        }, TimeSpan.FromHours(12));
    }

    public Task<bool> GenerateEmbeddingsAsync(List<DocumentChunk> chunks) => 
        _documentProcessingService.GenerateEmbeddingsAsync(chunks);

    public Task<bool> StoreInVectorDatabaseAsync(List<DocumentChunk> chunks, string collectionName) => 
        _documentProcessingService.StoreInVectorDatabaseAsync(chunks, collectionName);
}
