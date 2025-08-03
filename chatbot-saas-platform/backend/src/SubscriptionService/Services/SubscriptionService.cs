using Microsoft.EntityFrameworkCore;
using Shared.Application.Common.Interfaces;
using SubscriptionService.Services;
using SubscriptionService.Models;
using Hangfire;
using Shared.Domain.Entities;
using Shared.Domain.Enums;

namespace SubscriptionService.Services;

public class SubscriptionService : ISubscriptionService
{
    private readonly IApplicationDbContext _context;

    public SubscriptionService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> CancelSubscriptionAsync(Guid subscriptionId, Guid tenantId, bool immediately = false)
    {
        var subscription = await _context.Subscriptions
            .FirstOrDefaultAsync(s => s.Id == subscriptionId && s.TenantId == tenantId);

        if (subscription == null || subscription.Status == SubscriptionStatus.Cancelled)
            return false;

        subscription.Status = SubscriptionStatus.Cancelled;
        subscription.EndDate = immediately ? DateTime.UtcNow : subscription.EndDate ?? DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<Subscription> CreateSubscriptionAsync(CreateSubscriptionRequest request, Guid tenantId)
    {
        var plan = await _context.Plans.FirstOrDefaultAsync(p => p.Id == request.PlanId && p.IsActive);
        if (plan == null)
            throw new InvalidOperationException("Plan not found or inactive.");

        var subscription = new Subscription
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            PlanId = request.PlanId,
            Status = SubscriptionStatus.Active,
            StartDate = DateTime.UtcNow,
            BillingCycle = request.BillingCycle,
            NextBillingDate = DateTime.UtcNow.AddMonths(request.BillingCycle == BillingCycle.Monthly ? 1 : 12),
            Amount = request.BillingCycle == BillingCycle.Monthly ? plan.MonthlyPrice : plan.YearlyPrice,
            Currency = plan.Currency,
            TrialDays = plan.TrialDays,
            TrialEndDate = plan.TrialDays > 0 ? DateTime.UtcNow.AddDays(plan.TrialDays) : null
        };

        _context.Subscriptions.Add(subscription);
        await _context.SaveChangesAsync();
        return subscription;
    }

