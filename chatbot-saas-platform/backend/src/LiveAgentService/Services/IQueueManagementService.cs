using LiveAgentService.Models;

namespace LiveAgentService.Services;

public interface IQueueManagementService
{
    Task<bool> AddToQueueAsync(Guid conversationId, QueuePriority priority = QueuePriority.Normal);
    Task<bool> RemoveFromQueueAsync(Guid conversationId);
    Task<List<QueuedConversation>> GetQueuedConversationsAsync(Guid tenantId);
    Task<QueuedConversation?> GetNextInQueueAsync(Guid tenantId, string? department = null);
    Task<bool> UpdateQueuePositionAsync(Guid conversationId, int newPosition);
    Task<QueueStatistics> GetQueueStatisticsAsync(Guid tenantId);
    Task<bool> EscalateConversationAsync(Guid conversationId, string reason);
}
