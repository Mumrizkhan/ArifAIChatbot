using KnowledgeBaseService.Models;
using Shared.Domain.Entities;
namespace KnowledgeBaseService.Services;

public interface IDocumentProcessingService
{
    Task<DocumentProcessingResult> ProcessDocumentAsync(Document document, Stream fileStream);
    Task<string> ExtractTextFromFileAsync(Stream fileStream, string fileType);
    Task<List<DocumentChunk>> ChunkDocumentAsync(Document document, string content);
    Task<string> GenerateDocumentSummaryAsync(string content);
    Task<List<string>> ExtractTagsAsync(string content);
    Task<bool> GenerateEmbeddingsAsync(List<DocumentChunk> chunks);
    Task<bool> StoreInVectorDatabaseAsync(List<DocumentChunk> chunks, string collectionName);
}
