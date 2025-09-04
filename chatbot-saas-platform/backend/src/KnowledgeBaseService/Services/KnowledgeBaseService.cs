using Microsoft.EntityFrameworkCore;
using Shared.Application.Common.Interfaces;
using KnowledgeBaseService.Services;
using KnowledgeBaseService.Models;
using Shared.Domain.Entities;
using Shared.Infrastructure.Persistence;
using Shared.Application.Services;
namespace KnowledgeBaseService.Services;

public class KnowledgeBaseService : IKnowledgeBaseService
{
    private readonly IApplicationDbContext _context;
    private readonly IDocumentProcessingService _documentProcessingService;
    private readonly IVectorService _vectorSearchService;
    private readonly ILogger<KnowledgeBaseService> _logger;
    private readonly IConfiguration _configuration;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public KnowledgeBaseService(
        IApplicationDbContext context,
        IDocumentProcessingService documentProcessingService,
        IVectorService vectorSearchService,
        ILogger<KnowledgeBaseService> logger,
        IConfiguration configuration,
        IServiceScopeFactory serviceScopeFactory)
    {
        _context = context;
        _documentProcessingService = documentProcessingService;
        _vectorSearchService = vectorSearchService;
        _logger = logger;
        _configuration = configuration;
        _serviceScopeFactory = serviceScopeFactory;
    }

    public async Task<Document> UploadDocumentAsync(DocumentUploadRequest request, Guid tenantId, Guid userId)
    {
        try
        {
            _logger.LogInformation("Starting document upload for tenant {TenantId}, file: {FileName}", tenantId, request.File.FileName);

            var document = new Document
            {
                Id = Guid.NewGuid(),
                Title = request.Title,
                OriginalFileName = request.File.FileName,
                FileType = Path.GetExtension(request.File.FileName).ToLower(),
                FileSize = request.File.Length,
                Status = DocumentStatus.Uploaded,
                TenantId = tenantId,
                UploadedByUserId = userId,
                Tags = request.Tags,
                Language = request.Language,
                Metadata = request.Metadata,
                CreatedAt = DateTime.UtcNow
            };

            var filePath = await SaveFileAsync(request.File, document.Id, tenantId);
            document.FilePath = filePath;

            _context.Set<Document>().Add(document);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Document {DocumentId} uploaded successfully for tenant {TenantId}. Starting background processing.", document.Id, tenantId);
            var documentId=document.Id;
            _ = Task.Run(async () => await ProcessDocumentInBackgroundAsync(documentId));

            return document;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading document for tenant {TenantId}", tenantId);
            throw;
        }
    }

