using NotificationService.Models;
using Shared.Domain.Entities;

namespace NotificationService.Services;

public interface INotificationService
{
    Task<Guid> SendNotificationAsync(SendNotificationRequest request, Guid tenantId);
    Task<List<Guid>> SendBulkNotificationAsync(BulkNotificationRequest request, Guid tenantId);
    Task<List<Notification>> GetNotificationsAsync(Guid tenantId, Guid? userId = null, int page = 1, int pageSize = 20);
    Task<Notification?> GetNotificationAsync(Guid notificationId, Guid tenantId);
    Task<bool> MarkAsReadAsync(Guid notificationId, Guid tenantId);
    Task<bool> MarkAllAsReadAsync(Guid userId, Guid tenantId);
    Task<NotificationStatistics> GetStatisticsAsync(Guid tenantId, DateTime? startDate = null, DateTime? endDate = null);
    Task<bool> CancelNotificationAsync(Guid notificationId, Guid tenantId);
    Task<bool> RetryFailedNotificationAsync(Guid notificationId, Guid tenantId);
    Task ProcessScheduledNotificationsAsync();
    
    // Maintenance methods for Hangfire recurring jobs
    Task CleanupOldNotificationsAsync(DateTime cutoffDate);
    Task RetryFailedNotificationsAsync();
}
