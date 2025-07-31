using Shared.Domain.Entities;
using SubscriptionService.Models;

namespace SubscriptionService.Services;

public interface ISubscriptionService
{
    Task<Subscription> CreateSubscriptionAsync(CreateSubscriptionRequest request, Guid tenantId);
    Task<Subscription?> GetSubscriptionAsync(Guid subscriptionId, Guid tenantId);
    Task<Subscription?> GetActiveSubscriptionAsync(Guid tenantId);
    Task<List<Subscription>> GetSubscriptionsAsync(Guid tenantId);
    Task<bool> UpdateSubscriptionAsync(Guid subscriptionId, UpdateSubscriptionRequest request, Guid tenantId);
    Task<bool> CancelSubscriptionAsync(Guid subscriptionId, Guid tenantId, bool immediately = false);
    Task<bool> ReactivateSubscriptionAsync(Guid subscriptionId, Guid tenantId);
    Task<bool> UpgradeSubscriptionAsync(Guid subscriptionId, Guid newPlanId, Guid tenantId);
    Task<bool> DowngradeSubscriptionAsync(Guid subscriptionId, Guid newPlanId, Guid tenantId);
    Task ProcessSubscriptionRenewalsAsync();
    Task ProcessTrialExpirationsAsync();
    Task<BillingStatistics> GetBillingStatisticsAsync(DateTime? startDate = null, DateTime? endDate = null);
    Task<PreviewSubscriptionChangeResult> PreviewSubscriptionChangeAsync(Guid newPlanId, Guid tenantId, BillingCycle billingCycle);

}