    public async Task<List<Document>> GetDocumentsAsync(Guid tenantId, int page = 1, int pageSize = 20)
    {
        try
        {
            return await _context.Set<Document>()
                .Where(d => d.TenantId == tenantId)
                .OrderByDescending(d => d.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting documents for tenant {TenantId}", tenantId);
            throw;
        }
    }

    public async Task<Document?> GetDocumentAsync(Guid documentId, Guid tenantId)
    {
        try
        {
            return await _context.Set<Document>()
                .FirstOrDefaultAsync(d => d.Id == documentId && d.TenantId == tenantId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting document {DocumentId} for tenant {TenantId}", documentId, tenantId);
            throw;
        }
    }

    public async Task<bool> DeleteDocumentAsync(Guid documentId, Guid tenantId)
    {
        try
        {
            var document = await _context.Set<Document>()
                .FirstOrDefaultAsync(d => d.Id == documentId && d.TenantId == tenantId);

            if (document == null)
                return false;

            if (!string.IsNullOrEmpty(document.VectorCollectionName))
            {
                await _vectorSearchService.DeleteDocumentAsync(document.VectorCollectionName, documentId);
            }

            var chunks = await _context.Set<DocumentChunk>()
                .Where(c => c.DocumentId == documentId)
                .ToListAsync();

            _context.Set<DocumentChunk>().RemoveRange(chunks);

            _context.Set<Document>().Remove(document);

            if (File.Exists(document.FilePath))
            {
                File.Delete(document.FilePath);
            }

            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting document {DocumentId} for tenant {TenantId}", documentId, tenantId);
            return false;
        }
    }

    public async Task<List<DocumentSearchResult>> SearchDocumentsAsync(DocumentSearchRequest request, Guid tenantId)
    {
        try
        {
            var result = await _vectorSearchService.SearchAcrossAllCollectionsAsync(tenantId, request.Query, request.Limit);
            if (result == null || !result.Any())
            {
                _logger.LogInformation("No documents found for tenant {TenantId} with query: {Query}", tenantId, request.Query);
                return new List<DocumentSearchResult>();
            }

            var documentSearchResults = result.Select(r => new DocumentSearchResult
            {
                DocumentId = Guid.Parse(r.Id),
                Title = r.Metadata.ContainsKey("Title") ? r.Metadata["Title"].ToString() : "Untitled",
                Content = r.Content,
                Score = r.Score,
                Tags = r.Metadata.ContainsKey("Tags") ? r.Metadata["Tags"] as List<string> : new List<string>(),
                Summary = r.Metadata.ContainsKey("Summary") ? r.Metadata["Summary"].ToString() : string.Empty,
                CreatedAt = r.Metadata.ContainsKey("CreatedAt") ? DateTime.Parse(r.Metadata["CreatedAt"].ToString()) : DateTime.UtcNow,
                Metadata = r.Metadata.ToDictionary(kv => kv.Key, kv => kv.Value)                

            }).ToList();

            return documentSearchResults;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching documents for tenant {TenantId}", tenantId);
            throw;
        }
    }

    public async Task<RAGResponse> GenerateRAGResponseAsync(RAGRequest request, Guid tenantId)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            var searchRequest = new DocumentSearchRequest
            {
                Query = request.Query,
                Language = request.Language,
                Tags = request.Tags,
                Limit = request.MaxDocuments,
                MinScore = request.MinRelevanceScore,
                Filters = request.Filters
            };

            var relevantDocuments = await SearchDocumentsAsync(searchRequest, tenantId);

            if (!relevantDocuments.Any())
            {
                stopwatch.Stop();
                return new RAGResponse
                {
                    GeneratedResponse = "I couldn't find any relevant information in the knowledge base to answer your question.",
                    SourceDocuments = new List<DocumentSearchResult>(),
                    ConfidenceScore = 0.0,
                    ProcessingTime = stopwatch.Elapsed,
                    IsSuccessful = true
                };
            }

            var contextText = string.Join("\n\n", relevantDocuments.Select(d => d.Content));
            var generatedResponse = await GenerateResponseWithContextAsync(request.Query, contextText);

            stopwatch.Stop();

            return new RAGResponse
            {
                GeneratedResponse = generatedResponse,
                SourceDocuments = relevantDocuments,
                ConfidenceScore = relevantDocuments.Average(d => d.Score),
                ProcessingTime = stopwatch.Elapsed,
                IsSuccessful = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating RAG response for tenant {TenantId}", tenantId);
            stopwatch.Stop();

            return new RAGResponse
            {
                GeneratedResponse = "I apologize, but I encountered an error while processing your request.",
                ProcessingTime = stopwatch.Elapsed,
                IsSuccessful = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<KnowledgeBaseStatistics> GetStatisticsAsync(Guid tenantId)
    {
        try
        {
            var documents = await _context.Set<Document>()
                .Where(d => d.TenantId == tenantId)
                .ToListAsync();

            var chunks = await _context.Set<DocumentChunk>()
                .Where(c => c.TenantId == tenantId)
                .CountAsync();

            _logger.LogInformation("Statistics for tenant {TenantId}: {DocumentCount} documents, {ChunkCount} chunks",
                tenantId, documents.Count, chunks);

            var statistics = new KnowledgeBaseStatistics
            {
                TotalDocuments = documents.Count,
                ProcessedDocuments = documents.Count(d => d.Status == DocumentStatus.Processed),
                FailedDocuments = documents.Count(d => d.Status == DocumentStatus.Failed),
                TotalFileSize = documents.Sum(d => d.FileSize),
                TotalChunks = chunks,
                DocumentsByType = documents.GroupBy(d => d.FileType)
                    .ToDictionary(g => g.Key, g => g.Count()),
                DocumentsByLanguage = documents.GroupBy(d => d.Language)
                    .ToDictionary(g => g.Key, g => g.Count()),
                LastUpdated = DateTime.UtcNow
            };

            _logger.LogInformation("Generated statistics for tenant {TenantId}: {Stats}", tenantId,
                System.Text.Json.JsonSerializer.Serialize(statistics));

            return statistics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting statistics for tenant {TenantId}", tenantId);
            throw;
        }
    }

    public async Task<bool> ReprocessDocumentAsync(Guid documentId, Guid tenantId)
    {
        try
        {
            var document = await GetDocumentAsync(documentId, tenantId);
            if (document == null || !File.Exists(document.FilePath))
                return false;

            document.Status = DocumentStatus.Processing;
            await _context.SaveChangesAsync();

            _ = Task.Run(async () => await ProcessDocumentInBackgroundAsync(documentId));

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reprocessing document {DocumentId} for tenant {TenantId}", documentId, tenantId);
            return false;
        }
    }

    public async Task<Stream> DownloadDocumentAsync(Guid documentId, Guid tenantId)
    {
        try
        {
            var document = await GetDocumentAsync(documentId, tenantId);
            if (document == null || !File.Exists(document.FilePath))
                throw new FileNotFoundException("Document not found");

            return new FileStream(document.FilePath, FileMode.Open, FileAccess.Read);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading document {DocumentId} for tenant {TenantId}", documentId, tenantId);
            throw;
        }
    }

    private async Task<string> SaveFileAsync(IFormFile file, Guid documentId, Guid tenantId)
    {
        var uploadsPath = Path.Combine("uploads", tenantId.ToString());
        Directory.CreateDirectory(uploadsPath);

        var fileName = $"{documentId}{Path.GetExtension(file.FileName)}";
        var filePath = Path.Combine(uploadsPath, fileName);

        using var stream = new FileStream(filePath, FileMode.Create);
        await file.CopyToAsync(stream);

        return filePath;
    }

    private async Task ProcessDocumentInBackgroundAsync(Guid documentId)
    {
        var document = await _context.Set<Document>().FindAsync(documentId);
        if (document == null)
        {
            _logger.LogWarning("Document {DocumentId} not found for background processing", documentId);
            return;
        }

        _logger.LogInformation("Starting background processing for document {DocumentId} in tenant {TenantId}", document.Id, document.TenantId);
        try
        {
           
            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var documentProcessingService = scope.ServiceProvider.GetRequiredService<IDocumentProcessingService>();

                document.Status = DocumentStatus.Processing;
                await dbContext.SaveChangesAsync();

                _logger.LogInformation("Document {DocumentId} status updated to Processing", document.Id);

                using var fileStream = new FileStream(document.FilePath, FileMode.Open, FileAccess.Read);
                var result = await documentProcessingService.ProcessDocumentAsync(document, fileStream);

                if (result.IsSuccessful)
                {
                    var chunks = await documentProcessingService.ChunkDocumentAsync(document, document.Content);
                    dbContext.Set<DocumentChunk>().AddRange(chunks);

                    var collectionName = $"tenant_{document.TenantId:N}_collection_{Guid.NewGuid()}";
                    var vectorSearchService = scope.ServiceProvider.GetRequiredService<IVectorService>();

                    _logger.LogInformation("Creating/accessing collection {CollectionName} for document {DocumentId}", collectionName, document.Id);
                    await vectorSearchService.CreateCollectionAsync(collectionName);

                    _logger.LogInformation("Storing document {DocumentId} with {ChunkCount} chunks in vector database", document.Id, chunks.Count);
                    await vectorSearchService.UpsertDocumentAsync(document, chunks);

                    _logger.LogInformation("Document {DocumentId} successfully processed and stored in vector database", document.Id);
                }
                else
                {
                    _logger.LogError("Document processing failed for {DocumentId}: {ErrorMessage}", document.Id, result.ErrorMessage);
                }

                await dbContext.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing document {DocumentId} in background", documentId);
            document.Status = DocumentStatus.Failed;
            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                await dbContext.SaveChangesAsync();
            }
        }
    }

    private async Task<string> GenerateResponseWithContextAsync(string query, string context)
    {
        try
        {
            return $"Based on the available information: {context.Substring(0, Math.Min(context.Length, 500))}...";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating response with context");
            return "I apologize, but I couldn't generate a response based on the available information.";
        }
    }
}
