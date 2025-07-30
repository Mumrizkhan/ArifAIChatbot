using Shared.Domain.Common;

namespace KnowledgeBaseService.Models;

//public class Document : AuditableEntity
//{
//    public Guid Id { get; set; }
//    public string Title { get; set; } = string.Empty;
//    public string Content { get; set; } = string.Empty;
//    public string OriginalFileName { get; set; } = string.Empty;
//    public string FileType { get; set; } = string.Empty;
//    public long FileSize { get; set; }
//    public string FilePath { get; set; } = string.Empty;
//    public DocumentStatus Status { get; set; } = DocumentStatus.Processing;
//    public Guid TenantId { get; set; }
//    public Guid UploadedByUserId { get; set; }
//    public string? Summary { get; set; }
//    public List<string> Tags { get; set; } = new();
//    public string Language { get; set; } = "en";
//    public Dictionary<string, object> Metadata { get; set; } = new();
//    public int ChunkCount { get; set; }
//    public bool IsEmbedded { get; set; }
//    public string? VectorCollectionName { get; set; }
//}

//public class DocumentChunk : AuditableEntity
//{
//    public Guid Id { get; set; }
//    public Guid DocumentId { get; set; }
//    public string Content { get; set; } = string.Empty;
//    public int ChunkIndex { get; set; }
//    public int StartPosition { get; set; }
//    public int EndPosition { get; set; }
//    public string? Embedding { get; set; }
//    public Dictionary<string, object> Metadata { get; set; } = new();
//    public Guid TenantId { get; set; }
//}

//public enum DocumentStatus
//{
//    Uploaded,
//    Processing,
//    Processed,
//    Failed,
//    Archived
//}

public class DocumentUploadRequest
{
    public string Title { get; set; } = string.Empty;
    public IFormFile File { get; set; } = null!;
    public List<string> Tags { get; set; } = new();
    public string Language { get; set; } = "en";
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class DocumentSearchRequest
{
    public string Query { get; set; } = string.Empty;
    public string Language { get; set; } = "en";
    public List<string>? Tags { get; set; }
    public int Limit { get; set; } = 10;
    public double MinScore { get; set; } = 0.7;
    public Dictionary<string, object>? Filters { get; set; }
}

public class DocumentSearchResult
{
    public Guid DocumentId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public double Score { get; set; }
    public List<string> Tags { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
    public string? Summary { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class DocumentProcessingResult
{
    public bool IsSuccessful { get; set; }
    public string? ErrorMessage { get; set; }
    public int ChunksCreated { get; set; }
    public string? Summary { get; set; }
    public List<string> ExtractedTags { get; set; } = new();
    public TimeSpan ProcessingTime { get; set; }
}

public class KnowledgeBaseStatistics
{
    public int TotalDocuments { get; set; }
    public int ProcessedDocuments { get; set; }
    public int FailedDocuments { get; set; }
    public long TotalFileSize { get; set; }
    public int TotalChunks { get; set; }
    public Dictionary<string, int> DocumentsByType { get; set; } = new();
    public Dictionary<string, int> DocumentsByLanguage { get; set; } = new();
    public DateTime LastUpdated { get; set; }
}

public class RAGRequest
{
    public string Query { get; set; } = string.Empty;
    public string Language { get; set; } = "en";
    public int MaxDocuments { get; set; } = 5;
    public double MinRelevanceScore { get; set; } = 0.7;
    public List<string>? Tags { get; set; }
    public Dictionary<string, object>? Filters { get; set; }
}

public class RAGResponse
{
    public string GeneratedResponse { get; set; } = string.Empty;
    public List<DocumentSearchResult> SourceDocuments { get; set; } = new();
    public double ConfidenceScore { get; set; }
    public int TokensUsed { get; set; }
    public TimeSpan ProcessingTime { get; set; }
    public bool IsSuccessful { get; set; } = true;
    public string? ErrorMessage { get; set; }
}
