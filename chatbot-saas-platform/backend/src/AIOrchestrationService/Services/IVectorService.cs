using AIOrchestrationService.Models;

namespace AIOrchestrationService.Services;

public interface IVectorService
{
    Task<List<SearchResult>> SearchSimilarAsync(string query, string collectionName, int limit = 5);
    Task<bool> UpsertDocumentAsync(string collectionName, string documentId, string content, Dictionary<string, object> metadata);
    Task<bool> DeleteDocumentAsync(string collectionName, string documentId);
    Task<bool> CreateCollectionAsync(string collectionName, int vectorSize = 1536);
    Task<bool> DeleteCollectionAsync(string collectionName);
    Task<List<string>> GetCollectionsAsync();
}
