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
