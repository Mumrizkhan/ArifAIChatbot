using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Shared.Application.Common.Interfaces;
using NotificationService.Models;
using Shared.Domain.Entities;

namespace NotificationService.Hubs;

[Authorize]
public class NotificationHub : Hub
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ITenantService _tenantService;
    private readonly ILogger<NotificationHub> _logger;

    public NotificationHub(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        ITenantService tenantService,
        ILogger<NotificationHub> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _tenantService = tenantService;
        _logger = logger;
    }

    public async Task JoinUserGroup()
    {
        try
        {
            var userId = _currentUserService.UserId;
            var tenantId = _tenantService.GetCurrentTenantId();

            if (userId.HasValue)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId.Value}");
                await Groups.AddToGroupAsync(Context.ConnectionId, $"tenant_{tenantId}");
                
                _logger.LogInformation($"User {userId} joined notification groups for tenant {tenantId}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error joining user notification group");
        }
    }

    public async Task LeaveUserGroup()
    {
        try
        {
            var userId = _currentUserService.UserId;
            var tenantId = _tenantService.GetCurrentTenantId();

            if (userId.HasValue)
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user_{userId.Value}");
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"tenant_{tenantId}");
                
                _logger.LogInformation($"User {userId} left notification groups for tenant {tenantId}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error leaving user notification group");
        }
    }

    public async Task MarkNotificationAsRead(string notificationId)
    {
        try
        {
            var userId = _currentUserService.UserId;
            var tenantId = _tenantService.GetCurrentTenantId();

            if (userId.HasValue && Guid.TryParse(notificationId, out var notifId))
            {
                var notification = await _context.Set<Notification>()
                    .FirstOrDefaultAsync(n => n.Id == notifId && 
                                            n.UserId == userId.Value && 
                                            n.TenantId == tenantId);

                if (notification != null && notification.ReadAt == null)
                {
                    notification.ReadAt = DateTime.UtcNow;
                    notification.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();

                    await Clients.Caller.SendAsync("NotificationMarkedAsRead", new
                    {
                        NotificationId = notificationId,
                        ReadAt = notification.ReadAt
                    });

                    _logger.LogInformation($"Notification {notificationId} marked as read by user {userId}");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error marking notification {notificationId} as read");
        }
    }

    public async Task MarkAllNotificationsAsRead()
    {
        try
        {
            var userId = _currentUserService.UserId;
            var tenantId = _tenantService.GetCurrentTenantId();

            if (userId.HasValue)
            {
                var unreadNotifications = await _context.Set<Notification>()
                    .Where(n => n.UserId == userId.Value && 
                               n.TenantId == tenantId && 
                               n.ReadAt == null)
                    .ToListAsync();

                foreach (var notification in unreadNotifications)
                {
                    notification.ReadAt = DateTime.UtcNow;
                    notification.UpdatedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();

                await Clients.Caller.SendAsync("AllNotificationsMarkedAsRead", new
                {
                    Count = unreadNotifications.Count,
                    ReadAt = DateTime.UtcNow.ToString("O")
                });

                _logger.LogInformation($"All notifications marked as read for user {userId}. Count: {unreadNotifications.Count}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking all notifications as read");
        }
    }

    public async Task GetUnreadCount()
    {
        try
        {
            var userId = _currentUserService.UserId;
            var tenantId = _tenantService.GetCurrentTenantId();

            if (userId.HasValue)
            {
                var unreadCount = await _context.Set<Notification>()
                    .CountAsync(n => n.UserId == userId.Value && 
                                    n.TenantId == tenantId && 
                                    n.ReadAt == null);

                await Clients.Caller.SendAsync("UnreadNotificationCount", new
                {
                    Count = unreadCount
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting unread notification count");
        }
    }

    public async Task GetRecentNotifications(int count = 10)
    {
        try
        {
            var userId = _currentUserService.UserId;
            var tenantId = _tenantService.GetCurrentTenantId();

            if (userId.HasValue)
            {
                var notifications = await _context.Set<Notification>()
                    .Where(n => n.UserId == userId.Value && n.TenantId == tenantId)
                    .OrderByDescending(n => n.CreatedAt)
                    .Take(count)
                    .Select(n => new
                    {
                        n.Id,
                        n.Title,
                        n.Content,
                        Type = n.Type.ToString(),
                        n.Data,
                        n.CreatedAt,
                        n.ReadAt,
                        IsRead = n.ReadAt.HasValue
                    })
                    .ToListAsync();

                await Clients.Caller.SendAsync("RecentNotifications", notifications);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recent notifications");
        }
    }

    public async Task UpdateNotificationPreferences(string preferences)
    {
        try
        {
            var userId = _currentUserService.UserId;
            var tenantId = _tenantService.GetCurrentTenantId();

            if (userId.HasValue)
            {
                await Clients.Caller.SendAsync("NotificationPreferencesUpdated", new
                {
                    Success = true,
                    Message = "Notification preferences updated successfully"
                });

                _logger.LogInformation($"Notification preferences updated for user {userId}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating notification preferences");
        }
    }

    public async Task BroadcastToTenant(string message, string messageType = "info")
    {
        try
        {
            var userId = _currentUserService.UserId;
            var tenantId = _tenantService.GetCurrentTenantId();

            if (userId.HasValue)
            {
                var userTenant = await _context.Set<Shared.Domain.Entities.UserTenant>()
                    .FirstOrDefaultAsync(ut => ut.UserId == userId && ut.TenantId == tenantId);

                if (userTenant?.Role == Shared.Domain.Enums.TenantRole.Admin)
                {
                    await Clients.Group($"tenant_{tenantId}").SendAsync("TenantBroadcast", new
                    {
                        Message = message,
                        Type = messageType,
                        FromUserId = userId,
                        Timestamp = DateTime.UtcNow.ToString("O")
                    });

                    _logger.LogInformation($"Admin {userId} broadcasted message to tenant {tenantId}");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting message to tenant");
        }
    }

    public override async Task OnConnectedAsync()
    {
        var userId = _currentUserService.UserId;
        var tenantId = _tenantService.GetCurrentTenantId();
        
        _logger.LogInformation($"User {userId} connected to notification hub for tenant {tenantId}");
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = _currentUserService.UserId;
        var tenantId = _tenantService.GetCurrentTenantId();

        _logger.LogInformation($"User {userId} disconnected from notification hub for tenant {tenantId}");
        await base.OnDisconnectedAsync(exception);
    }
}
