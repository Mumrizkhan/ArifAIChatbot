using Shared.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Domain.Entities
{
    public class Document : AuditableEntity
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string OriginalFileName { get; set; } = string.Empty;
        public string FileType { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public string FilePath { get; set; } = string.Empty;
        public DocumentStatus Status { get; set; } = DocumentStatus.Processing;
        public Guid TenantId { get; set; }
        public Guid UploadedByUserId { get; set; }
        public string? Summary { get; set; }
        public List<string> Tags { get; set; } = new();
        public string Language { get; set; } = "en";
        public Dictionary<string, object> Metadata { get; set; } = new();
        public int ChunkCount { get; set; }
        public bool IsEmbedded { get; set; }
        public string? VectorCollectionName { get; set; }
    }

    public class DocumentChunk : AuditableEntity
    {
        public Guid Id { get; set; }
        public Guid DocumentId { get; set; }
        public string Content { get; set; } = string.Empty;
        public int ChunkIndex { get; set; }
        public int StartPosition { get; set; }
        public int EndPosition { get; set; }
        public string? Embedding { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
        public Guid TenantId { get; set; }
    }

    public enum DocumentStatus
    {
        Uploaded,
        Processing,
        Processed,
        Failed,
        Archived
    }
}
