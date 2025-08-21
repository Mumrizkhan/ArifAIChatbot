//using KnowledgeBaseService.Models;
//using Shared.Domain.Entities;


//namespace KnowledgeBaseService.Services;

//public interface IVectorSearchService
//{
//    Task<List<DocumentSearchResult>> SearchSimilarDocumentsAsync(DocumentSearchRequest request, Guid tenantId);
//    Task<bool> CreateCollectionAsync(string collectionName, Guid tenantId);
//    Task<bool> DeleteCollectionAsync(string collectionName);
//    Task<bool> DeleteDocumentFromVectorAsync(Guid documentId, string collectionName);
//    Task<List<string>> GetCollectionsAsync(Guid tenantId);
//    Task<bool> UpdateDocumentInVectorAsync(Document document, List<DocumentChunk> chunks);
//}
