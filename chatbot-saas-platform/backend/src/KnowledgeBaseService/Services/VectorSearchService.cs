//using Qdrant.Client;
//using Qdrant.Client.Grpc;
//using Microsoft.EntityFrameworkCore;
//using Shared.Application.Common.Interfaces;
//using KnowledgeBaseService.Services;
//using KnowledgeBaseService.Models;
//using Shared.Domain.Entities;
//using Document = Shared.Domain.Entities.Document;

//namespace KnowledgeBaseService.Services;

//public class VectorSearchService : IVectorSearchService
//{
//    private readonly QdrantClient _qdrantClient;
//    private readonly IApplicationDbContext _context;
//    private readonly ILogger<VectorSearchService> _logger;
//    private readonly IConfiguration _configuration;

//    public VectorSearchService(
//        IApplicationDbContext context,
//        ILogger<VectorSearchService> logger,
//        IConfiguration configuration)
//    {
//        _context = context;
//        _logger = logger;
//        _configuration = configuration;
        
//        var host = _configuration["Qdrant:Host"] ?? "localhost";
//        var port = int.Parse(_configuration["Qdrant:Port"] ?? "6334");
//        var apiKey = _configuration["Qdrant:ApiKey"] ?? "";
        
//        _qdrantClient = new QdrantClient(host, port, https: false, apiKey);
//    }

//    public async Task<List<DocumentSearchResult>> SearchSimilarDocumentsAsync(DocumentSearchRequest request, Guid tenantId)
//    {
//        try
//        {
//            var collectionName = $"tenant_{tenantId:N}_knowledge";
//            _logger.LogInformation("Searching documents in collection {CollectionName} for tenant {TenantId}, query: {Query}, limit: {Limit}", 
//                collectionName, tenantId, request.Query, request.Limit);
            
//            var queryEmbedding = await GenerateQueryEmbeddingAsync(request.Query);
//            if (queryEmbedding == null)
//            {
//                _logger.LogWarning("Failed to generate embedding for query: {Query}", request.Query);
//                return new List<DocumentSearchResult>();
//            }

//            var response = await _qdrantClient.SearchAsync(collectionName, queryEmbedding, limit: (ulong)request.Limit, scoreThreshold: (float)request.MinScore);
            
//            _logger.LogInformation("Qdrant search returned {ResultCount} results for tenant {TenantId}", response.Count(), tenantId);
            
//            var results = new List<DocumentSearchResult>();
//            var documentIds = new List<Guid>();

//            foreach (var point in response)
//            {
//                if (point.Payload.TryGetValue("document_id", out var docIdValue) &&
//                    Guid.TryParse(docIdValue.StringValue, out var documentId))
//                {
//                    documentIds.Add(documentId);
//                }
//            }

//            _logger.LogInformation("Found {UniqueDocumentCount} unique documents in search results for tenant {TenantId}: {DocumentIds}", 
//                documentIds.Distinct().Count(), tenantId, string.Join(", ", documentIds.Distinct()));

//            var documents = await _context.Set<Document>()
//                .Where(d => documentIds.Contains(d.Id) && d.TenantId == tenantId)
//                .ToListAsync();

//            foreach (var point in response)
//            {
//                if (point.Payload.TryGetValue("document_id", out var docIdValue) &&
//                    Guid.TryParse(docIdValue.StringValue, out var documentId))
//                {
//                    var document = documents.FirstOrDefault(d => d.Id == documentId);
//                    if (document != null)
//                    {
//                        if (request.Tags != null && request.Tags.Any() && 
//                            !request.Tags.Any(tag => document.Tags.Contains(tag, StringComparer.OrdinalIgnoreCase)))
//                        {
//                            continue;
//                        }

//                        var content = point.Payload.TryGetValue("content", out var contentValue) 
//                            ? contentValue.StringValue 
//                            : document.Content;

//                        results.Add(new DocumentSearchResult
//                        {
//                            DocumentId = documentId,
//                            Title = document.Title,
//                            Content = TruncateContent(content, 500),
//                            Score = (double)point.Score,
//                            Tags = document.Tags,
//                            Metadata = document.Metadata,
//                            Summary = document.Summary,
//                            CreatedAt = document.CreatedAt
//                        });
//                    }
//                }
//            }

//            _logger.LogInformation("Returning {FinalResultCount} search results from {UniqueDocuments} documents for tenant {TenantId}", 
//                results.Count, results.Select(r => r.DocumentId).Distinct().Count(), tenantId);

//            return results.OrderByDescending(r => r.Score).ToList();
//        }
//        catch (Exception ex)
//        {
//            _logger.LogError(ex, "Error searching similar documents for tenant {TenantId}", tenantId);
//            return new List<DocumentSearchResult>();
//        }
//    }

//    public async Task<bool> CreateCollectionAsync(string collectionName, Guid tenantId)
//    {
//        try
//        {
//            var createRequest = new CreateCollection
//            {
//                CollectionName = collectionName,
//                VectorsConfig = new VectorsConfig
//                {
//                    Params = new VectorParams
//                    {
//                        Size = 1536, // OpenAI embedding size
//                        Distance = Distance.Cosine
//                    }
//                }
//            };

//            await _qdrantClient.CreateCollectionAsync(collectionName, new VectorParams
//            {
//                Size = 1536,
//                Distance = Distance.Cosine
//            });
//            var response = true;
            
//            if (response)
//            {
//                _logger.LogInformation("Created collection {CollectionName} for tenant {TenantId}", collectionName, tenantId);
//            }

