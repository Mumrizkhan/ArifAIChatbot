namespace AnalyticsService.Models;

public class ConversationsSummaryDto
{
    public int TotalConversations { get; set; }
    public int ActiveConversations { get; set; }
    public int CompletedConversations { get; set; }
    public double AverageResponseTime { get; set; }
}

public class MessagesSummaryDto
{
    public int TotalMessages { get; set; }
    public int BotMessages { get; set; }
    public int UserMessages { get; set; }
    public int AgentMessages { get; set; }
}

public class TenantsSummaryDto
{
    public int TotalTenants { get; set; }
    public int ActiveTenants { get; set; }
    public int NewTenants { get; set; }
}

public class RealtimeAnalyticsDto
{
    public int ActiveUsers { get; set; }
    public int OngoingConversations { get; set; }
    public int AvailableAgents { get; set; }
    public double SystemLoad { get; set; }
}

public class AnalyticsDto
{
    public Dictionary<string, object> Data { get; set; } = new();
    public string TimeRange { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; }
}

public class AnalyticsRequest
{
    public string? DateFrom { get; set; }
    public string? DateTo { get; set; }
    public string? TenantId { get; set; }
    public string? MetricType { get; set; }
}

public class ExportAnalyticsRequest
{
    public string Format { get; set; } = "csv";
    public string? DateFrom { get; set; }
    public string? DateTo { get; set; }
    public string? TenantId { get; set; }
}

public class DashboardStatsDto
{
    public int TotalUsers { get; set; }
    public int TotalConversations { get; set; }
    public int TotalMessages { get; set; }
    public double AverageResponseTime { get; set; }
}

public class ConversationMetricsDto
{
    public int Total { get; set; }
    public int Completed { get; set; }
    public int Active { get; set; }
    public double AverageRating { get; set; }
}

public class AgentMetricsDto
{
    public int TotalAgents { get; set; }
    public int ActiveAgents { get; set; }
    public double AverageResponseTime { get; set; }
    public double AverageRating { get; set; }
}

public class BotMetricsDto
{
    public int TotalInteractions { get; set; }
    public double SuccessRate { get; set; }
    public double AverageResponseTime { get; set; }
}

public class OverviewMetricsDto
{
    public Dictionary<string, int> ConversationsByDate { get; set; } = new();
    public Dictionary<string, int> MessagesByDate { get; set; } = new();
}

public class UserMetricsDto
{
    public int TotalUsers { get; set; }
    public int ActiveUsers { get; set; }
    public int NewUsers { get; set; }
}

public class ChatbotMetricsDto
{
    public int TotalChatbots { get; set; }
    public int ActiveChatbots { get; set; }
    public double AverageUptime { get; set; }
}

public class SubscriptionMetricsDto
{
    public int TotalSubscriptions { get; set; }
    public int ActiveSubscriptions { get; set; }
    public decimal TotalRevenue { get; set; }
}

public class PerformanceMetricsDto
{
    public double AverageResponseTime { get; set; }
    public double SystemUptime { get; set; }
    public int ErrorRate { get; set; }
}

public class ReportDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? LastRunAt { get; set; }
}

public class CreateReportRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public Dictionary<string, object> Configuration { get; set; } = new();
}

public class UpdateReportRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public Dictionary<string, object>? Configuration { get; set; }
}

public class ReportExecutionDto
{
    public Guid Id { get; set; }
    public Guid ReportId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ResultUrl { get; set; }
}

public class CompareAnalyticsDto
{
    public object Current { get; set; } = new();
    public object Previous { get; set; } = new();
    public double Change { get; set; }
    public double ChangePercent { get; set; }
}

