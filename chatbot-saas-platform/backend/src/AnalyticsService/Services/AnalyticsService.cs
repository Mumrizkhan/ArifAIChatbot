using Microsoft.EntityFrameworkCore;
using Shared.Application.Common.Interfaces;
using AnalyticsService.Models;

namespace AnalyticsService.Services;

public class AnalyticsService : IAnalyticsService
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<AnalyticsService> _logger;

    public AnalyticsService(IApplicationDbContext context, ILogger<AnalyticsService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ConversationsSummaryDto> GetConversationsSummaryAsync()
    {
        var conversations = await _context.Conversations.ToListAsync();
        
        return new ConversationsSummaryDto
        {
            TotalConversations = conversations.Count,
            ActiveConversations = conversations.Count(c => c.Status == Shared.Domain.Enums.ConversationStatus.Active),
            CompletedConversations = conversations.Count(c => c.Status == Shared.Domain.Enums.ConversationStatus.Completed),
            AverageResponseTime = conversations.Any() ? conversations.Average(c => c.AverageResponseTime ?? 0) : 0
        };
    }

    public async Task<MessagesSummaryDto> GetMessagesSummaryAsync()
    {
        var messages = await _context.Messages.ToListAsync();
        
        return new MessagesSummaryDto
        {
            TotalMessages = messages.Count,
            BotMessages = messages.Count(m => m.SenderType == "Bot"),
            UserMessages = messages.Count(m => m.SenderType == "User"),
            AgentMessages = messages.Count(m => m.SenderType == "Agent")
        };
    }

    public async Task<TenantsSummaryDto> GetTenantsSummaryAsync()
    {
        var tenants = await _context.Tenants.ToListAsync();
        var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
        
        return new TenantsSummaryDto
        {
            TotalTenants = tenants.Count,
            ActiveTenants = tenants.Count(t => t.IsActive),
            NewTenants = tenants.Count(t => t.CreatedAt >= thirtyDaysAgo)
        };
    }

    public async Task<RealtimeAnalyticsDto> GetRealtimeAnalyticsAsync()
    {
        var activeConversations = await _context.Conversations
            .CountAsync(c => c.Status == Shared.Domain.Enums.ConversationStatus.Active);
        
        var availableAgents = await _context.Users
            .CountAsync(u => u.Role == Shared.Domain.Enums.UserRole.Agent && (u.IsOnline || u.Status== Shared.Domain.Enums.UserStatus.Online));

        return new RealtimeAnalyticsDto
        {
            ActiveUsers = activeConversations,
            OngoingConversations = activeConversations,
            AvailableAgents = availableAgents,
            SystemLoad = 0.75
        };
    }

    public async Task<AnalyticsDto> GetAnalyticsAsync(AnalyticsRequest request)
    {
        var data = new Dictionary<string, object>();
        
        if (!string.IsNullOrEmpty(request.MetricType))
        {
            switch (request.MetricType.ToLower())
            {
                case "conversations":
                    data["conversations"] = await GetConversationsSummaryAsync();
                    break;
                case "messages":
                    data["messages"] = await GetMessagesSummaryAsync();
                    break;
                case "tenants":
                    data["tenants"] = await GetTenantsSummaryAsync();
                    break;
            }
        }

        return new AnalyticsDto
        {
            Data = data,
            TimeRange = $"{request.DateFrom} - {request.DateTo}",
            GeneratedAt = DateTime.UtcNow
        };
    }

    public async Task<byte[]> ExportAnalyticsAsync(ExportAnalyticsRequest request)
    {
        var analytics = await GetAnalyticsAsync(new AnalyticsRequest
        {
            DateFrom = request.DateFrom,
            DateTo = request.DateTo,
            TenantId = request.TenantId
        });

        var csvContent = "Metric,Value\n";
        foreach (var item in analytics.Data)
        {
            csvContent += $"{item.Key},{item.Value}\n";
        }

        return System.Text.Encoding.UTF8.GetBytes(csvContent);
    }

    public async Task<DashboardStatsDto> GetDashboardStatsAsync()
    {
        try
        {
            var totalUsers = await _context.Users.CountAsync();
            var totalConversations = await _context.Conversations.CountAsync();
            var totalMessages = await _context.Messages.CountAsync();
            //var avgResponseTime = await _context.Conversations
            //    .Where(c => c.AverageResponseTime.HasValue)
            //    .Select(c => c.AverageResponseTime.Value)
            //    .DefaultIfEmpty(0) // Provide a default value for empty sequences
            //    .AverageAsync();
            var avgResponseTime = await _context.Conversations
           .Where(c => c.AverageResponseTime.HasValue)
           .Select(c => c.AverageResponseTime.Value)
           .ToListAsync();            

            return new DashboardStatsDto
            {
                TotalUsers = totalUsers,
                TotalConversations = totalConversations,
                TotalMessages = totalMessages,
                AverageResponseTime = avgResponseTime.Any() ? avgResponseTime.Average() : 0
            };
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task<ConversationMetricsDto> GetConversationMetricsAsync(string timeRange, string? tenantId)
    {
        var query = _context.Conversations.AsQueryable();
        
        if (!string.IsNullOrEmpty(tenantId) && Guid.TryParse(tenantId, out var tid))
        {
            query = query.Where(c => c.TenantId == tid);
        }

        var conversations = await query.ToListAsync();
        
        return new ConversationMetricsDto
        {
            Total = conversations.Count,
            Completed = conversations.Count(c => c.Status == Shared.Domain.Enums.ConversationStatus.Completed),
            Active = conversations.Count(c => c.Status == Shared.Domain.Enums.ConversationStatus.Active),
            AverageRating = conversations.Where(c => c.Rating.HasValue).Any() 
                ? conversations.Where(c => c.Rating.HasValue).Average(c => c.Rating.Value) 
                : 0
        };
    }

    public async Task<AgentMetricsDto> GetAgentMetricsAsync(string timeRange, string? tenantId)
    {
        var query = _context.Users.Where(u => u.Role == Shared.Domain.Enums.UserRole.Agent);
        
        if (!string.IsNullOrEmpty(tenantId) && Guid.TryParse(tenantId, out var tid))
        {
            query = query.Where(u => u.TenantId == tid);
        }

        var agents = await query.ToListAsync();
        
        return new AgentMetricsDto
        {
            TotalAgents = agents.Count,
            ActiveAgents = agents.Count(a => (a.IsOnline|| a.Status== Shared.Domain.Enums.UserStatus.Online)),
            AverageResponseTime = 120,
            AverageRating = 4.5
        };
    }

    public async Task<BotMetricsDto> GetBotMetricsAsync(string timeRange, string? tenantId)
    {
        var query = _context.Messages.Where(m => m.SenderType == "Bot");
        
        if (!string.IsNullOrEmpty(tenantId) && Guid.TryParse(tenantId, out var tid))
        {
            query = query.Where(m => m.Conversation.TenantId == tid);
        }

        var botMessages = await query.CountAsync();
        
        return new BotMetricsDto
        {
            TotalInteractions = botMessages,
            SuccessRate = 0.85,
            AverageResponseTime = 2.5
        };
    }

    public async Task<OverviewMetricsDto> GetOverviewMetricsAsync(string? dateFrom, string? dateTo, string granularity)
    {
        var startDate = DateTime.TryParse(dateFrom, out var start) ? start : DateTime.UtcNow.AddDays(-30);
        var endDate = DateTime.TryParse(dateTo, out var end) ? end : DateTime.UtcNow;

        var conversations = await _context.Conversations
            .Where(c => c.CreatedAt >= startDate && c.CreatedAt <= endDate)
            .ToListAsync();

        var messages = await _context.Messages
            .Where(m => m.CreatedAt >= startDate && m.CreatedAt <= endDate)
            .ToListAsync();

        var conversationsByDate = conversations
            .GroupBy(c => c.CreatedAt.Date)
            .ToDictionary(g => g.Key.ToString("yyyy-MM-dd"), g => g.Count());

        var messagesByDate = messages
            .GroupBy(m => m.CreatedAt.Date)
            .ToDictionary(g => g.Key.ToString("yyyy-MM-dd"), g => g.Count());

        return new OverviewMetricsDto
        {
            ConversationsByDate = conversationsByDate,
            MessagesByDate = messagesByDate
        };
    }

    public async Task<UserMetricsDto> GetUserMetricsAsync(string? dateFrom, string? dateTo, string granularity)
    {
        var startDate = DateTime.TryParse(dateFrom, out var start) ? start : DateTime.UtcNow.AddDays(-30);
        var endDate = DateTime.TryParse(dateTo, out var end) ? end : DateTime.UtcNow;

        var users = await _context.Users.ToListAsync();
        var activeUsers = await _context.Users
            .Where(u => u.LastLoginAt >= startDate)
            .CountAsync();

        return new UserMetricsDto
        {
            TotalUsers = users.Count,
            ActiveUsers = activeUsers,
            NewUsers = users.Count(u => u.CreatedAt >= startDate && u.CreatedAt <= endDate)
        };
    }

    public async Task<ChatbotMetricsDto> GetChatbotMetricsAsync(string? dateFrom, string? dateTo, string granularity)
    {
        var chatbots = await _context.ChatbotConfigs.ToListAsync();
        
        return new ChatbotMetricsDto
        {
            TotalChatbots = chatbots.Count,
            ActiveChatbots = chatbots.Count(c => c.IsActive),
            AverageUptime = 0.99
        };
    }

    public async Task<SubscriptionMetricsDto> GetSubscriptionMetricsAsync(string? dateFrom, string? dateTo, string granularity)
    {
        var subscriptions = await _context.Subscriptions.ToListAsync();
        
        return new SubscriptionMetricsDto
        {
            TotalSubscriptions = subscriptions.Count,
            ActiveSubscriptions = subscriptions.Count(s => s.Status == Shared.Domain.Entities.SubscriptionStatus.Active),
            TotalRevenue = subscriptions.Where(s => s.Status ==  Shared.Domain.Entities.SubscriptionStatus.Active).Sum(s => s.Amount)
        };
    }

    public async Task<PerformanceMetricsDto> GetPerformanceMetricsAsync(string? dateFrom, string? dateTo)
    {
        var avgResponseTime = await _context.Conversations
            .Where(c => c.AverageResponseTime.HasValue)
            .Select(c => c.AverageResponseTime.Value)
            .DefaultIfEmpty(0) // Provide a default value for empty sequences
            .AverageAsync();

        return new PerformanceMetricsDto
        {
            AverageResponseTime = avgResponseTime,
            SystemUptime = 0.999,
            ErrorRate = 1
        };
    }

    public async Task<List<ReportDto>> GetReportsAsync()
    {
        return new List<ReportDto>();
    }

    public async Task<ReportDto> GetReportAsync(Guid id)
    {
        return new ReportDto { Id = id, Name = "Sample Report" };
    }

    public async Task<ReportDto> CreateReportAsync(CreateReportRequest request)
    {
        return new ReportDto 
        { 
            Id = Guid.NewGuid(), 
            Name = request.Name,
            Description = request.Description,
            Type = request.Type,
            CreatedAt = DateTime.UtcNow
        };
    }

    public async Task<bool> UpdateReportAsync(Guid id, UpdateReportRequest request)
    {
        return true;
    }

    public async Task<bool> DeleteReportAsync(Guid id)
    {
        return true;
    }

    public async Task<ReportExecutionDto> RunReportAsync(Guid id)
    {
        return new ReportExecutionDto
        {
            Id = Guid.NewGuid(),
            ReportId = id,
            Status = "Running",
            StartedAt = DateTime.UtcNow
        };
    }

    public async Task<CompareAnalyticsDto> CompareAnalyticsAsync(CompareAnalyticsRequest request)
    {
        return new CompareAnalyticsDto
        {
            Current = new { value = 100 },
            Previous = new { value = 85 },
            Change = 15,
            ChangePercent = 17.6
        };
    }

    public async Task<List<GoalDto>> GetGoalsAsync()
    {
        return new List<GoalDto>
        {
            new GoalDto 
            { 
                Id = Guid.NewGuid(), 
                Name = "Increase conversions", 
                Target = 1000, 
                Progress = 750, 
                Deadline = DateTime.Parse("2024-12-31"),
                CreatedAt = DateTime.UtcNow
            }
        };
    }

    public async Task<GoalDto> CreateGoalAsync(CreateGoalRequest request)
    {
        return new GoalDto
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Target = request.Target,
            Metric = request.Metric,
            Deadline = DateTime.Parse(request.Deadline),
            CreatedAt = DateTime.UtcNow
        };
    }

    public async Task<bool> UpdateGoalAsync(Guid id, UpdateGoalRequest request)
    {
        return true;
    }

    public async Task<bool> DeleteGoalAsync(Guid id)
    {
        return true;
    }

    public async Task<CustomReportDto> GetCustomReportAsync(CustomReportRequest request)
    {
        var query = _context.Conversations
            .Where(c => c.CreatedAt >= request.StartDate && c.CreatedAt <= request.EndDate);

        if (!string.IsNullOrEmpty(request.TenantId) && Guid.TryParse(request.TenantId, out var parsedTenantId))
        {
            query = query.Where(c => c.TenantId == parsedTenantId);
        }

        var conversations = await query.Include(c => c.Messages).ToListAsync();
        
        return new CustomReportDto
        {
            TotalConversations = conversations.Count,
            TotalMessages = conversations.Sum(c => c.Messages.Count),
            AverageMessagesPerConversation = conversations.Any() ? conversations.Average(c => c.Messages.Count) : 0,
            ResolvedConversations = conversations.Count(c => c.Status == Shared.Domain.Enums.ConversationStatus.Resolved),
            PeriodStart = request.StartDate,
            PeriodEnd = request.EndDate,
            GeneratedAt = DateTime.UtcNow
        };
    }

    private DateTime GetStartDateFromTimeRange(string timeRange)
    {
        return timeRange switch
        {
            "1d" => DateTime.UtcNow.AddDays(-1),
            "7d" => DateTime.UtcNow.AddDays(-7),
            "30d" => DateTime.UtcNow.AddDays(-30),
            "90d" => DateTime.UtcNow.AddDays(-90),
            _ => DateTime.UtcNow.AddDays(-7)
        };
    }
}
