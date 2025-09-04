using AnalyticsService.Models;
using Shared.Domain.Entities;

namespace AnalyticsService.Services;

public interface IAnalyticsService
{

    Task<LiveAgentAnalyticsSummary> GetLiveAgentSummaryAsync(string tenantId, DateTime from, DateTime to);
    Task<List<AgentWorkload>> GetAgentWorkloadsAsync(string tenantId);
    Task<List<HourlyMetric>> GetHourlyTrendsAsync(string tenantId, DateTime from, DateTime to);
    Task ProcessAnalyticsEventAsync(AnalyticsEvent analyticsEvent);
    Task<Dictionary<string, object>> GetRealtimeMetricsAsync(string tenantId);
    Task<bool> ValidateAnalyticsEventAsync(AnalyticsEventDto eventDto);

    Task<ConversationsSummaryDto> GetConversationsSummaryAsync();
    Task<MessagesSummaryDto> GetMessagesSummaryAsync();
    Task<TenantsSummaryDto> GetTenantsSummaryAsync();
    Task<RealtimeAnalyticsDto> GetRealtimeAnalyticsAsync();
    Task<AnalyticsDto> GetAnalyticsAsync(AnalyticsRequest request);
    Task<byte[]> ExportAnalyticsAsync(ExportAnalyticsRequest request);
    Task<DashboardStatsDto> GetDashboardStatsAsync();
    Task<ConversationMetricsDto> GetConversationMetricsAsync(string timeRange, string? tenantId);
    Task<AgentMetricsDto> GetAgentMetricsAsync(string timeRange, string? tenantId);
    Task<BotMetricsDto> GetBotMetricsAsync(string timeRange, string? tenantId);
    Task<OverviewMetricsDto> GetOverviewMetricsAsync(string? dateFrom, string? dateTo, string granularity);
    Task<UserMetricsDto> GetUserMetricsAsync(string? dateFrom, string? dateTo, string granularity);
    Task<ChatbotMetricsDto> GetChatbotMetricsAsync(string? dateFrom, string? dateTo, string granularity);
    Task<SubscriptionMetricsDto> GetSubscriptionMetricsAsync(string? dateFrom, string? dateTo, string granularity);
    Task<PerformanceMetricsDto> GetPerformanceMetricsAsync(string? dateFrom, string? dateTo);
    Task<List<ReportDto>> GetReportsAsync();
    Task<ReportDto> GetReportAsync(Guid id);
    Task<ReportDto> CreateReportAsync(CreateReportRequest request);
    Task<bool> UpdateReportAsync(Guid id, UpdateReportRequest request);
    Task<bool> DeleteReportAsync(Guid id);
    Task<ReportExecutionDto> RunReportAsync(Guid id);
    Task<CompareAnalyticsDto> CompareAnalyticsAsync(CompareAnalyticsRequest request);
    Task<List<GoalDto>> GetGoalsAsync();
    Task<GoalDto> CreateGoalAsync(CreateGoalRequest request);
    Task<bool> UpdateGoalAsync(Guid id, UpdateGoalRequest request);
    Task<bool> DeleteGoalAsync(Guid id);
    Task<CustomReportDto> GetCustomReportAsync(CustomReportRequest request);

    // Add new methods for database operations that were in the controller
    Task<bool> TrackEventsBatchAsync(AnalyticsEventBatchRequest request, string tenantId);
    Task<AnalyticsDashboard> GetDashboardDataAsync(string tenantId, DateTime? from, DateTime? to);
    Task<AgentMetrics> GetAgentMetricsDetailedAsync(string agentId, string tenantId, DateTime? from, DateTime? to);
    Task<ConversationAnalytics> GetConversationAnalyticsAsync(string conversationId, string tenantId);
    Task<RealtimeAnalytics> GetRealtimeAnalyticsDetailedAsync(string tenantId);

    // Add methods needed by the message bus service
    Task<List<string>> GetActiveTenantsAsync();
    Task ProcessPendingEventBatchesAsync();
}