public class GoalDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public double Target { get; set; }
    public double Progress { get; set; }
    public string Metric { get; set; } = string.Empty;
    public DateTime Deadline { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CustomReportDto
{
    public int TotalConversations { get; set; }
    public int TotalMessages { get; set; }
    public double AverageMessagesPerConversation { get; set; }
    public int ResolvedConversations { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public DateTime GeneratedAt { get; set; }
}

public class CompareAnalyticsRequest
{
    public string DateFrom { get; set; } = string.Empty;
    public string DateTo { get; set; } = string.Empty;
    public string PreviousDateFrom { get; set; } = string.Empty;
    public string PreviousDateTo { get; set; } = string.Empty;
    public string Metric { get; set; } = string.Empty;
}

public class CreateGoalRequest
{
    public string Name { get; set; } = string.Empty;
    public double Target { get; set; }
    public string Metric { get; set; } = string.Empty;
    public string Deadline { get; set; } = string.Empty;
}

public class UpdateGoalRequest
{
    public string? Name { get; set; }
    public double? Target { get; set; }
    public string? Deadline { get; set; }
}

public class CustomReportRequest
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string? TenantId { get; set; }
    public List<string> Metrics { get; set; } = new();
}

/// <summary>
/// Request DTO for batching analytics events
/// </summary>
public class AnalyticsEventBatchRequest
{
    public List<AnalyticsEventDto> Events { get; set; } = new();
}

/// <summary>
/// DTO for analytics events
/// </summary>
public class AnalyticsEventDto
{
    public string EventType { get; set; } = null!;
    public Dictionary<string, object>? EventData { get; set; }
    public DateTime Timestamp { get; set; }
    public string SessionId { get; set; } = null!;
    public string? ConversationId { get; set; }
    public string? AgentId { get; set; }
    public string? UserId { get; set; }
    public string TenantId { get; set; } = null!;
    public Dictionary<string, object>? Metadata { get; set; }
}

/// <summary>
/// Analytics dashboard data
/// </summary>
public class AnalyticsDashboard
{
    public string TenantId { get; set; } = null!;
    public DateRange DateRange { get; set; } = null!;

    // Conversation metrics
    public int TotalConversations { get; set; }
    public int ConversationsWithAgents { get; set; }
    public double AverageConversationDuration { get; set; } // in milliseconds

    // Agent metrics
    public int TotalAgentRequests { get; set; }
    public int AgentAssignments { get; set; }
    public double AverageWaitTime { get; set; } // in milliseconds
    public double AverageResponseTime { get; set; } // in milliseconds

    // Customer satisfaction
    public int FeedbackSubmissions { get; set; }
    public double AverageRating { get; set; } // 1-5 scale
    public Dictionary<string, int> SatisfactionBreakdown { get; set; } = new();

    // Message metrics
    public int TotalMessages { get; set; }
    public Dictionary<string, int> MessagesByType { get; set; } = new();

    // Top performing agents
    public List<AgentPerformance> TopAgents { get; set; } = new();
}

/// <summary>
/// Agent performance metrics
/// </summary>
public class AgentPerformance
{
    public string AgentId { get; set; } = null!;
    public string? AgentName { get; set; }
    public int ConversationsHandled { get; set; }
    public int MessagesSent { get; set; }
    public double AverageRating { get; set; }
    public int FeedbackCount { get; set; }
    public double AverageResponseTime { get; set; } // in milliseconds
}

/// <summary>
/// Detailed agent metrics
/// </summary>
public class AgentMetrics
{
    public string AgentId { get; set; } = null!;
    public DateRange DateRange { get; set; } = null!;

    // Conversation metrics
    public int TotalConversations { get; set; }
    public int ActiveConversations { get; set; }
    public int CompletedConversations { get; set; }

    // Time metrics
    public double AverageResponseTime { get; set; } // in milliseconds
    public double AverageSessionDuration { get; set; } // in milliseconds
    public double TotalOnlineTime { get; set; } // in milliseconds

    // Message metrics
    public int MessagesSent { get; set; }
    public int MessagesReceived { get; set; }

    // Customer satisfaction
    public int FeedbackReceived { get; set; }
    public double AverageRating { get; set; }
    public Dictionary<string, int> SatisfactionDistribution { get; set; } = new();

    // Performance trends
    public List<DailyMetric> DailyMetrics { get; set; } = new();
}

/// <summary>
/// Daily performance metrics
/// </summary>
public class DailyMetric
{
    public DateTime Date { get; set; }
    public int Conversations { get; set; }
    public int Messages { get; set; }
    public double AverageResponseTime { get; set; }
    public double AverageRating { get; set; }
    public int FeedbackCount { get; set; }
}

/// <summary>
/// Conversation analytics
/// </summary>
public class ConversationAnalytics
{
    public string ConversationId { get; set; } = null!;
    public List<AnalyticsEventDto> Events { get; set; } = new();
    public double Duration { get; set; } // in milliseconds
    public int MessageCount { get; set; }
    public int AgentCount { get; set; }
    public double WaitTime { get; set; } // in milliseconds
    public List<double> ResponseTimes { get; set; } = new();
    public double? CustomerSatisfaction { get; set; }
}

/// <summary>
/// Real-time analytics data
/// </summary>
public class RealtimeAnalytics
{
    public string TenantId { get; set; } = null!;
    public DateTime Timestamp { get; set; }

    // Current state
    public int ActiveConversations { get; set; }
    public int OnlineAgents { get; set; }
    public int QueuedConversations { get; set; }

    // Last hour metrics
    public int ConversationsStartedLastHour { get; set; }
    public int MessagesLastHour { get; set; }
    public int FeedbackLastHour { get; set; }

    // Last 24 hours metrics
    public int ConversationsStartedLast24Hours { get; set; }
    public double AverageWaitTimeLast24Hours { get; set; }
    public double AverageResponseTimeLast24Hours { get; set; }
    public double CustomerSatisfactionLast24Hours { get; set; }
}

/// <summary>
/// Date range for analytics queries
/// </summary>
public class DateRange
{
    public DateTime From { get; set; }
    public DateTime To { get; set; }
}

/// <summary>
/// Live agent analytics summary
/// </summary>
public class LiveAgentAnalyticsSummary
{
    public string TenantId { get; set; } = null!;
    public DateRange DateRange { get; set; } = null!;

    // Agent request and assignment metrics
    public int TotalAgentRequests { get; set; }
    public int SuccessfulAssignments { get; set; }
    public double AssignmentSuccessRate => TotalAgentRequests > 0 ? (double)SuccessfulAssignments / TotalAgentRequests * 100 : 0;
    public double AverageWaitTime { get; set; } // in milliseconds
    public double MedianWaitTime { get; set; } // in milliseconds

    // Response time metrics
    public double AverageFirstResponseTime { get; set; } // in milliseconds
    public double AverageResponseTime { get; set; } // in milliseconds
    public double MedianResponseTime { get; set; } // in milliseconds

    // Conversation completion metrics
    public int CompletedConversations { get; set; }
    public int EscalatedConversations { get; set; }
    public int TransferredConversations { get; set; }
    public double AverageConversationDuration { get; set; } // in milliseconds

    // Customer satisfaction metrics
    public int TotalFeedback { get; set; }
    public double AverageRating { get; set; }
    public int SatisfiedCustomers { get; set; }
    public int NeutralCustomers { get; set; }
    public int UnsatisfiedCustomers { get; set; }
    public double SatisfactionRate => TotalFeedback > 0 ? (double)SatisfiedCustomers / TotalFeedback * 100 : 0;

    // Agent performance distribution
    public List<AgentPerformanceDistribution> AgentDistribution { get; set; } = new();

    // Hourly trends
    public List<HourlyMetric> HourlyTrends { get; set; } = new();
}

/// <summary>
/// Agent performance distribution
/// </summary>
public class AgentPerformanceDistribution
{
    public string PerformanceRange { get; set; } = null!; // e.g., "4.0-5.0", "3.0-4.0", etc.
    public int AgentCount { get; set; }
    public double Percentage { get; set; }
}

/// <summary>
/// Hourly performance metrics
/// </summary>
public class HourlyMetric
{
    public DateTime Hour { get; set; }
    public int AgentRequests { get; set; }
    public int Assignments { get; set; }
    public double AverageWaitTime { get; set; }
    public double AverageResponseTime { get; set; }
    public double AverageRating { get; set; }
    public int ActiveAgents { get; set; }
}

/// <summary>
/// Agent workload metrics
/// </summary>
public class AgentWorkload
{
    public string AgentId { get; set; } = null!;
    public string? AgentName { get; set; }
    public int CurrentConversations { get; set; }
    public int MaxConcurrentConversations { get; set; }
    public double WorkloadPercentage => MaxConcurrentConversations > 0 ? (double)CurrentConversations / MaxConcurrentConversations * 100 : 0;
    public string Status { get; set; } = "offline"; // online, busy, away, offline
    public DateTime LastActivity { get; set; }
    public double AverageResponseTime { get; set; }
    public double CustomerSatisfaction { get; set; }
    public int ConversationsToday { get; set; }
    public int MessagesToday { get; set; }
}
