using System;

namespace Shared.Infrastructure.Messaging.Events;

public record ConversationStartedEvent(
    Guid ConversationId,
    Guid TenantId,
    Guid? UserId,
    string InitialMessage,
    DateTime Timestamp);

public record ConversationEndedEvent(
    Guid ConversationId,
    Guid TenantId,
    TimeSpan Duration,
    int MessageCount,
    DateTime Timestamp);

public record MessageReceivedEvent(
    Guid ConversationId,
    Guid MessageId,
    Guid TenantId,
    string Content,
    string SenderType,
    DateTime Timestamp);

public record AIResponseGeneratedEvent(
    Guid ConversationId,
    Guid MessageId,
    Guid TenantId,
    string Response,
    TimeSpan ProcessingTime,
    bool UsedCache,
    DateTime Timestamp);

public record LiveAgentAssignedEvent(
    Guid ConversationId,
    Guid AgentId,
    Guid TenantId,
    DateTime AssignedAt,
    string Reason);

public record LiveAgentUnassignedEvent(
    Guid ConversationId,
    Guid AgentId,
    Guid TenantId,
    DateTime UnassignedAt,
    string Reason);
