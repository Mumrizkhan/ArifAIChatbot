using System;
using System.Collections.Generic;

namespace Shared.Infrastructure.Messaging.Events;

public record NotificationRequestedEvent(
    Guid NotificationId,
    Guid TenantId,
    string NotificationType,
    string Recipient,
    string Subject,
    string Content,
    Dictionary<string, object>? Metadata,
    DateTime RequestedAt);

public record NotificationSentEvent(
    Guid NotificationId,
    Guid TenantId,
    string NotificationType,
    string Recipient,
    bool Success,
    string? ErrorMessage,
    DateTime SentAt);

public record EmailQueuedEvent(
    Guid EmailId,
    Guid TenantId,
    string To,
    string Subject,
    string TemplateId,
    Dictionary<string, object> TemplateData,
    DateTime QueuedAt);

public record SmsQueuedEvent(
    Guid SmsId,
    Guid TenantId,
    string PhoneNumber,
    string Message,
    DateTime QueuedAt);
