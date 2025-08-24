

using Shared.Application.Common.Models;
using Shared.Domain.Entities;

namespace Shared.Application.Services;

public interface IVectorService
{
    Task<List<SearchResult>> SearchSimilarAsync(string query, string collectionName, int limit = 5);
    Task<List<SearchResult>> SearchAcrossAllCollectionsAsync(Guid tenantId, string query, int limit = 5);
    Task<bool> UpsertDocumentAsync(Document document, Dictionary<string, object> metadata);
    Task<bool> UpsertDocumentAsync(Domain.Entities.Document document, List<DocumentChunk> chunks);
    Task<bool> DeleteDocumentAsync(string collectionName, Guid documentId);
    Task<bool> CreateCollectionAsync(string collectionName, int vectorSize = 1536);
    Task<bool> CreateCollectionAsync(Guid tenantId, int vectorSize = 1536);
    Task<bool> DeleteCollectionAsync(string collectionName);
    Task<List<string>> GetCollectionsAsync();
}
