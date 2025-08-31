using Shared.Domain.Events;

namespace Shared.Application.Common.Interfaces;

public interface IMessageBusNotificationService
{
    Task PublishNotificationAsync(NotificationCreatedEvent notificationEvent);
    Task PublishBulkNotificationAsync(BulkNotificationEvent bulkEvent);
    Task PublishNotificationReadAsync(NotificationReadEvent readEvent);
    Task PublishConversationAssignedAsync(ConversationAssignedEvent assignedEvent);
    Task PublishNewMessageAsync(NewMessageEvent messageEvent);
    Task PublishConversationEscalatedAsync(ConversationEscalatedEvent escalatedEvent);
    Task PublishSystemAlertAsync(SystemAlertEvent alertEvent);
}