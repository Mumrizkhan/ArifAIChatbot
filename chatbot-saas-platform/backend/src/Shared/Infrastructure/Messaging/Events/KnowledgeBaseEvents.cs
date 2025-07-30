using System;

namespace Shared.Infrastructure.Messaging.Events;

public record DocumentUploadedEvent(
    Guid DocumentId,
    Guid TenantId,
    string FileName,
    long FileSize,
    string ContentType,
    DateTime UploadedAt);

public record DocumentProcessingStartedEvent(
    Guid DocumentId,
    Guid TenantId,
    string ProcessingType,
    DateTime StartedAt);

public record DocumentProcessedEvent(
    Guid DocumentId,
    Guid TenantId,
    bool Success,
    string? ErrorMessage,
    TimeSpan ProcessingTime,
    int ChunksCreated,
    DateTime CompletedAt);

public record DocumentDeletedEvent(
    Guid DocumentId,
    Guid TenantId,
    string FileName,
    DateTime DeletedAt);

public record KnowledgeBaseSearchEvent(
    Guid TenantId,
    string Query,
    int ResultCount,
    TimeSpan SearchTime,
    bool UsedCache,
    DateTime Timestamp);
