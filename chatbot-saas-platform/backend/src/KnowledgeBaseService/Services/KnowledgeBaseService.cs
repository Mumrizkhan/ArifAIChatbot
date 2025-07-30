using Microsoft.EntityFrameworkCore;
using Shared.Application.Common.Interfaces;
using KnowledgeBaseService.Services;
using KnowledgeBaseService.Models;
using Shared.Domain.Entities;
namespace KnowledgeBaseService.Services;

public class KnowledgeBaseService : IKnowledgeBaseService
{
    private readonly IApplicationDbContext _context;
    private readonly IDocumentProcessingService _documentProcessingService;
    private readonly IVectorSearchService _vectorSearchService;
    private readonly ILogger<KnowledgeBaseService> _logger;
    private readonly IConfiguration _configuration;

    public KnowledgeBaseService(
        IApplicationDbContext context,
        IDocumentProcessingService documentProcessingService,
        IVectorSearchService vectorSearchService,
        ILogger<KnowledgeBaseService> logger,
        IConfiguration configuration)
    {
        _context = context;
        _documentProcessingService = documentProcessingService;
        _vectorSearchService = vectorSearchService;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<Document> UploadDocumentAsync(DocumentUploadRequest request, Guid tenantId, Guid userId)
    {
        try
        {
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

            _ = Task.Run(async () => await ProcessDocumentInBackgroundAsync(document));

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
                await _vectorSearchService.DeleteDocumentFromVectorAsync(documentId, document.VectorCollectionName);
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
            return await _vectorSearchService.SearchSimilarDocumentsAsync(request, tenantId);
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

            return new KnowledgeBaseStatistics
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

            _ = Task.Run(async () => await ProcessDocumentInBackgroundAsync(document));

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

    private async Task ProcessDocumentInBackgroundAsync(Document document)
    {
        try
        {
            document.Status = DocumentStatus.Processing;
            await _context.SaveChangesAsync();

            using var fileStream = new FileStream(document.FilePath, FileMode.Open, FileAccess.Read);
            var result = await _documentProcessingService.ProcessDocumentAsync(document, fileStream);

            if (result.IsSuccessful)
            {
                var chunks = await _documentProcessingService.ChunkDocumentAsync(document, document.Content);
                _context.Set<DocumentChunk>().AddRange(chunks);
                
                var collectionName = $"tenant_{document.TenantId:N}_knowledge";
                await _vectorSearchService.CreateCollectionAsync(collectionName, document.TenantId);
                
                await _vectorSearchService.UpdateDocumentInVectorAsync(document, chunks);
            }

            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing document {DocumentId} in background", document.Id);
            document.Status = DocumentStatus.Failed;
            await _context.SaveChangesAsync();
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
