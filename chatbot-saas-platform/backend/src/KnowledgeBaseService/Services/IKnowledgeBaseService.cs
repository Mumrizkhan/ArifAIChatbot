using KnowledgeBaseService.Models;
using Shared.Domain.Entities;

namespace KnowledgeBaseService.Services;

public interface IKnowledgeBaseService
{
    Task<Document> UploadDocumentAsync(DocumentUploadRequest request, Guid tenantId, Guid userId);
    Task<List<Document>> GetDocumentsAsync(Guid tenantId, int page = 1, int pageSize = 20);
    Task<Document?> GetDocumentAsync(Guid documentId, Guid tenantId);
    Task<bool> DeleteDocumentAsync(Guid documentId, Guid tenantId);
    Task<List<DocumentSearchResult>> SearchDocumentsAsync(DocumentSearchRequest request, Guid tenantId);
    Task<RAGResponse> GenerateRAGResponseAsync(RAGRequest request, Guid tenantId);
    Task<KnowledgeBaseStatistics> GetStatisticsAsync(Guid tenantId);
    Task<bool> ReprocessDocumentAsync(Guid documentId, Guid tenantId);
    Task<Stream> DownloadDocumentAsync(Guid documentId, Guid tenantId);
}
