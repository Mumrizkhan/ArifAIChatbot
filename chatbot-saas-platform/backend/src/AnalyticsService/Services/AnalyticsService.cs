using AnalyticsService.Models;
using Microsoft.EntityFrameworkCore;
using Shared.Application.Common.Interfaces;
using Shared.Domain.Entities;
using Shared.Infrastructure.Persistence;
using System.Text.Json;

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
     public async Task<LiveAgentAnalyticsSummary> GetLiveAgentSummaryAsync(string tenantId, DateTime from, DateTime to)
    {
        try
        {
            var events = await _context.AnalyticsEvents
                .Where(e => e.TenantId == tenantId && e.Timestamp >= from && e.Timestamp <= to)
                .ToListAsync();

            var agentRequestEvents = events.Where(e => e.EventType == "live_agent_agentRequested").ToList();
            var agentAssignmentEvents = events.Where(e => e.EventType == "live_agent_agentAssigned").ToList();
            var feedbackEvents = events.Where(e => e.EventType == "live_agent_feedbackSubmitted").ToList();
            var conversationEndEvents = events.Where(e => e.EventType == "live_agent_conversationEnded").ToList();
            var responseTimeEvents = events.Where(e => e.EventType == "live_agent_responseTime").ToList();

            // Calculate wait times
            var waitTimes = new List<double>();
            foreach (var assignment in agentAssignmentEvents)
            {
                var eventData = assignment.EventData;
                if (eventData?.TryGetValue("waitTime", out var waitTimeObj) == true)
                {
                    if (double.TryParse(waitTimeObj.ToString(), out var waitTime))
                    {
                        waitTimes.Add(waitTime);
                    }
                }
            }

            // Calculate response times
            var responseTimes = new List<double>();
            foreach (var response in responseTimeEvents)
            {
                var eventData = response.EventData;
                if (eventData?.TryGetValue("responseTimeMs", out var responseTimeObj) == true)
                {
                    if (double.TryParse(responseTimeObj.ToString(), out var responseTime))
                    {
                        responseTimes.Add(responseTime);
                    }
                }
            }

            // Calculate satisfaction metrics
            var ratings = new List<double>();
            var satisfactionCounts = new Dictionary<string, int> { ["satisfied"] = 0, ["neutral"] = 0, ["unsatisfied"] = 0 };

            foreach (var feedback in feedbackEvents)
            {
                var eventData = feedback.EventData;

                if (eventData?.TryGetValue("rating", out var ratingObj) == true)
                {
                    if (double.TryParse(ratingObj.ToString(), out var rating))
                    {
                        ratings.Add(rating);
                    }
                }

                if (eventData?.TryGetValue("satisfaction", out var satisfactionObj) == true)
                {
                    var satisfaction = satisfactionObj.ToString();
                    if (satisfactionCounts.ContainsKey(satisfaction))
                    {
                        satisfactionCounts[satisfaction]++;
                    }
                }
            }

            // Calculate conversation durations
            var conversationDurations = new List<double>();
            foreach (var endEvent in conversationEndEvents)
            {
                var eventData = endEvent.EventData ;
                if (eventData?.TryGetValue("duration", out var durationObj) == true)
                {
                    if (double.TryParse(durationObj.ToString(), out var duration))
                    {
                        conversationDurations.Add(duration);
                    }
                }
            }

            // Generate hourly trends
            var hourlyTrends = await GetHourlyTrendsAsync(tenantId, from, to);

            // Calculate agent performance distribution
            var agentDistribution = await CalculateAgentPerformanceDistribution(tenantId, from, to);

            return new LiveAgentAnalyticsSummary
            {
                TenantId = tenantId,
                DateRange = new DateRange { From = from, To = to },

                TotalAgentRequests = agentRequestEvents.Count,
                SuccessfulAssignments = agentAssignmentEvents.Count,
                AverageWaitTime = waitTimes.Any() ? waitTimes.Average() : 0,
                MedianWaitTime = waitTimes.Any() ? CalculateMedian(waitTimes) : 0,

                AverageFirstResponseTime = responseTimes.Any() ? responseTimes.Average() : 0,
                AverageResponseTime = responseTimes.Any() ? responseTimes.Average() : 0,
                MedianResponseTime = responseTimes.Any() ? CalculateMedian(responseTimes) : 0,

                CompletedConversations = conversationEndEvents.Count,
                EscalatedConversations = events.Count(e => e.EventType == "live_agent_conversationEscalated"),
                TransferredConversations = events.Count(e => e.EventType == "live_agent_conversationTransferred"),
                AverageConversationDuration = conversationDurations.Any() ? conversationDurations.Average() : 0,

                TotalFeedback = feedbackEvents.Count,
                AverageRating = ratings.Any() ? ratings.Average() : 0,
                SatisfiedCustomers = satisfactionCounts["satisfied"],
                NeutralCustomers = satisfactionCounts["neutral"],
                UnsatisfiedCustomers = satisfactionCounts["unsatisfied"],

                AgentDistribution = agentDistribution,
                HourlyTrends = hourlyTrends
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate live agent analytics summary for tenant {TenantId}", tenantId);
            throw;
        }
    }

    public async Task<List<AgentWorkload>> GetAgentWorkloadsAsync(string tenantId)
    {
        try
        {
            var now = DateTime.UtcNow;
            var today = now.Date;

            // Get recent agent activity
            var recentEvents = await _context.AnalyticsEvents
                .Where(e => e.TenantId == tenantId &&
                           !string.IsNullOrEmpty(e.AgentId) &&
                           e.Timestamp >= today)
                .GroupBy(e => e.AgentId)
                .ToListAsync();

            var workloads = new List<AgentWorkload>();

            foreach (var agentGroup in recentEvents)
            {
                var agentId = agentGroup.Key;
                var events = agentGroup.ToList();

                // Calculate current active conversations
                var activeConversations = await GetActiveConversationsForAgent(agentId, tenantId);

                // Calculate today's metrics
                var conversationsToday = events.Count(e => e.EventType == "live_agent_agentAssigned");
                var messagesToday = events.Count(e => e.EventType == "live_agent_messageSent");

                // Calculate average response time and satisfaction
                var avgResponseTime = await CalculateAgentAverageResponseTime(agentId, tenantId, today, now);
                var avgSatisfaction = await CalculateAgentAverageSatisfaction(agentId, tenantId, today, now);

                var workload = new AgentWorkload
                {
                    AgentId = agentId,
                    CurrentConversations = activeConversations,
                    MaxConcurrentConversations = 5, // This should come from agent settings
                    Status = await GetAgentStatus(agentId, tenantId),
                    LastActivity = events.Max(e => e.Timestamp),
                    AverageResponseTime = avgResponseTime,
                    CustomerSatisfaction = avgSatisfaction,
                    ConversationsToday = conversationsToday,
                    MessagesToday = messagesToday
                };

                workloads.Add(workload);
            }

            return workloads.OrderByDescending(w => w.WorkloadPercentage).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get agent workloads for tenant {TenantId}", tenantId);
            throw;
        }
    }

    public async Task<List<HourlyMetric>> GetHourlyTrendsAsync(string tenantId, DateTime from, DateTime to)
    {
        try
        {
            var events = await _context.AnalyticsEvents
                .Where(e => e.TenantId == tenantId && e.Timestamp >= from && e.Timestamp <= to)
                .ToListAsync();

            var hourlyMetrics = new List<HourlyMetric>();
            var currentHour = from.Date.AddHours(from.Hour);

            while (currentHour <= to)
            {
                var nextHour = currentHour.AddHours(1);
                var hourEvents = events.Where(e => e.Timestamp >= currentHour && e.Timestamp < nextHour).ToList();

                var agentRequests = hourEvents.Count(e => e.EventType == "live_agent_agentRequested");
                var assignments = hourEvents.Count(e => e.EventType == "live_agent_agentAssigned");

                // Calculate average wait time for this hour
                var waitTimes = new List<double>();
                foreach (var assignment in hourEvents.Where(e => e.EventType == "live_agent_agentAssigned"))
                {
                    var eventData = assignment.EventData ;
                    if (eventData?.TryGetValue("waitTime", out var waitTimeObj) == true)
                    {
                        if (double.TryParse(waitTimeObj.ToString(), out var waitTime))
                        {
                            waitTimes.Add(waitTime);
                        }
                    }
                }

                // Calculate average response time for this hour
                var responseTimes = new List<double>();
                foreach (var response in hourEvents.Where(e => e.EventType == "live_agent_responseTime"))
                {
                    var eventData = response.EventData;
                    if (eventData?.TryGetValue("responseTimeMs", out var responseTimeObj) == true)
                    {
                        if (double.TryParse(responseTimeObj.ToString(), out var responseTime))
                        {
                            responseTimes.Add(responseTime);
                        }
                    }
                }

                // Calculate average rating for this hour
                var ratings = new List<double>();
                foreach (var feedback in hourEvents.Where(e => e.EventType == "live_agent_feedbackSubmitted"))
                {
                    var eventData = feedback.EventData ;
                    if (eventData?.TryGetValue("rating", out var ratingObj) == true)
                    {
                        if (double.TryParse(ratingObj.ToString(), out var rating))
                        {
                            ratings.Add(rating);
                        }
                    }
                }

                var metric = new HourlyMetric
                {
                    Hour = currentHour,
                    AgentRequests = agentRequests,
                    Assignments = assignments,
                    AverageWaitTime = waitTimes.Any() ? waitTimes.Average() : 0,
                    AverageResponseTime = responseTimes.Any() ? responseTimes.Average() : 0,
                    AverageRating = ratings.Any() ? ratings.Average() : 0,
                    ActiveAgents = hourEvents.Where(e => !string.IsNullOrEmpty(e.AgentId)).Select(e => e.AgentId).Distinct().Count()
                };

                hourlyMetrics.Add(metric);
                currentHour = nextHour;
            }

            return hourlyMetrics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get hourly trends for tenant {TenantId}", tenantId);
            throw;
        }
    }

    public async Task ProcessAnalyticsEventAsync(AnalyticsEvent analyticsEvent)
    {
        try
        {
            // Additional processing for specific event types
            switch (analyticsEvent.EventType)
            {
                case "live_agent_agentAssigned":
                    await ProcessAgentAssignmentEvent(analyticsEvent);
                    break;
                case "live_agent_feedbackSubmitted":
                    await ProcessFeedbackEvent(analyticsEvent);
                    break;
                case "live_agent_conversationEnded":
                    await ProcessConversationEndEvent(analyticsEvent);
                    break;
            }

            _logger.LogInformation("Processed analytics event {EventType} for tenant {TenantId}",
                analyticsEvent.EventType, analyticsEvent.TenantId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process analytics event {EventType} for tenant {TenantId}",
                analyticsEvent.EventType, analyticsEvent.TenantId);
        }
    }

    public async Task<Dictionary<string, object>> GetRealtimeMetricsAsync(string tenantId)
    {
        try
        {
            var now = DateTime.UtcNow;
            var last5Minutes = now.AddMinutes(-5);
            var lastHour = now.AddHours(-1);

            var metrics = new Dictionary<string, object>
            {
                ["timestamp"] = now,
                ["activeConversations"] = await GetActiveConversationsCount(tenantId),
                ["onlineAgents"] = await GetOnlineAgentsCount(tenantId),
                ["queuedRequests"] = await GetQueuedRequestsCount(tenantId),
                ["avgResponseTimeLast5Min"] = await GetAverageResponseTime(tenantId, last5Minutes, now),
                ["messagesLastHour"] = await GetMessageCount(tenantId, lastHour, now),
                ["satisfactionLastHour"] = await GetAverageSatisfaction(tenantId, lastHour, now)
            };

            return metrics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get realtime metrics for tenant {TenantId}", tenantId);
            throw;
        }
    }

    public async Task<bool> ValidateAnalyticsEventAsync(AnalyticsEventDto eventDto)
    {
        try
        {
            // Basic validation
            if (string.IsNullOrEmpty(eventDto.EventType) ||
                string.IsNullOrEmpty(eventDto.SessionId) ||
                string.IsNullOrEmpty(eventDto.TenantId))
            {
                return false;
            }

            // Event type validation
            var validEventTypes = new[]
            {
                "live_agent_agentRequested",
                "live_agent_agentAssigned",
                "live_agent_agentJoined",
                "live_agent_agentLeft",
                "live_agent_conversationEnded",
                "live_agent_conversationEscalated",
                "live_agent_conversationTransferred",
                "live_agent_messageReceived",
                "live_agent_messageSent",
                "live_agent_feedbackSubmitted",
                "live_agent_responseTime"
            };

            if (!validEventTypes.Contains(eventDto.EventType))
            {
                _logger.LogWarning("Invalid event type: {EventType}", eventDto.EventType);
                return false;
            }

            // Specific validation for live agent events
            if (eventDto.EventType.StartsWith("live_agent_") &&
                (string.IsNullOrEmpty(eventDto.ConversationId)))
            {
                _logger.LogWarning("Live agent event missing conversation ID: {EventType}", eventDto.EventType);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate analytics event");
            return false;
        }
    }

    #region Private Helper Methods

    private double CalculateMedian(List<double> values)
    {
        if (!values.Any()) return 0;

        var sorted = values.OrderBy(x => x).ToList();
        var mid = sorted.Count / 2;

        return sorted.Count % 2 == 0
            ? (sorted[mid - 1] + sorted[mid]) / 2.0
            : sorted[mid];
    }

    private async Task<List<AgentPerformanceDistribution>> CalculateAgentPerformanceDistribution(string tenantId, DateTime from, DateTime to)
    {
        // Implementation for calculating agent performance distribution
        var distribution = new List<AgentPerformanceDistribution>
        {
            new() { PerformanceRange = "4.5-5.0", AgentCount = 0, Percentage = 0 },
            new() { PerformanceRange = "4.0-4.5", AgentCount = 0, Percentage = 0 },
            new() { PerformanceRange = "3.5-4.0", AgentCount = 0, Percentage = 0 },
            new() { PerformanceRange = "3.0-3.5", AgentCount = 0, Percentage = 0 },
            new() { PerformanceRange = "Below 3.0", AgentCount = 0, Percentage = 0 }
        };

        // Placeholder implementation - would need actual agent rating calculations
        return distribution;
    }

    private async Task ProcessAgentAssignmentEvent(AnalyticsEvent analyticsEvent)
    {
        // Additional processing for agent assignment events
        // Could trigger notifications, update agent workload, etc.
        await Task.CompletedTask;
    }

    private async Task ProcessFeedbackEvent(AnalyticsEvent analyticsEvent)
    {
        // Additional processing for feedback events
        // Could trigger alerts for low ratings, update agent performance metrics, etc.
        await Task.CompletedTask;
    }

    private async Task ProcessConversationEndEvent(AnalyticsEvent analyticsEvent)
    {
        // Additional processing for conversation end events
        // Could update conversation summaries, agent availability, etc.
        await Task.CompletedTask;
    }

    // Placeholder implementations for various metric calculations
    private async Task<int> GetActiveConversationsForAgent(string agentId, string tenantId) => 0;
    private async Task<string> GetAgentStatus(string agentId, string tenantId) => "online";
    private async Task<double> CalculateAgentAverageResponseTime(string agentId, string tenantId, DateTime from, DateTime to) => 0;
    private async Task<double> CalculateAgentAverageSatisfaction(string agentId, string tenantId, DateTime from, DateTime to) => 0;
    private async Task<int> GetActiveConversationsCount(string tenantId) => 0;
    private async Task<int> GetOnlineAgentsCount(string tenantId) => 0;
    private async Task<int> GetQueuedRequestsCount(string tenantId) => 0;
    private async Task<double> GetAverageResponseTime(string tenantId, DateTime from, DateTime to) => 0;
    private async Task<int> GetMessageCount(string tenantId, DateTime from, DateTime to) => 0;
    private async Task<double> GetAverageSatisfaction(string tenantId, DateTime from, DateTime to) => 0;

    #endregion
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

    // Implementation of missing methods moved from controller
    public async Task<bool> TrackEventsBatchAsync(AnalyticsEventBatchRequest request, string tenantId)
    {
        try
        {
            var analyticsEvents = new List<AnalyticsEvent>();

            foreach (var eventDto in request.Events)
            {
                var analyticsEvent = new AnalyticsEvent
                {
                    Id = Guid.NewGuid(),
                    EventType = eventDto.EventType,
                    EventData = eventDto.EventData,
                    Timestamp = eventDto.Timestamp,
                    SessionId = eventDto.SessionId,
                    ConversationId = eventDto.ConversationId,
                    AgentId = eventDto.AgentId,
                    UserId = eventDto.UserId,
                    TenantId = tenantId,
                    Metadata = eventDto.Metadata,
                    CreatedAt = DateTime.UtcNow
                };

                analyticsEvents.Add(analyticsEvent);
            }

            await _context.AnalyticsEvents.AddRangeAsync(analyticsEvents);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Tracked {Count} analytics events for tenant {TenantId}",
                analyticsEvents.Count, tenantId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to track analytics events");
            return false;
        }
    }

    public async Task<AnalyticsDashboard> GetDashboardDataAsync(string tenantId, DateTime? from, DateTime? to)
    {
        try
        {
            var fromDate = from ?? DateTime.UtcNow.AddDays(-30);
            var toDate = to ?? DateTime.UtcNow;

            var events = await _context.AnalyticsEvents
                .Where(e => e.TenantId == tenantId && e.Timestamp >= fromDate && e.Timestamp <= toDate)
                .ToListAsync();

            var dashboard = new AnalyticsDashboard
            {
                TenantId = tenantId,
                DateRange = new DateRange { From = fromDate, To = toDate },

                // Conversation metrics
                TotalConversations = await GetTotalConversations(tenantId, fromDate, toDate),
                ConversationsWithAgents = await GetConversationsWithAgents(tenantId, fromDate, toDate),
                AverageConversationDuration = await GetAverageConversationDuration(tenantId, fromDate, toDate),

                // Agent metrics
                TotalAgentRequests = events.Count(e => e.EventType == "live_agent_agentRequested"),
                AgentAssignments = events.Count(e => e.EventType == "live_agent_agentAssigned"),
                AverageWaitTime = await GetAverageWaitTime(tenantId, fromDate, toDate),
                AverageResponseTime = await GetAverageResponseTime(tenantId, fromDate, toDate),

                // Customer satisfaction
                FeedbackSubmissions = events.Count(e => e.EventType == "live_agent_feedbackSubmitted"),
                AverageRating = await GetAverageRating(tenantId, fromDate, toDate),
                SatisfactionBreakdown = await GetSatisfactionBreakdown(tenantId, fromDate, toDate),

                // Message metrics
                TotalMessages = events.Count(e => e.EventType == "live_agent_messageReceived" || e.EventType == "live_agent_messageSent"),
                MessagesByType = await GetMessagesByType(tenantId, fromDate, toDate),

                // Top performing agents
                TopAgents = await GetTopAgents(tenantId, fromDate, toDate)
            };

            return dashboard;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get dashboard data for tenant {TenantId}", tenantId);
            throw;
        }
    }

    public async Task<AgentMetrics> GetAgentMetricsDetailedAsync(string agentId, string tenantId, DateTime? from, DateTime? to)
    {
        try
        {
            var fromDate = from ?? DateTime.UtcNow.AddDays(-30);
            var toDate = to ?? DateTime.UtcNow;

            var events = await _context.AnalyticsEvents
                .Where(e => e.TenantId == tenantId &&
                           e.AgentId == agentId &&
                           e.Timestamp >= fromDate &&
                           e.Timestamp <= toDate)
                .ToListAsync();

            var metrics = new AgentMetrics
            {
                AgentId = agentId,
                DateRange = new DateRange { From = fromDate, To = toDate },

                // Conversation metrics
                TotalConversations = events.Count(e => e.EventType == "live_agent_agentAssigned"),
                ActiveConversations = await GetActiveConversations(agentId, tenantId),
                CompletedConversations = events.Count(e => e.EventType == "live_agent_conversationEnded"),

                // Time metrics
                AverageResponseTime = await GetAgentAverageResponseTime(agentId, tenantId, fromDate, toDate),
                AverageSessionDuration = await GetAgentAverageSessionDuration(agentId, tenantId, fromDate, toDate),
                TotalOnlineTime = await GetAgentTotalOnlineTime(agentId, tenantId, fromDate, toDate),

                // Message metrics
                MessagesSent = events.Count(e => e.EventType == "live_agent_messageSent"),
                MessagesReceived = events.Count(e => e.EventType == "live_agent_messageReceived"),

                // Customer satisfaction
                FeedbackReceived = events.Count(e => e.EventType == "live_agent_feedbackSubmitted"),
                AverageRating = await GetAgentAverageRating(agentId, tenantId, fromDate, toDate),
                SatisfactionDistribution = await GetAgentSatisfactionDistribution(agentId, tenantId, fromDate, toDate),

                // Performance trends
                DailyMetrics = await GetAgentDailyMetrics(agentId, tenantId, fromDate, toDate)
            };

            return metrics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get metrics for agent {AgentId}", agentId);
            throw;
        }
    }

    public async Task<ConversationAnalytics> GetConversationAnalyticsAsync(string conversationId, string tenantId)
    {
        try
        {
            var events = await _context.AnalyticsEvents
                .Where(e => e.TenantId == tenantId && e.ConversationId == conversationId)
                .OrderBy(e => e.Timestamp)
                .ToListAsync();

            var analytics = new ConversationAnalytics
            {
                ConversationId = conversationId,
                Events = events.Select(e => new AnalyticsEventDto
                {
                    EventType = e.EventType,
                    EventData = e.EventData ?? new Dictionary<string, object>(),
                    Timestamp = e.Timestamp,
                    AgentId = e.AgentId,
                    Metadata = e.Metadata
                }).ToList(),

                // Calculated metrics
                Duration = await GetConversationDuration(conversationId, tenantId),
                MessageCount = events.Count(e => e.EventType.Contains("message")),
                AgentCount = events.Where(e => !string.IsNullOrEmpty(e.AgentId)).Select(e => e.AgentId).Distinct().Count(),
                WaitTime = await GetConversationWaitTime(conversationId, tenantId),
                ResponseTimes = await GetConversationResponseTimes(conversationId, tenantId),
                CustomerSatisfaction = await GetConversationSatisfaction(conversationId, tenantId)
            };

            return analytics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get analytics for conversation {ConversationId}", conversationId);
            throw;
        }
    }

    public async Task<RealtimeAnalytics> GetRealtimeAnalyticsDetailedAsync(string tenantId)
    {
        try
        {
            var now = DateTime.UtcNow;
            var last24Hours = now.AddDays(-1);
            var lastHour = now.AddHours(-1);

            var realtime = new RealtimeAnalytics
            {
                TenantId = tenantId,
                Timestamp = now,

                // Current state
                ActiveConversations = await GetActiveConversationsCount(tenantId),
                OnlineAgents = await GetOnlineAgentsCount(tenantId),
                QueuedConversations = await GetQueuedConversationsCount(tenantId),

                // Last hour metrics
                ConversationsStartedLastHour = await GetConversationsStarted(tenantId, lastHour, now),
                MessagesLastHour = await GetMessagesCount(tenantId, lastHour, now),
                FeedbackLastHour = await GetFeedbackCount(tenantId, lastHour, now),

                // Last 24 hours metrics
                ConversationsStartedLast24Hours = await GetConversationsStarted(tenantId, last24Hours, now),
                AverageWaitTimeLast24Hours = await GetAverageWaitTime(tenantId, last24Hours, now),
                AverageResponseTimeLast24Hours = await GetAverageResponseTime(tenantId, last24Hours, now),
                CustomerSatisfactionLast24Hours = await GetAverageRating(tenantId, last24Hours, now)
            };

            return realtime;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get realtime analytics for tenant {TenantId}", tenantId);
            throw;
        }
    }

    #region Helper Methods Moved from Controller

    private async Task<int> GetTotalConversations(string tenantId, DateTime from, DateTime to)
    {
        return await _context.AnalyticsEvents
            .Where(e => e.TenantId == tenantId &&
                       e.EventType == "conversation_started" &&
                       e.Timestamp >= from &&
                       e.Timestamp <= to)
            .CountAsync();
    }

    private async Task<int> GetConversationsWithAgents(string tenantId, DateTime from, DateTime to)
    {
        return await _context.AnalyticsEvents
            .Where(e => e.TenantId == tenantId &&
                       e.EventType == "live_agent_agentAssigned" &&
                       e.Timestamp >= from &&
                       e.Timestamp <= to)
            .Select(e => e.ConversationId)
            .Distinct()
            .CountAsync();
    }

    private async Task<double> GetAverageConversationDuration(string tenantId, DateTime from, DateTime to)
    {
        var endedConversations = await _context.AnalyticsEvents
            .Where(e => e.TenantId == tenantId &&
                       e.EventType == "live_agent_conversationEnded" &&
                       e.Timestamp >= from &&
                       e.Timestamp <= to)
            .ToListAsync();

        if (!endedConversations.Any()) return 0;

        var durations = new List<double>();
        foreach (var event_ in endedConversations)
        {
            var eventData = event_.EventData;
            if (eventData?.TryGetValue("duration", out var durationObj) == true)
            {
                if (double.TryParse(durationObj.ToString(), out var duration))
                {
                    durations.Add(duration);
                }
            }
        }

        return durations.Any() ? durations.Average() : 0;
    }

    private async Task<double> GetAverageWaitTime(string tenantId, DateTime from, DateTime to)
    {
        var assignmentEvents = await _context.AnalyticsEvents
            .Where(e => e.TenantId == tenantId &&
                       e.EventType == "live_agent_agentAssigned" &&
                       e.Timestamp >= from &&
                       e.Timestamp <= to)
            .ToListAsync();

        if (!assignmentEvents.Any()) return 0;

        var waitTimes = new List<double>();
        foreach (var event_ in assignmentEvents)
        {
            var eventData = event_.EventData;
            if (eventData?.TryGetValue("waitTime", out var waitTimeObj) == true)
            {
                if (double.TryParse(waitTimeObj.ToString(), out var waitTime))
                {
                    waitTimes.Add(waitTime);
                }
            }
        }

        return waitTimes.Any() ? waitTimes.Average() : 0;
    }

    private async Task<double> GetAverageRating(string tenantId, DateTime from, DateTime to)
    {
        var feedbackEvents = await _context.AnalyticsEvents
            .Where(e => e.TenantId == tenantId &&
                       e.EventType == "live_agent_feedbackSubmitted" &&
                       e.Timestamp >= from &&
                       e.Timestamp <= to)
            .ToListAsync();

        if (!feedbackEvents.Any()) return 0;

        var ratings = new List<double>();
        foreach (var event_ in feedbackEvents)
        {
            var eventData = event_.EventData;
            if (eventData?.TryGetValue("rating", out var ratingObj) == true)
            {
                if (double.TryParse(ratingObj.ToString(), out var rating))
                {
                    ratings.Add(rating);
                }
            }
        }

        return ratings.Any() ? ratings.Average() : 0;
    }

    private async Task<Dictionary<string, int>> GetSatisfactionBreakdown(string tenantId, DateTime from, DateTime to)
    {
        var feedbackEvents = await _context.AnalyticsEvents
            .Where(e => e.TenantId == tenantId &&
                       e.EventType == "live_agent_feedbackSubmitted" &&
                       e.Timestamp >= from &&
                       e.Timestamp <= to)
            .ToListAsync();

        var breakdown = new Dictionary<string, int>
        {
            ["satisfied"] = 0,
            ["neutral"] = 0,
            ["unsatisfied"] = 0
        };

        foreach (var event_ in feedbackEvents)
        {
            var eventData = event_.EventData;
            if (eventData?.TryGetValue("satisfaction", out var satisfactionObj) == true)
            {
                var satisfaction = satisfactionObj.ToString();
                if (breakdown.ContainsKey(satisfaction))
                {
                    breakdown[satisfaction]++;
                }
            }
        }

        return breakdown;
    }

    private async Task<Dictionary<string, int>> GetMessagesByType(string tenantId, DateTime from, DateTime to)
    {
        var messageEvents = await _context.AnalyticsEvents
            .Where(e => e.TenantId == tenantId &&
                       (e.EventType == "live_agent_messageReceived" || e.EventType == "live_agent_messageSent") &&
                       e.Timestamp >= from &&
                       e.Timestamp <= to)
            .ToListAsync();

        var breakdown = new Dictionary<string, int>
        {
            ["text"] = 0,
            ["file"] = 0,
            ["image"] = 0
        };

        foreach (var event_ in messageEvents)
        {
            var eventData = event_.EventData;
            if (eventData?.TryGetValue("messageType", out var messageTypeObj) == true)
            {
                var messageType = messageTypeObj.ToString();
                if (breakdown.ContainsKey(messageType))
                {
                    breakdown[messageType]++;
                }
            }
        }

        return breakdown;
    }

    private async Task<List<AgentPerformance>> GetTopAgents(string tenantId, DateTime from, DateTime to)
    {
        var agentEvents = await _context.AnalyticsEvents
            .Where(e => e.TenantId == tenantId &&
                       !string.IsNullOrEmpty(e.AgentId) &&
                       e.Timestamp >= from &&
                       e.Timestamp <= to)
            .GroupBy(e => e.AgentId)
            .ToListAsync();

        var topAgents = new List<AgentPerformance>();

        foreach (var agentGroup in agentEvents)
        {
            var agentId = agentGroup.Key;
            var events = agentGroup.ToList();

            var performance = new AgentPerformance
            {
                AgentId = agentId,
                ConversationsHandled = events.Count(e => e.EventType == "live_agent_agentAssigned"),
                MessagesSent = events.Count(e => e.EventType == "live_agent_messageSent"),
                AverageRating = await GetAgentAverageRating(agentId, tenantId, from, to),
                FeedbackCount = events.Count(e => e.EventType == "live_agent_feedbackSubmitted"),
                AverageResponseTime = await GetAgentAverageResponseTime(agentId, tenantId, from, to)
            };

            topAgents.Add(performance);
        }

        return topAgents.OrderByDescending(a => a.AverageRating).ThenByDescending(a => a.ConversationsHandled).Take(10).ToList();
    }

    // Additional helper methods with placeholder implementations
    private async Task<int> GetActiveConversations(string agentId, string tenantId) => 0;
    private async Task<double> GetAgentAverageResponseTime(string agentId, string tenantId, DateTime from, DateTime to) => 0;
    private async Task<double> GetAgentAverageSessionDuration(string agentId, string tenantId, DateTime from, DateTime to) => 0;
    private async Task<double> GetAgentTotalOnlineTime(string agentId, string tenantId, DateTime from, DateTime to) => 0;
    private async Task<double> GetAgentAverageRating(string agentId, string tenantId, DateTime from, DateTime to) => 0;
    private async Task<Dictionary<string, int>> GetAgentSatisfactionDistribution(string agentId, string tenantId, DateTime from, DateTime to) => new();
    private async Task<List<DailyMetric>> GetAgentDailyMetrics(string agentId, string tenantId, DateTime from, DateTime to) => new();
    private async Task<double> GetConversationDuration(string conversationId, string tenantId) => 0;
    private async Task<double> GetConversationWaitTime(string conversationId, string tenantId) => 0;
    private async Task<List<double>> GetConversationResponseTimes(string conversationId, string tenantId) => new();
    private async Task<double?> GetConversationSatisfaction(string conversationId, string tenantId) => null;
    private async Task<int> GetQueuedConversationsCount(string tenantId) => 0;
    private async Task<int> GetConversationsStarted(string tenantId, DateTime from, DateTime to) => 0;
    private async Task<int> GetMessagesCount(string tenantId, DateTime from, DateTime to) => 0;
    private async Task<int> GetFeedbackCount(string tenantId, DateTime from, DateTime to) => 0;

    // Add methods needed by the message bus service
    public async Task<List<string>> GetActiveTenantsAsync()
    {
        try
        {
            var tenants = await _context.Tenants
                .Where(t => t.IsActive)
                .Select(t => t.Id.ToString())
                .ToListAsync();

            return tenants;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get active tenants");
            return new List<string>();
        }
    }

    public async Task ProcessPendingEventBatchesAsync()
    {
        try
        {
            // Implementation for processing pending event batches
            // This could involve processing events that failed to process initially
            _logger.LogDebug("Processing pending event batches - placeholder implementation");
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process pending event batches");
            throw;
        }
    }

    #endregion
}

