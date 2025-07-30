using Shared.Domain.Entities;

namespace ChatRuntimeService.Services;

public interface IMessageQueueService
{
    Task QueueMessageAsync(Message message);
    Task<IEnumerable<Message>> GetPendingMessagesAsync(Guid userId);
    Task MarkMessageAsDeliveredAsync(Guid messageId);
    Task ProcessOfflineMessagesAsync(Guid userId);
}
