using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Shared.Application.Common.Interfaces;
using Shared.Domain.Entities;
using Shared.Domain.Enums;

namespace ChatRuntimeService.Hubs;

//[Authorize]
public class ChatHub : Hub
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ITenantService _tenantService;
    private readonly ILogger<ChatHub> _logger;

    public ChatHub(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        ITenantService tenantService,
        ILogger<ChatHub> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _tenantService = tenantService;
        _logger = logger;
    }

    public async Task JoinConversation(string conversationId)
    {
        try
        {
            if (Guid.TryParse(conversationId, out var convId))
            {
                var conversation = await _context.Conversations
                    .FirstOrDefaultAsync(c => c.Id == convId && c.TenantId == _tenantService.GetCurrentTenantId());

                if (conversation != null)
                {
                    await Groups.AddToGroupAsync(Context.ConnectionId, $"conversation_{conversationId}");
                    _logger.LogInformation($"User {_currentUserService.UserId} joined conversation {conversationId}");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error joining conversation {conversationId}");
        }
    }

    public async Task LeaveConversation(string conversationId)
    {
        try
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"conversation_{conversationId}");
            _logger.LogInformation($"User {_currentUserService.UserId} left conversation {conversationId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error leaving conversation {conversationId}");
        }
    }

    public async Task SendMessage(string conversationId, string content, string messageType = "Text")
    {
        try
        {
            if (!Guid.TryParse(conversationId, out var convId))
                return;

            var conversation = await _context.Conversations
                .FirstOrDefaultAsync(c => c.Id == convId && c.TenantId == _tenantService.GetCurrentTenantId());

            if (conversation == null)
                return;

            var message = new Message
            {
                ConversationId = convId,
                Content = content,
                Type = Enum.Parse<MessageType>(messageType),
                Sender = _currentUserService.UserId.HasValue ? MessageSender.Agent : MessageSender.Customer,
                SenderId = _currentUserService.UserId,
                CreatedAt = DateTime.UtcNow,
                TenantId = _tenantService.GetCurrentTenantId()
            };

            _context.Messages.Add(message);
            conversation.MessageCount++;
            conversation.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var messageDto = new
            {
                Id = message.Id,
                ConversationId = message.ConversationId,
                Content = message.Content,
                Type = message.Type.ToString(),
                Sender = message.Sender.ToString(),
                SenderId = message.SenderId,
                CreatedAt = message.CreatedAt,
                SenderName = _currentUserService.UserId.HasValue ? 
                    $"{_currentUserService.Email}" : conversation.CustomerName ?? "Customer"
            };

            await Clients.Group($"conversation_{conversationId}").SendAsync("ReceiveMessage", messageDto);
            _logger.LogInformation($"Message sent to conversation {conversationId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error sending message to conversation {conversationId}");
        }
    }

    public async Task StartTyping(string conversationId)
    {
        try
        {
            var typingInfo = new
            {
                UserId = _currentUserService.UserId,
                UserName = _currentUserService.Email ?? "User",
                ConversationId = conversationId
            };

            await Clients.OthersInGroup($"conversation_{conversationId}")
                .SendAsync("UserStartedTyping", typingInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error broadcasting typing status for conversation {conversationId}");
        }
    }

    public async Task StopTyping(string conversationId)
    {
        try
        {
            var typingInfo = new
            {
                UserId = _currentUserService.UserId,
                ConversationId = conversationId
            };

            await Clients.OthersInGroup($"conversation_{conversationId}")
                .SendAsync("UserStoppedTyping", typingInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error broadcasting stop typing status for conversation {conversationId}");
        }
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation($"User {_currentUserService.UserId} connected to chat hub");
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation($"User {_currentUserService.UserId} disconnected from chat hub");
        await base.OnDisconnectedAsync(exception);
    }

    public async Task JoinTenantGroup(string tenantId)
    {
        try
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"Tenant_{tenantId}");
            _logger.LogInformation($"User {_currentUserService.UserId} joined tenant group {tenantId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error joining tenant group");
        }
    }

    public async Task RequestTenantAnalyticsUpdate(string tenantId, string startDate, string endDate)
    {
        try
        {
            var analytics = new
            {
                conversations = new { total = 1247, active = 23, resolved = 1156, averageDuration = 15.2, satisfactionScore = 4.6 },
                agents = new { online = 8, busy = 3, averageResponseTime = 2.4, utilization = 75 },
                customers = new { total = 892, returning = 401, newToday = 12, averageRating = 4.5 },
                performance = new { resolutionRate = 92, firstResponseTime = 2.1, escalationRate = 8, botAccuracy = 87 },
                trends = new
                {
                    conversationVolume = new[]
                    {
                        new { date = "2024-01-01", count = 120 },
                        new { date = "2024-01-02", count = 135 },
                        new { date = "2024-01-03", count = 98 }
                    },
                    satisfactionTrend = new[]
                    {
                        new { date = "2024-01-01", score = 4.5 },
                        new { date = "2024-01-02", score = 4.7 },
                        new { date = "2024-01-03", score = 4.6 }
                    },
                    responseTimeTrend = new[]
                    {
                        new { date = "2024-01-01", time = 2.3 },
                        new { date = "2024-01-02", time = 2.1 },
                        new { date = "2024-01-03", time = 2.4 }
                    }
                },
                realtime = new
                {
                    activeConversations = 23,
                    waitingCustomers = 5,
                    onlineAgents = 8,
                    currentLoad = 68
                }
            };
            
            await Clients.Group($"Tenant_{tenantId}").SendAsync("TenantAnalyticsUpdated", analytics);
            _logger.LogInformation($"Tenant analytics sent to tenant {tenantId} for period {startDate} to {endDate}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending tenant analytics update");
        }
    }

    public async Task RequestTenantRealtimeUpdate(string tenantId)
    {
        try
        {
            var realtime = new
            {
                activeConversations = 23,
                waitingCustomers = 5,
                onlineAgents = 8,
                currentLoad = 68
            };
            
            await Clients.Group($"Tenant_{tenantId}").SendAsync("TenantRealtimeUpdated", realtime);
            _logger.LogInformation($"Tenant realtime data sent to tenant {tenantId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending tenant realtime update");
        }
    }

    public async Task JoinAdminGroup()
    {
        try
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "AdminGroup");
            _logger.LogInformation($"Admin user {_currentUserService.UserId} joined admin group");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error joining admin group");
        }
    }

    public async Task RequestDashboardStatsUpdate()
    {
        try
        {
            var stats = new
            {
                totalTenants = 42,
                totalUsers = 1247,
                totalConversations = 8934,
                activeAgents = 18,
                monthlyRevenue = 125000,
                growthRate = 12.5
            };
            
            await Clients.Caller.SendAsync("DashboardStatsUpdated", stats);
            _logger.LogInformation($"Dashboard stats sent to user {_currentUserService.UserId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending dashboard stats update");
        }
    }

    public async Task RequestConversationMetricsUpdate(string timeRange, string tenantId = null)
    {
        try
        {
            var metrics = new
            {
                totalConversations = 1247,
                averageDuration = 15.2,
                resolutionRate = 92,
                satisfactionScore = 4.6,
                dailyData = new[]
                {
                    new { date = "2024-01-01", conversations = 120, resolved = 110, avgDuration = 15 },
                    new { date = "2024-01-02", conversations = 135, resolved = 125, avgDuration = 18 },
                    new { date = "2024-01-03", conversations = 98, resolved = 88, avgDuration = 12 }
                }
            };
            
            await Clients.Caller.SendAsync("ConversationMetricsUpdated", metrics);
            _logger.LogInformation($"Conversation metrics sent to user {_currentUserService.UserId} for timeRange: {timeRange}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending conversation metrics update");
        }
    }

    public async Task RequestAgentMetricsUpdate(string timeRange, string tenantId = null)
    {
        try
        {
            var metrics = new
            {
                totalAgents = 24,
                activeAgents = 18,
                averageResponseTime = 2.4,
                averageRating = 4.7,
                topPerformers = new[]
                {
                    new { id = "1", name = "John Doe", rating = 4.9, conversationsHandled = 156 },
                    new { id = "2", name = "Jane Smith", rating = 4.8, conversationsHandled = 142 }
                }
            };
            
            await Clients.Caller.SendAsync("AgentMetricsUpdated", metrics);
            _logger.LogInformation($"Agent metrics sent to user {_currentUserService.UserId} for timeRange: {timeRange}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending agent metrics update");
        }
    }
}