    public async Task<bool> DowngradeSubscriptionAsync(Guid subscriptionId, Guid newPlanId, Guid tenantId)
    {
        var subscription = await _context.Subscriptions
            .FirstOrDefaultAsync(s => s.Id == subscriptionId && s.TenantId == tenantId);

        var plan = await _context.Plans.FirstOrDefaultAsync(p => p.Id == newPlanId && p.IsActive);
        if (subscription == null || plan == null)
            return false;

        subscription.PlanId = newPlanId;
        subscription.Amount = subscription.BillingCycle == BillingCycle.Monthly ? plan.MonthlyPrice : plan.YearlyPrice;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<Subscription?> GetActiveSubscriptionAsync(Guid tenantId)
    {
        return await _context.Subscriptions
            .FirstOrDefaultAsync(s => s.TenantId == tenantId && s.Status == SubscriptionStatus.Active);
    }

    public async Task<BillingStatistics> GetBillingStatisticsAsync(DateTime? startDate = null, DateTime? endDate = null)
    {
        var query = _context.Subscriptions.AsQueryable();
        if (startDate.HasValue)
            query = query.Where(s => s.StartDate >= startDate.Value);
        if (endDate.HasValue)
            query = query.Where(s => s.StartDate <= endDate.Value);

        var activeSubscriptions = await query.CountAsync(s => s.Status == SubscriptionStatus.Active);
        var trialSubscriptions = await query.CountAsync(s => s.Status == SubscriptionStatus.Trialing);
        var cancelledSubscriptions = await query.CountAsync(s => s.Status == SubscriptionStatus.Cancelled);

        var subscriptionsByPlan = await query
            .GroupBy(s => s.PlanId)
            .Select(g => new { PlanId = g.Key.ToString(), Count = g.Count() })
            .ToDictionaryAsync(x => x.PlanId, x => x.Count);

        var revenueByPlan = await query
            .GroupBy(s => s.PlanId)
            .Select(g => new { PlanId = g.Key.ToString(), Revenue = g.Sum(s => s.Amount) })
            .ToDictionaryAsync(x => x.PlanId, x => x.Revenue);

        return new BillingStatistics
        {
            ActiveSubscriptions = activeSubscriptions,
            TrialSubscriptions = trialSubscriptions,
            CancelledSubscriptions = cancelledSubscriptions,
            SubscriptionsByPlan = subscriptionsByPlan,
            RevenueByPlan = revenueByPlan,
            PeriodStart = startDate ?? DateTime.MinValue,
            PeriodEnd = endDate ?? DateTime.MaxValue
        };
    }

    public async Task<object> GetBillingStatsAsync()
    {
        var totalRevenue = await _context.Subscriptions
            .Where(s => s.Status == SubscriptionStatus.Active)
            .SumAsync(s => s.Amount);

        var activeSubscriptions = await _context.Subscriptions
            .CountAsync(s => s.Status == SubscriptionStatus.Active);

        var trialSubscriptions = await _context.Subscriptions
            .CountAsync(s => s.Status == SubscriptionStatus.Trialing);

        var cancelledSubscriptions = await _context.Subscriptions
            .CountAsync(s => s.Status == SubscriptionStatus.Cancelled);

        var monthlyRevenue = await _context.Subscriptions
            .Where(s => s.Status == SubscriptionStatus.Active &&
                        s.BillingCycle == BillingCycle.Monthly)
            .SumAsync(s => s.Amount);

        var yearlyRevenue = await _context.Subscriptions
            .Where(s => s.Status == SubscriptionStatus.Active &&
                        s.BillingCycle == BillingCycle.Yearly)
            .SumAsync(s => s.Amount);

        return new
        {
            TotalRevenue = totalRevenue,
            MonthlyRevenue = monthlyRevenue,
            YearlyRevenue = yearlyRevenue,
            ActiveSubscriptions = activeSubscriptions,
            TrialSubscriptions = trialSubscriptions,
            CancelledSubscriptions = cancelledSubscriptions,
            TotalSubscriptions = activeSubscriptions + trialSubscriptions + cancelledSubscriptions
        };
    }

    public async Task<Subscription?> GetSubscriptionAsync(Guid subscriptionId, Guid tenantId)
    {
        return await _context.Subscriptions
            .FirstOrDefaultAsync(s => s.Id == subscriptionId && s.TenantId == tenantId);
    }

    public async Task<List<Subscription>> GetSubscriptionsAsync(Guid tenantId)
    {
        return await _context.Subscriptions
            .Where(s => s.TenantId == tenantId)
            .ToListAsync();
    }

    public async Task ProcessSubscriptionRenewalsAsync()
    {
        var now = DateTime.UtcNow;
        var subscriptions = await _context.Subscriptions
            .Where(s => s.Status == SubscriptionStatus.Active && s.NextBillingDate <= now)
            .ToListAsync();

        foreach (var subscription in subscriptions)
        {
            subscription.NextBillingDate = subscription.BillingCycle == BillingCycle.Monthly
                ? now.AddMonths(1)
                : now.AddYears(1);
            // Optionally: create invoice, charge payment, etc.
        }

        await _context.SaveChangesAsync();
    }

    public async Task ProcessTrialExpirationsAsync()
    {
        var now = DateTime.UtcNow;
        var subscriptions = await _context.Subscriptions
            .Where(s => s.Status == SubscriptionStatus.Trialing && s.TrialEndDate <= now)
            .ToListAsync();

        foreach (var subscription in subscriptions)
        {
            subscription.Status = SubscriptionStatus.Active;
            subscription.TrialEndDate = null;
        }

        await _context.SaveChangesAsync();
    }

    public async Task<bool> ReactivateSubscriptionAsync(Guid subscriptionId, Guid tenantId)
    {
        var subscription = await _context.Subscriptions
            .FirstOrDefaultAsync(s => s.Id == subscriptionId && s.TenantId == tenantId);

        if (subscription == null || subscription.Status != SubscriptionStatus.Cancelled)
            return false;

        subscription.Status = SubscriptionStatus.Active;
        subscription.EndDate = null;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdateSubscriptionAsync(Guid subscriptionId, UpdateSubscriptionRequest request, Guid tenantId)
    {
        var subscription = await _context.Subscriptions
            .FirstOrDefaultAsync(s => s.Id == subscriptionId && s.TenantId == tenantId);

        if (subscription == null)
            return false;

        if (request.PlanId.HasValue)
        {
            var plan = await _context.Plans.FirstOrDefaultAsync(p => p.Id == request.PlanId.Value && p.IsActive);
            if (plan == null)
                return false;
            subscription.PlanId = request.PlanId.Value;
            subscription.Amount = subscription.BillingCycle == BillingCycle.Monthly ? plan.MonthlyPrice : plan.YearlyPrice;
        }

        if (request.BillingCycle.HasValue)
        {
            subscription.BillingCycle = request.BillingCycle.Value;
            // Optionally update NextBillingDate
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpgradeSubscriptionAsync(Guid subscriptionId, Guid newPlanId, Guid tenantId)
    {
        var subscription = await _context.Subscriptions
            .FirstOrDefaultAsync(s => s.Id == subscriptionId && s.TenantId == tenantId);

        var plan = await _context.Plans.FirstOrDefaultAsync(p => p.Id == newPlanId && p.IsActive);
        if (subscription == null || plan == null)
            return false;

        subscription.PlanId = newPlanId;
        subscription.Amount = subscription.BillingCycle == BillingCycle.Monthly ? plan.MonthlyPrice : plan.YearlyPrice;
        await _context.SaveChangesAsync();
        return true;
    }
    public async Task<PreviewSubscriptionChangeResult> PreviewSubscriptionChangeAsync(Guid newPlanId, Guid tenantId, BillingCycle billingCycle)
    {
        // Fetch current active subscription
        var currentSubscription = await _context.Subscriptions
            .Include(s => s.Plan)
            .FirstOrDefaultAsync(s => s.TenantId == tenantId && s.Status == SubscriptionStatus.Active);

        var currentPlan = currentSubscription?.Plan;

        // Fetch new plan
        var newPlan = await _context.Plans.FirstOrDefaultAsync(p => p.Id == newPlanId);

        // Calculate proration and next billing amount (simple example)
        decimal prorationAmount = 0;
        decimal nextBillingAmount = newPlan != null
            ? (billingCycle == BillingCycle.Monthly ? newPlan.MonthlyPrice : newPlan.YearlyPrice)
            : 0;

        DateTime effectiveDate = DateTime.UtcNow;

        // TODO: Add real proration logic if needed

        return new PreviewSubscriptionChangeResult
        {
            CurrentPlan = currentPlan,
            NewPlan = newPlan,
            ProrationAmount = prorationAmount,
            NextBillingAmount = nextBillingAmount,
            EffectiveDate = effectiveDate
        };
    }
}