//            return response;
//        }
//        catch (Exception ex)
//        {
//            _logger.LogError(ex, "Error creating collection {CollectionName} for tenant {TenantId}", collectionName, tenantId);
//            return false;
//        }
//    }

//    public async Task<bool> DeleteCollectionAsync(string collectionName)
//    {
//        try
//        {
//            await _qdrantClient.DeleteCollectionAsync(collectionName);
            
//            _logger.LogInformation("Deleted collection {CollectionName}", collectionName);
//            return true;
//        }
//        catch (Exception ex)
//        {
//            _logger.LogError(ex, "Error deleting collection {CollectionName}", collectionName);
//            return false;
//        }
//    }

//    public async Task<bool> DeleteDocumentFromVectorAsync(Guid documentId, string collectionName)
//    {
//        try
//        {
//            var chunks = await _context.Set<DocumentChunk>()
//                .Where(c => c.DocumentId == documentId)
//                .ToListAsync();

//            var chunkIds = chunks.Select(c => c.Id.ToString()).ToList();

//            if (chunkIds.Any())
//            {
//                try
//                {
//                    var guids = chunkIds.Where(id => Guid.TryParse(id, out _))
//                                      .Select(id => Guid.Parse(id))
//                                      .ToList();
//                    if (guids.Any())
//                    {
//                        await _qdrantClient.DeleteAsync(collectionName, guids);
//                    }
//                    return true;
//                }
//                catch (Exception deleteEx)
//                {
//                    _logger.LogWarning(deleteEx, "Failed to delete points, attempting individual deletion");
//                    foreach (var chunkId in chunkIds)
//                    {
//                        try
//                        {
//                            if (Guid.TryParse(chunkId, out var guid))
//                            {
//                                await _qdrantClient.DeleteAsync(collectionName, guid);
//                            }
//                        }
//                        catch (Exception ex)
//                        {
//                            _logger.LogWarning(ex, "Failed to delete point {ChunkId}", chunkId);
//                        }
//                    }
//                    return true;
//                }
//            }

//            return true;
//        }
//        catch (Exception ex)
//        {
//            _logger.LogError(ex, "Error deleting document {DocumentId} from vector database", documentId);
//            return false;
//        }
//    }

//    public async Task<List<string>> GetCollectionsAsync(Guid tenantId)
//    {
//        try
//        {
//            var response = await _qdrantClient.ListCollectionsAsync();
//            var tenantPrefix = $"tenant_{tenantId:N}_";
            
//            return response
//                .Where(c => c.StartsWith(tenantPrefix))
//                .ToList();
//        }
//        catch (Exception ex)
//        {
//            _logger.LogError(ex, "Error getting collections for tenant {TenantId}", tenantId);
//            return new List<string>();
//        }
//    }

//    public async Task<bool> UpdateDocumentInVectorAsync(Document document, List<DocumentChunk> chunks)
//    {
//        try
//        {
//            var collectionName = document.VectorCollectionName ?? $"tenant_{document.TenantId:N}_knowledge";
            
//            await DeleteDocumentFromVectorAsync(document.Id, collectionName);

//            var points = new List<PointStruct>();

//            foreach (var chunk in chunks.Where(c => !string.IsNullOrEmpty(c.Embedding)))
//            {
//                var embedding = ParseEmbedding(chunk.Embedding!);
//                if (embedding != null)
//                {
//                    var payload = new Dictionary<string, Value>
//                    {
//                        ["content"] = chunk.Content,
//                        ["document_id"] = document.Id.ToString(),
//                        ["document_title"] = document.Title,
//                        ["chunk_index"] = chunk.ChunkIndex.ToString(),
//                        ["language"] = document.Language
//                    };

//                    foreach (var meta in chunk.Metadata)
//                    {
//                        payload[meta.Key] = meta.Value.ToString() ?? "";
//                    }

//                    var point = new PointStruct
//                    {
//                        Id = new PointId { Uuid = chunk.Id.ToString() },
//                        Vectors = embedding
//                    };
                    
//                    foreach (var kvp in payload)
//                    {
//                        point.Payload[kvp.Key] = kvp.Value;
//                    }
                    
//                    points.Add(point);
//                }
//            }

//            if (points.Any())
//            {
//                var upsertRequest = new UpsertPoints
//                {
//                    CollectionName = collectionName,
//                    Points = { points }
//                };

//                var response = await _qdrantClient.UpsertAsync(collectionName, points);
//                return response.Status == UpdateStatus.Completed;
//            }

//            return true;
//        }
//        catch (Exception ex)
//        {
//            _logger.LogError(ex, "Error updating document {DocumentId} in vector database", document.Id);
//            return false;
//        }
//    }

//    private async Task<float[]?> GenerateQueryEmbeddingAsync(string query)
//    {
//        try
//        {
//            var random = new Random();
//            return Enumerable.Range(0, 1536).Select(_ => (float)random.NextDouble()).ToArray();
//        }
//        catch (Exception ex)
//        {
//            _logger.LogError(ex, "Error generating query embedding");
//            return null;
//        }
//    }

//    private float[]? ParseEmbedding(string embeddingString)
//    {
//        try
//        {
//            return embeddingString.Split(',').Select(float.Parse).ToArray();
//        }
//        catch
//        {
//            return null;
//        }
//    }

//    private string TruncateContent(string content, int maxLength)
//    {
//        if (content.Length <= maxLength)
//            return content;

//        var truncated = content.Substring(0, maxLength);
//        var lastSpace = truncated.LastIndexOf(' ');
        
//        return lastSpace > 0 ? truncated.Substring(0, lastSpace) + "..." : truncated + "...";
//    }
//}
