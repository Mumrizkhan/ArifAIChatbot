using Shared.Domain.Entities;
using SubscriptionService.Models;

namespace SubscriptionService.Services;

public interface IUsageTrackingService
{
    Task RecordUsageAsync(Guid tenantId, string metricName, int quantity, Dictionary<string, object>? metadata = null);
    Task<List<UsageRecord>> GetUsageAsync(Guid tenantId, string? metricName = null, DateTime? startDate = null, DateTime? endDate = null);
    Task<Dictionary<string, int>> GetUsageSummaryAsync(Guid tenantId, DateTime? startDate = null, DateTime? endDate = null);
    Task<bool> CheckUsageLimitAsync(Guid tenantId, string metricName);
    Task<UsageStatistics> GetUsageStatisticsAsync(Guid? tenantId = null, DateTime? startDate = null, DateTime? endDate = null);
    Task ProcessUsageAggregationAsync();
}
