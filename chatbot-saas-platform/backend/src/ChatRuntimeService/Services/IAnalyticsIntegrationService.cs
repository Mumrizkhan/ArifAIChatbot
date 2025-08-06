using AnalyticsService.Models;

namespace ChatRuntimeService.Services;

public interface IAnalyticsIntegrationService
{
    Task<RealtimeAnalyticsDto> GetRealtimeAnalyticsAsync();
    Task<DashboardStatsDto> GetDashboardStatsAsync();
    Task<ConversationMetricsDto> GetConversationMetricsAsync(string timeRange, string? tenantId = null);
    Task<AgentMetricsDto> GetAgentMetricsAsync(string timeRange, string? tenantId = null);
    Task<AnalyticsDto> GetTenantAnalyticsAsync(string tenantId, string startDate, string endDate);
}
