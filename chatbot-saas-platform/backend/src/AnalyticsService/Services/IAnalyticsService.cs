using AnalyticsService.Models;

namespace AnalyticsService.Services;

public interface IAnalyticsService
{
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
}
