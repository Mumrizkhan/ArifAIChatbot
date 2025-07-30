using Microsoft.EntityFrameworkCore;
using Shared.Application.Common.Interfaces;
using SubscriptionService.Services;
using SubscriptionService.Models;
using Shared.Domain.Entities;

namespace SubscriptionService.Services;

public class UsageTrackingService : IUsageTrackingService
{
    private readonly IApplicationDbContext _context;
    private readonly IPlanService _planService;
    private readonly ILogger<UsageTrackingService> _logger;

    public UsageTrackingService(
        IApplicationDbContext context,
        IPlanService planService,
        ILogger<UsageTrackingService> logger)
    {
        _context = context;
        _planService = planService;
        _logger = logger;
    }

    public async Task RecordUsageAsync(Guid tenantId, string metricName, int quantity, Dictionary<string, object>? metadata = null)
    {
        try
        {
            var now = DateTime.UtcNow;
            var periodStart = new DateTime(now.Year, now.Month, 1);
            var periodEnd = periodStart.AddMonths(1).AddDays(-1);

            var existingRecord = await _context.Set<UsageRecord>()
                .FirstOrDefaultAsync(ur => ur.TenantId == tenantId && 
                                          ur.MetricName == metricName &&
                                          ur.PeriodStart == periodStart &&
                                          ur.PeriodEnd == periodEnd);

            if (existingRecord != null)
            {
                existingRecord.Quantity += quantity;
                existingRecord.RecordedAt = now;
                existingRecord.UpdatedAt = now;
                
                if (metadata != null)
                {
                    foreach (var kvp in metadata)
                    {
                        existingRecord.Metadata[kvp.Key] = kvp.Value;
                    }
                }
            }
            else
            {
                var subscription = await _context.Set<Subscription>()
                    .FirstOrDefaultAsync(s => s.TenantId == tenantId && 
                        (s.Status == SubscriptionStatus.Active || s.Status == SubscriptionStatus.Trialing));

                var usageRecord = new UsageRecord
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    SubscriptionId = subscription?.Id,
                    MetricName = metricName,
                    Quantity = quantity,
                    RecordedAt = now,
                    PeriodStart = periodStart,
                    PeriodEnd = periodEnd,
                    Metadata = metadata ?? new Dictionary<string, object>(),
                    CreatedAt = now
                };

                _context.Set<UsageRecord>().Add(usageRecord);
            }

            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording usage for tenant {TenantId}, metric {MetricName}", tenantId, metricName);
            throw;
        }
    }

    public async Task<List<UsageRecord>> GetUsageAsync(Guid tenantId, string? metricName = null, DateTime? startDate = null, DateTime? endDate = null)
    {
        try
        {
            var query = _context.Set<UsageRecord>()
                .Where(ur => ur.TenantId == tenantId);

            if (!string.IsNullOrEmpty(metricName))
            {
                query = query.Where(ur => ur.MetricName == metricName);
            }

            if (startDate.HasValue)
            {
                query = query.Where(ur => ur.PeriodStart >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(ur => ur.PeriodEnd <= endDate.Value);
            }

            return await query
                .OrderByDescending(ur => ur.PeriodStart)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting usage for tenant {TenantId}", tenantId);
            throw;
        }
    }

    public async Task<Dictionary<string, int>> GetUsageSummaryAsync(Guid tenantId, DateTime? startDate = null, DateTime? endDate = null)
    {
        try
        {
            var start = startDate ?? DateTime.UtcNow.AddDays(-30);
            var end = endDate ?? DateTime.UtcNow;

            var usageRecords = await _context.Set<UsageRecord>()
                .Where(ur => ur.TenantId == tenantId && 
                            ur.PeriodStart >= start && 
                            ur.PeriodEnd <= end)
                .ToListAsync();

            return usageRecords
                .GroupBy(ur => ur.MetricName)
                .ToDictionary(g => g.Key, g => g.Sum(ur => ur.Quantity));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting usage summary for tenant {TenantId}", tenantId);
            throw;
        }
    }

    public async Task<bool> CheckUsageLimitAsync(Guid tenantId, string metricName)
    {
        try
        {
            var limit = await _planService.GetFeatureLimitAsync(tenantId, metricName);
            if (!limit.HasValue)
                return true; // No limit set

            var currentUsage = await GetCurrentMonthUsageAsync(tenantId, metricName);
            return currentUsage < limit.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking usage limit for tenant {TenantId}, metric {MetricName}", tenantId, metricName);
            return false;
        }
    }

    public async Task<UsageStatistics> GetUsageStatisticsAsync(Guid? tenantId = null, DateTime? startDate = null, DateTime? endDate = null)
    {
        try
        {
            var start = startDate ?? DateTime.UtcNow.AddDays(-30);
            var end = endDate ?? DateTime.UtcNow;

            var query = _context.Set<UsageRecord>()
                .Where(ur => ur.PeriodStart >= start && ur.PeriodEnd <= end);

            if (tenantId.HasValue)
            {
                query = query.Where(ur => ur.TenantId == tenantId.Value);
            }

            var usageRecords = await query.ToListAsync();

            var usageByMetric = usageRecords
                .GroupBy(ur => ur.MetricName)
                .ToDictionary(g => g.Key, g => g.Sum(ur => ur.Quantity));

            var usageByTenant = usageRecords
                .GroupBy(ur => ur.TenantId)
                .ToDictionary(g => g.Key.ToString(), g => g.Sum(ur => ur.Quantity));

            return new UsageStatistics
            {
                UsageByMetric = usageByMetric,
                UsageByTenant = usageByTenant,
                TotalUsage = usageRecords.Sum(ur => ur.Quantity),
                PeriodStart = start,
                PeriodEnd = end
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting usage statistics");
            throw;
        }
    }

    public async Task ProcessUsageAggregationAsync()
    {
        try
        {
            var lastMonth = DateTime.UtcNow.AddMonths(-1);
            var periodStart = new DateTime(lastMonth.Year, lastMonth.Month, 1);
            var periodEnd = periodStart.AddMonths(1).AddDays(-1);

            var usageRecords = await _context.Set<UsageRecord>()
                .Where(ur => ur.PeriodStart == periodStart && ur.PeriodEnd == periodEnd)
                .GroupBy(ur => new { ur.TenantId, ur.MetricName })
                .Select(g => new
                {
                    g.Key.TenantId,
                    g.Key.MetricName,
                    TotalQuantity = g.Sum(ur => ur.Quantity),
                    RecordCount = g.Count()
                })
                .ToListAsync();

            foreach (var aggregation in usageRecords)
            {
                _logger.LogInformation("Aggregated usage for tenant {TenantId}, metric {MetricName}: {Quantity}", 
                    aggregation.TenantId, aggregation.MetricName, aggregation.TotalQuantity);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing usage aggregation");
        }
    }

    private async Task<int> GetCurrentMonthUsageAsync(Guid tenantId, string metricName)
    {
        var now = DateTime.UtcNow;
        var periodStart = new DateTime(now.Year, now.Month, 1);
        var periodEnd = periodStart.AddMonths(1).AddDays(-1);

        var usageRecord = await _context.Set<UsageRecord>()
            .FirstOrDefaultAsync(ur => ur.TenantId == tenantId && 
                                      ur.MetricName == metricName &&
                                      ur.PeriodStart == periodStart &&
                                      ur.PeriodEnd == periodEnd);

        return usageRecord?.Quantity ?? 0;
    }
}
