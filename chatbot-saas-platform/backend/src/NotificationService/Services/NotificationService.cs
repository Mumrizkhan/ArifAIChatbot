using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using Shared.Application.Common.Interfaces;
using NotificationService.Services;
using NotificationService.Models;
using NotificationService.Hubs;
using Shared.Domain.Entities;

namespace NotificationService.Services;

public class NotificationService : INotificationService
{
    private readonly IApplicationDbContext _context;
    private readonly IEmailService _emailService;
    private readonly ISmsService _smsService;
    private readonly IPushNotificationService _pushNotificationService;
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        IApplicationDbContext context,
        IEmailService emailService,
        ISmsService smsService,
        IPushNotificationService pushNotificationService,
        IHubContext<NotificationHub> hubContext,
        ILogger<NotificationService> logger)
    {
        _context = context;
        _emailService = emailService;
        _smsService = smsService;
        _pushNotificationService = pushNotificationService;
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task<Guid> SendNotificationAsync(SendNotificationRequest request, Guid tenantId)
    {
        try
        {
            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                Title = request.Title,
                Content = request.Content,
                Type = request.Type,
                TenantId = tenantId,
                UserId = request.UserId,
                RecipientEmail = request.RecipientEmail,
                RecipientPhone = request.RecipientPhone,
                RecipientDeviceToken = request.RecipientDeviceToken,
                Data = request.Data,
                TemplateId = request.TemplateId,
                TemplateData = request.TemplateData,
                ScheduledAt = request.ScheduledAt,
                Language = request.Language,
                Status = request.ScheduledAt.HasValue ? NotificationStatus.Scheduled : NotificationStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            _context.Set<Notification>().Add(notification);
            await _context.SaveChangesAsync();

            if (!request.ScheduledAt.HasValue)
            {
                await ProcessNotificationAsync(notification, request.Channels);
            }

            return notification.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending notification for tenant {TenantId}", tenantId);
            throw;
        }
    }

    public async Task<List<Guid>> SendBulkNotificationAsync(BulkNotificationRequest request, Guid tenantId)
    {
        try
        {
            var notificationIds = new List<Guid>();
            var notifications = new List<Notification>();

            if (request.UserIds != null)
            {
                foreach (var userId in request.UserIds)
                {
                    var notification = new Notification
                    {
                        Id = Guid.NewGuid(),
                        Title = request.Title,
                        Content = request.Content,
                        Type = request.Type,
                        TenantId = tenantId,
                        UserId = userId,
                        Data = request.Data,
                        TemplateId = request.TemplateId,
                        TemplateData = request.TemplateData,
                        ScheduledAt = request.ScheduledAt,
                        Language = request.Language,
                        Status = request.ScheduledAt.HasValue ? NotificationStatus.Scheduled : NotificationStatus.Pending,
                        CreatedAt = DateTime.UtcNow
                    };

                    notifications.Add(notification);
                    notificationIds.Add(notification.Id);
                }
            }

            if (request.RecipientEmails != null)
            {
                foreach (var email in request.RecipientEmails)
                {
                    var notification = new Notification
                    {
                        Id = Guid.NewGuid(),
                        Title = request.Title,
                        Content = request.Content,
                        Type = request.Type,
                        TenantId = tenantId,
                        RecipientEmail = email,
                        Data = request.Data,
                        TemplateId = request.TemplateId,
                        TemplateData = request.TemplateData,
                        ScheduledAt = request.ScheduledAt,
                        Language = request.Language,
                        Status = request.ScheduledAt.HasValue ? NotificationStatus.Scheduled : NotificationStatus.Pending,
                        CreatedAt = DateTime.UtcNow
                    };

                    notifications.Add(notification);
                    notificationIds.Add(notification.Id);
                }
            }

            if (request.RecipientPhones != null)
            {
                foreach (var phone in request.RecipientPhones)
                {
                    var notification = new Notification
                    {
                        Id = Guid.NewGuid(),
                        Title = request.Title,
                        Content = request.Content,
                        Type = request.Type,
                        TenantId = tenantId,
                        RecipientPhone = phone,
                        Data = request.Data,
                        TemplateId = request.TemplateId,
                        TemplateData = request.TemplateData,
                        ScheduledAt = request.ScheduledAt,
                        Language = request.Language,
                        Status = request.ScheduledAt.HasValue ? NotificationStatus.Scheduled : NotificationStatus.Pending,
                        CreatedAt = DateTime.UtcNow
                    };

                    notifications.Add(notification);
                    notificationIds.Add(notification.Id);
                }
            }

            _context.Set<Notification>().AddRange(notifications);
            await _context.SaveChangesAsync();

            if (!request.ScheduledAt.HasValue)
            {
                var tasks = notifications.Select(n => ProcessNotificationAsync(n, request.Channels));
                await Task.WhenAll(tasks);
            }

            return notificationIds;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending bulk notification for tenant {TenantId}", tenantId);
            throw;
        }
    }

    public async Task<List<Notification>> GetNotificationsAsync(Guid tenantId, Guid? userId = null, int page = 1, int pageSize = 20)
    {
        try
        {
            var query = _context.Set<Notification>()
                .Where(n => n.TenantId == tenantId);

            if (userId.HasValue)
            {
                query = query.Where(n => n.UserId == userId.Value);
            }

            return await query
                .OrderByDescending(n => n.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting notifications for tenant {TenantId}", tenantId);
            throw;
        }
    }

    public async Task<Notification?> GetNotificationAsync(Guid notificationId, Guid tenantId)
    {
        try
        {
            return await _context.Set<Notification>()
                .FirstOrDefaultAsync(n => n.Id == notificationId && n.TenantId == tenantId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting notification {NotificationId} for tenant {TenantId}", notificationId, tenantId);
            throw;
        }
    }

    public async Task<bool> MarkAsReadAsync(Guid notificationId, Guid tenantId)
    {
        try
        {
            var notification = await GetNotificationAsync(notificationId, tenantId);
            if (notification == null)
                return false;

            notification.ReadAt = DateTime.UtcNow;
            notification.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking notification {NotificationId} as read", notificationId);
            return false;
        }
    }

    public async Task<bool> MarkAllAsReadAsync(Guid userId, Guid tenantId)
    {
        try
        {
            var notifications = await _context.Set<Notification>()
                .Where(n => n.UserId == userId && n.TenantId == tenantId && n.ReadAt == null)
                .ToListAsync();

            foreach (var notification in notifications)
            {
                notification.ReadAt = DateTime.UtcNow;
                notification.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking all notifications as read for user {UserId}", userId);
            return false;
        }
    }

    public async Task<NotificationStatistics> GetStatisticsAsync(Guid tenantId, DateTime? startDate = null, DateTime? endDate = null)
    {
        try
        {
            var start = startDate ?? DateTime.UtcNow.AddDays(-30);
            var end = endDate ?? DateTime.UtcNow;

            var notifications = await _context.Set<Notification>()
                .Where(n => n.TenantId == tenantId && n.CreatedAt >= start && n.CreatedAt <= end)
                .ToListAsync();

            return new NotificationStatistics
            {
                TotalSent = notifications.Count(n => n.Status == NotificationStatus.Sent || n.Status == NotificationStatus.Delivered),
                TotalDelivered = notifications.Count(n => n.Status == NotificationStatus.Delivered),
                TotalRead = notifications.Count(n => n.ReadAt.HasValue),
                TotalFailed = notifications.Count(n => n.Status == NotificationStatus.Failed),
                DeliveryRate = notifications.Count > 0 ? (double)notifications.Count(n => n.Status == NotificationStatus.Delivered) / notifications.Count * 100 : 0,
                ReadRate = notifications.Count > 0 ? (double)notifications.Count(n => n.ReadAt.HasValue) / notifications.Count * 100 : 0,
                ByChannel = notifications.GroupBy(n => n.Channel).ToDictionary(g => g.Key, g => g.Count()),
                ByType = notifications.GroupBy(n => n.Type).ToDictionary(g => g.Key, g => g.Count()),
                PeriodStart = start,
                PeriodEnd = end
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting notification statistics for tenant {TenantId}", tenantId);
            throw;
        }
    }

    public async Task<bool> CancelNotificationAsync(Guid notificationId, Guid tenantId)
    {
        try
        {
            var notification = await GetNotificationAsync(notificationId, tenantId);
            if (notification == null || notification.Status != NotificationStatus.Scheduled)
                return false;

            notification.Status = NotificationStatus.Cancelled;
            notification.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling notification {NotificationId}", notificationId);
            return false;
        }
    }

    public async Task<bool> RetryFailedNotificationAsync(Guid notificationId, Guid tenantId)
    {
        try
        {
            var notification = await GetNotificationAsync(notificationId, tenantId);
            if (notification == null || notification.Status != NotificationStatus.Failed)
                return false;

            if (notification.RetryCount >= notification.MaxRetries)
                return false;

            notification.Status = NotificationStatus.Pending;
            notification.RetryCount++;
            notification.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var channels = new List<NotificationChannel> { notification.Channel };
            await ProcessNotificationAsync(notification, channels);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrying notification {NotificationId}", notificationId);
            return false;
        }
    }

    public async Task ProcessScheduledNotificationsAsync()
    {
        try
        {
            var scheduledNotifications = await _context.Set<Notification>()
                .Where(n => n.Status == NotificationStatus.Scheduled && 
                           n.ScheduledAt.HasValue && 
                           n.ScheduledAt.Value <= DateTime.UtcNow)
                .ToListAsync();

            foreach (var notification in scheduledNotifications)
            {
                var channels = new List<NotificationChannel> { notification.Channel };
                await ProcessNotificationAsync(notification, channels);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing scheduled notifications");
        }
    }

    private async Task ProcessNotificationAsync(Notification notification, List<NotificationChannel> channels)
    {
        try
        {
            notification.Status = NotificationStatus.Sending;
            notification.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var success = false;

            foreach (var channel in channels)
            {
                notification.Channel = channel;
                
                switch (channel)
                {
                    case NotificationChannel.Email:
                        if (!string.IsNullOrEmpty(notification.RecipientEmail))
                        {
                            success = await _emailService.SendEmailAsync(
                                notification.RecipientEmail,
                                notification.Title,
                                notification.Content,
                                notification.TemplateId,
                                notification.TemplateData);
                        }
                        break;

                    case NotificationChannel.SMS:
                        if (!string.IsNullOrEmpty(notification.RecipientPhone))
                        {
                            success = await _smsService.SendSmsAsync(
                                notification.RecipientPhone,
                                notification.Content,
                                notification.TemplateId,
                                notification.TemplateData);
                        }
                        break;

                    case NotificationChannel.Push:
                        if (!string.IsNullOrEmpty(notification.RecipientDeviceToken))
                        {
                            success = await _pushNotificationService.SendPushNotificationAsync(
                                notification.RecipientDeviceToken,
                                notification.Title,
                                notification.Content,
                                notification.Data);
                        }
                        break;

                    case NotificationChannel.InApp:
                        if (notification.UserId.HasValue)
                        {
                            await _hubContext.Clients.User(notification.UserId.Value.ToString())
                                .SendAsync("ReceiveNotification", new
                                {
                                    notification.Id,
                                    notification.Title,
                                    notification.Content,
                                    notification.Type,
                                    notification.Data,
                                    notification.CreatedAt
                                });
                            success = true;
                        }
                        break;
                }

                if (success)
                    break;
            }

            notification.Status = success ? NotificationStatus.Sent : NotificationStatus.Failed;
            notification.SentAt = success ? DateTime.UtcNow : null;
            notification.UpdatedAt = DateTime.UtcNow;

            if (!success)
            {
                notification.ErrorMessage = "Failed to send through all channels";
            }

            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing notification {NotificationId}", notification.Id);
            
            notification.Status = NotificationStatus.Failed;
            notification.ErrorMessage = ex.Message;
            notification.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }
}
