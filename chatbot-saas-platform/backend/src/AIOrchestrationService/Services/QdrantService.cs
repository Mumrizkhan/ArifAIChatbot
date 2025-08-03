using Qdrant.Client;
using Qdrant.Client.Grpc;
using AIOrchestrationService.Services;
using AIOrchestrationService.Models;

namespace AIOrchestrationService.Services;

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

    public async Task<bool> UpsertDocumentAsync(string collectionName, string documentId, string content, Dictionary<string, object> metadata)
    {
        try
        {
            var embedding = GenerateDummyEmbedding();
            
            var payload = new Dictionary<string, Value>
            {
                ["content"] = content,
                ["document_id"] = documentId
            };

            foreach (var meta in metadata)
            {
                payload[meta.Key] = meta.Value.ToString();
            }

            var point = new PointStruct
            {
                Id = new PointId { Uuid = documentId },
                Vectors = embedding
            };
            
            foreach (var kvp in payload)
            {
                point.Payload[kvp.Key] = kvp.Value;
            }

            var response = await _qdrantClient.UpsertAsync(collectionName, new[] { point });
            return response.Status == UpdateStatus.Completed;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error upserting document {documentId} to collection {collectionName}");
            return false;
        }
    }

    public async Task<bool> DeleteDocumentAsync(string collectionName, string documentId)
    {
        try
        {
            if (Guid.TryParse(documentId, out var guid))
            {
                await _qdrantClient.DeleteAsync(collectionName, guid);
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
            var createRequest = new CreateCollection
            {
                CollectionName = collectionName,
                VectorsConfig = new VectorsConfig
                {
                    Params = new VectorParams
                    {
                        Size = (ulong)vectorSize,
                        Distance = Distance.Cosine
                    }
                }
            };

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

    private float[] GenerateDummyEmbedding(int size = 1536)
    {
        var random = new Random();
        return Enumerable.Range(0, size).Select(_ => (float)random.NextDouble()).ToArray();
    }
}
