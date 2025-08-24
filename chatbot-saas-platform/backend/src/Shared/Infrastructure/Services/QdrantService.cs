using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Qdrant.Client;
using Qdrant.Client.Grpc;

using Shared.Application.Common.Models;
using Shared.Domain.Entities;

namespace Shared.Application.Services;

public class QdrantService : IVectorService
{
    private readonly QdrantClient _qdrantClient;
    private readonly ILogger<QdrantService> _logger;
    private readonly IConfiguration _configuration;

    public QdrantService(ILogger<QdrantService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
        
        var host = _configuration["Qdrant:Host"] ?? "localhost";
        var port = int.Parse(_configuration["Qdrant:Port"] ?? "6334");
        
        _qdrantClient = new QdrantClient(host, port, https: false);
    }

    public async Task<List<SearchResult>> SearchSimilarAsync(string query, string collectionName, int limit = 5)
    {
        try
        {
            var queryVector = ParseEmbedding(query);
            
            var response = await _qdrantClient.SearchAsync(collectionName, queryVector, limit: (ulong)limit);
            
            return response.Select(point => new SearchResult
            {
                Id = point.Id.Uuid,
                Content = point.Payload.TryGetValue("content", out var content) ? content.StringValue : "",
                Score = (double)point.Score,
                Metadata = point.Payload.ToDictionary(
                    kvp => kvp.Key,
                    kvp => (object)kvp.Value.StringValue
                )
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error searching in collection {collectionName}");
            return new List<SearchResult>();
        }
    }
   

    public async Task<List<SearchResult>> SearchAcrossAllCollectionsAsync(Guid tenantId, string query, int limit = 5)
    {
        try
        {
            // Get all collections for the tenant
            var allCollections = await GetCollectionsAsync();
            var tenantCollections = allCollections
                .Where(c => c.StartsWith($"tenant_{tenantId:N}_collection_"))
                .ToList();

            var allResults = new List<SearchResult>();

            // Search each collection
            foreach (var collection in tenantCollections)
            {
                var results = await SearchSimilarAsync(query, collection, limit);
                allResults.AddRange(results);
            }

            // Sort results by score and limit the total number of results
            return allResults
                .OrderByDescending(r => r.Score)
                .Take(limit)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error searching across all collections for tenant {tenantId}");
            return new List<SearchResult>();
        }
    }

    public async Task<bool> UpsertDocumentAsync(Domain.Entities.Document document, Dictionary<string, object> metadata)
    {
        try
        {
            var embedding = GenerateDummyEmbedding();

            var payload = new Dictionary<string, Value>
            {
                ["content"] = document.Content,
                ["document_id"] = document.Id.ToString(),
                ["document_title"] = metadata.ContainsKey("title") ? metadata["title"].ToString() : "Untitled",
                ["document_tags"] = metadata.ContainsKey("tags") ? string.Join(",", (List<string>)metadata["tags"]) : ""
            };

            foreach (var meta in metadata)
            {
                if (!payload.ContainsKey(meta.Key))
                {
                    payload[meta.Key] = meta.Value.ToString();
                }
            }

            var point = new PointStruct
            {
                Id = new PointId { Uuid = Guid.NewGuid().ToString() }, // Unique ID for each chunk
                Vectors = embedding
            };

            foreach (var kvp in payload)
            {
                point.Payload[kvp.Key] = kvp.Value;
            }

            var response = await _qdrantClient.UpsertAsync(document.VectorCollectionName, new[] { point });
            return response.Status == UpdateStatus.Completed;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error upserting document {document.Id} to collection {document.VectorCollectionName}");
            return false;
        }
    }

    public async Task<bool> DeleteDocumentAsync(string collectionName, Guid documentId)
    {
        try
        {
            if (Guid.Empty!=documentId)
            {
                await _qdrantClient.DeleteAsync(collectionName, documentId);
            }
            else
            {
                _logger.LogWarning($"Invalid GUID format for document ID: {documentId}");
                return false;
            }
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error deleting document {documentId} from collection {collectionName}");
            return false;
        }
    }

    public async Task<bool> CreateCollectionAsync(string collectionName, int vectorSize = 1536)
    {
        try
        {
            await _qdrantClient.CreateCollectionAsync(collectionName, new VectorParams
            {
                Size = (ulong)vectorSize,
                Distance = Distance.Cosine
            });
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error creating collection {collectionName}");
            return false;
        }
    }

    public async Task<bool> CreateCollectionAsync(Guid tenantId, int vectorSize = 1536)
    {
        var collectionName = Guid.NewGuid();
        try
        {
            var fullCollectionName = $"tenant_{tenantId}_collection_{collectionName}";
            await _qdrantClient.CreateCollectionAsync(fullCollectionName, new VectorParams
            {
                Size = (ulong)vectorSize,
                Distance = Distance.Cosine
            });
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error creating collection {collectionName} for tenant {tenantId}");
            return false;
        }
    }

    public async Task<bool> DeleteCollectionAsync(string collectionName)
    {
        try
        {
            await _qdrantClient.DeleteCollectionAsync(collectionName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error deleting collection {collectionName}");
            return false;
        }
    }

    public async Task<List<string>> GetCollectionsAsync()
    {
        try
        {
            var response = await _qdrantClient.ListCollectionsAsync();
            return response.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing collections");
            return new List<string>();
        }
    }

    public async Task<List<string>> GetTenantCollectionsAsync(Guid tenantId)
    {
        try
        {
            var allCollections = await _qdrantClient.ListCollectionsAsync();
            return allCollections.Where(c => c.StartsWith($"tenant_{tenantId}_collection_")).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error listing collections for tenant {tenantId}");
            return new List<string>();
        }
    }

  

  

    private float[] GenerateDummyEmbedding(int size = 1536)
    {
        var random = new Random();
        return Enumerable.Range(0, size).Select(_ => (float)random.NextDouble()).ToArray();
    }
      
   
    public async Task<bool> UpsertDocumentAsync(Domain.Entities.Document document, List<DocumentChunk> chunks)
    {
        try
        {
           

            var points = new List<PointStruct>();

            foreach (var chunk in chunks.Where(c => !string.IsNullOrEmpty(c.Embedding)))
            {
                var embedding = ParseEmbedding(chunk.Embedding!);
                if (embedding != null)
                {
                    var payload = new Dictionary<string, Value>
                    {
                        ["content"] = chunk.Content,
                        ["document_id"] = document.Id.ToString(),
                        ["document_title"] = document.Title,
                        ["chunk_index"] = chunk.ChunkIndex.ToString(),
                        ["language"] = document.Language
                    };

                    foreach (var meta in chunk.Metadata)
                    {
                        payload[meta.Key] = meta.Value.ToString() ?? "";
                    }

                    var point = new PointStruct
                    {
                        Id = new PointId { Uuid = chunk.Id.ToString() },
                        Vectors = embedding
                    };

                    foreach (var kvp in payload)
                    {
                        point.Payload[kvp.Key] = kvp.Value;
                    }

                    points.Add(point);
                }
            }

            if (points.Any())
            {
                var upsertRequest = new UpsertPoints
                {
                    CollectionName = document.VectorCollectionName,
                    Points = { points }
                };

                var response = await _qdrantClient.UpsertAsync(document.VectorCollectionName, points);
                return response.Status == UpdateStatus.Completed;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating document {DocumentId} in vector database", document.Id);
            return false;
        }
    }

    private async Task<float[]?> GenerateQueryEmbeddingAsync(string query)
    {
        try
        {
            var random = new Random();
            return Enumerable.Range(0, 1536).Select(_ => (float)random.NextDouble()).ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating query embedding");
            return null;
        }
    }
    private float[] ParseEmbedding(string embeddingString)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(embeddingString) || !embeddingString.All(c => char.IsDigit(c) || c == ',' || char.IsWhiteSpace(c)))
            {
                throw new FormatException("Input string is not in the correct format for parsing.");
            }
            return embeddingString.Split(',').Select(float.Parse).ToArray();
        }
        catch
        {
            return GenerateDummyEmbedding();
        }
    }
   

    private string TruncateContent(string content, int maxLength)
    {
        if (content.Length <= maxLength)
            return content;

        var truncated = content.Substring(0, maxLength);
        var lastSpace = truncated.LastIndexOf(' ');

        return lastSpace > 0 ? truncated.Substring(0, lastSpace) + "..." : truncated + "...";
    }

   
}
