using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Shared.Application.Common.Interfaces;
using Shared.Domain.Entities;
using Shared.Domain.Enums;
using ChatRuntimeService.Services;

namespace ChatRuntimeService.Hubs;

//[Authorize]
public class ChatHub : Hub
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ITenantService _tenantService;
    private readonly ILogger<ChatHub> _logger;
    private readonly IAnalyticsIntegrationService _analyticsIntegrationService;

    public ChatHub(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        ITenantService tenantService,
        ILogger<ChatHub> logger,
        IAnalyticsIntegrationService analyticsIntegrationService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _tenantService = tenantService;
        _logger = logger;
        _analyticsIntegrationService = analyticsIntegrationService;
    }

    public async Task JoinConversation(string conversationId)
    {
        try
        {
            _logger.LogInformation($"JoinConversation called with ID: {conversationId}, User: {_currentUserService.UserId}, Tenant: {_tenantService.GetCurrentTenantId()}");
            
            if (Guid.TryParse(conversationId, out var convId))
            {
                var conversation = await _context.Conversations
                    .FirstOrDefaultAsync(c => c.Id == convId && c.TenantId == _tenantService.GetCurrentTenantId());

                if (conversation != null)
                {
                    await Groups.AddToGroupAsync(Context.ConnectionId, $"conversation_{conversationId}");
                    _logger.LogInformation($"User {_currentUserService.UserId} successfully joined conversation {conversationId}");
                }
                else
                {
                    _logger.LogWarning($"Conversation {conversationId} not found for tenant {_tenantService.GetCurrentTenantId()}");
                }
            }
            else
            {
                _logger.LogWarning($"Invalid conversation ID format: {conversationId}");
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
            var analyticsData = await _analyticsIntegrationService.GetTenantAnalyticsAsync(tenantId, startDate, endDate);
            var conversationMetrics = await _analyticsIntegrationService.GetConversationMetricsAsync("30d", tenantId);
            var agentMetrics = await _analyticsIntegrationService.GetAgentMetricsAsync("30d", tenantId);
            var realtimeData = await _analyticsIntegrationService.GetRealtimeAnalyticsAsync();

            var analytics = new
            {
                conversations = new { 
                    total = conversationMetrics.Total, 
                    active = conversationMetrics.Active, 
                    resolved = conversationMetrics.Completed, 
                    averageDuration = 15.2, 
                    satisfactionScore = conversationMetrics.AverageRating 
                },
                agents = new { 
                    online = agentMetrics.ActiveAgents, 
                    busy = agentMetrics.TotalAgents - agentMetrics.ActiveAgents, 
                    averageResponseTime = agentMetrics.AverageResponseTime, 
                    utilization = 75 
                },
                customers = new { total = 892, returning = 401, newToday = 12, averageRating = 4.5 },
                performance = new { resolutionRate = 92, firstResponseTime = 2.1, escalationRate = 8, botAccuracy = 87 },
                trends = analyticsData.Data.ContainsKey("trends") ? analyticsData.Data["trends"] : new
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
                    activeConversations = realtimeData.OngoingConversations,
                    waitingCustomers = 5,
                    onlineAgents = realtimeData.AvailableAgents,
                    currentLoad = (int)(realtimeData.SystemLoad * 100)
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
            var realtimeData = await _analyticsIntegrationService.GetRealtimeAnalyticsAsync();
            
            var realtime = new
            {
                activeConversations = realtimeData.OngoingConversations,
                waitingCustomers = 5,
                onlineAgents = realtimeData.AvailableAgents,
                currentLoad = (int)(realtimeData.SystemLoad * 100)
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
            var dashboardStats = await _analyticsIntegrationService.GetDashboardStatsAsync();
            
            var stats = new
            {
                totalTenants = 42,
                totalUsers = dashboardStats.TotalUsers,
                totalConversations = dashboardStats.TotalConversations,
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
            var conversationMetrics = await _analyticsIntegrationService.GetConversationMetricsAsync(timeRange, tenantId);
            
            var metrics = new
            {
                totalConversations = conversationMetrics.Total,
                averageDuration = 15.2,
                resolutionRate = conversationMetrics.Total > 0 ? (conversationMetrics.Completed * 100 / conversationMetrics.Total) : 0,
                satisfactionScore = conversationMetrics.AverageRating,
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
            var agentMetrics = await _analyticsIntegrationService.GetAgentMetricsAsync(timeRange, tenantId);
            
            var metrics = new
            {
                totalAgents = agentMetrics.TotalAgents,
                activeAgents = agentMetrics.ActiveAgents,
                averageResponseTime = agentMetrics.AverageResponseTime,
                averageRating = agentMetrics.AverageRating,
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

    public async Task JoinAgentGroup(string agentId)
    {
        try
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"agent_{agentId}");
            _logger.LogInformation($"Agent {agentId} joined agent group");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error joining agent group for {AgentId}", agentId);
        }
    }

    public async Task LeaveAgentGroup(string agentId)
    {
        try
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"agent_{agentId}");
            _logger.LogInformation($"Agent {agentId} left agent group");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error leaving agent group for {AgentId}", agentId);
        }
    }

    public async Task NotifyAgentAssignment(string conversationId, string agentId)
    {
        try
        {
            await Clients.Group($"agent_{agentId}")
                .SendAsync("ConversationAssigned", new { ConversationId = conversationId });
            _logger.LogInformation($"Notified agent {agentId} of conversation assignment {conversationId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error notifying agent assignment");
        }
    }
}
