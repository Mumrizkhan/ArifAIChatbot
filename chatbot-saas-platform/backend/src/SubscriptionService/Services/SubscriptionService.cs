using Microsoft.EntityFrameworkCore;
using Shared.Application.Common.Interfaces;
using SubscriptionService.Services;
using SubscriptionService.Models;
using Hangfire;
using Shared.Domain.Entities;

namespace SubscriptionService.Services;

public class SubscriptionService : ISubscriptionService
{
    private readonly IApplicationDbContext _context;
    private readonly IPaymentService _paymentService;
    private readonly IBillingService _billingService;
    private readonly IPlanService _planService;
    private readonly ILogger<SubscriptionService> _logger;

    public SubscriptionService(
        IApplicationDbContext context,
        IPaymentService paymentService,
        IBillingService billingService,
        IPlanService planService,
        ILogger<SubscriptionService> logger)
    {
        _context = context;
        _paymentService = paymentService;
        _billingService = billingService;
        _planService = planService;
        _logger = logger;
    }

    public async Task<Subscription> CreateSubscriptionAsync(CreateSubscriptionRequest request, Guid tenantId)
    {
        try
        {
            var plan = await _planService.GetPlanAsync(request.PlanId);
            if (plan == null)
                throw new ArgumentException("Plan not found");

            var existingSubscription = await GetActiveSubscriptionAsync(tenantId);
            if (existingSubscription != null)
                throw new InvalidOperationException("Tenant already has an active subscription");

            var subscription = new Subscription
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                PlanId = request.PlanId,
                Status = request.StartTrial && plan.TrialDays > 0 ? SubscriptionStatus.Trialing : SubscriptionStatus.Active,
                StartDate = DateTime.UtcNow,
                BillingCycle = request.BillingCycle,
                Amount = request.BillingCycle == BillingCycle.Monthly ? plan.MonthlyPrice : plan.YearlyPrice,
                Currency = plan.Currency,
                Metadata = request.Metadata,
                TrialDays = request.StartTrial ? plan.TrialDays : 0,
                Plan = plan,
                CreatedAt = DateTime.UtcNow
            };

            if (request.StartTrial && plan.TrialDays > 0)
            {
                subscription.TrialEndDate = DateTime.UtcNow.AddDays(plan.TrialDays);
                subscription.NextBillingDate = subscription.TrialEndDate.Value;
            }
            else
            {
                subscription.NextBillingDate = request.BillingCycle == BillingCycle.Monthly 
                    ? DateTime.UtcNow.AddMonths(1) 
                    : DateTime.UtcNow.AddYears(1);
            }

            if (!string.IsNullOrEmpty(request.PaymentMethodId))
            {
                var stripeCustomerId = await _paymentService.CreateCustomerAsync(tenantId, "", "");
                subscription.StripeCustomerId = stripeCustomerId;
                
                var stripeService = new Stripe.SubscriptionService();
                var stripeSubscription = await stripeService.CreateAsync(new Stripe.SubscriptionCreateOptions
                {
                    Customer = stripeCustomerId,
                    Items = new List<Stripe.SubscriptionItemOptions>
                    {
                        new Stripe.SubscriptionItemOptions
                        {
                            Price = request.BillingCycle == BillingCycle.Monthly 
                                ? plan.StripePriceIdMonthly 
                                : plan.StripePriceIdYearly
                        }
                    },
                    DefaultPaymentMethod = request.PaymentMethodId,
                    TrialPeriodDays = request.StartTrial ? plan.TrialDays : null,
                    Metadata = request.Metadata.ToDictionary(kvp => kvp.Key, kvp => kvp.Value?.ToString() ?? "")
                });

                subscription.StripeSubscriptionId = stripeSubscription.Id;
            }

            _context.Set<Subscription>().Add(subscription);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Subscription {SubscriptionId} created for tenant {TenantId}", subscription.Id, tenantId);
            return subscription;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating subscription for tenant {TenantId}", tenantId);
            throw;
        }
    }

    public async Task<Subscription?> GetSubscriptionAsync(Guid subscriptionId, Guid tenantId)
    {
        try
        {
            return await _context.Set<Subscription>()
                .Include(s => s.Plan)
                .Include(s => s.Invoices)
                .FirstOrDefaultAsync(s => s.Id == subscriptionId && s.TenantId == tenantId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting subscription {SubscriptionId}", subscriptionId);
            throw;
        }
    }

    public async Task<Subscription?> GetActiveSubscriptionAsync(Guid tenantId)
    {
        try
        {
            return await _context.Set<Subscription>()
                .Include(s => s.Plan)
                .FirstOrDefaultAsync(s => s.TenantId == tenantId && 
                    (s.Status == SubscriptionStatus.Active || s.Status == SubscriptionStatus.Trialing));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active subscription for tenant {TenantId}", tenantId);
            throw;
        }
    }

    public async Task<List<Subscription>> GetSubscriptionsAsync(Guid tenantId)
    {
        try
        {
            return await _context.Set<Subscription>()
                .Include(s => s.Plan)
                .Where(s => s.TenantId == tenantId)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting subscriptions for tenant {TenantId}", tenantId);
            throw;
        }
    }

    public async Task<bool> UpdateSubscriptionAsync(Guid subscriptionId, UpdateSubscriptionRequest request, Guid tenantId)
    {
        try
        {
            var subscription = await GetSubscriptionAsync(subscriptionId, tenantId);
            if (subscription == null)
                return false;

            if (request.PlanId.HasValue && request.PlanId != subscription.PlanId)
            {
                var newPlan = await _planService.GetPlanAsync(request.PlanId.Value);
                if (newPlan == null)
                    throw new ArgumentException("New plan not found");

                subscription.PlanId = request.PlanId.Value;
                subscription.Plan = newPlan;
                subscription.Amount = request.BillingCycle == BillingCycle.Monthly ? newPlan.MonthlyPrice : newPlan.YearlyPrice;
            }

            if (request.BillingCycle.HasValue)
            {
                subscription.BillingCycle = request.BillingCycle.Value;
                subscription.Amount = request.BillingCycle == BillingCycle.Monthly 
                    ? subscription.Plan.MonthlyPrice 
                    : subscription.Plan.YearlyPrice;
            }

            if (request.Metadata.Any())
            {
                foreach (var kvp in request.Metadata)
                {
                    subscription.Metadata[kvp.Key] = kvp.Value;
                }
            }

            subscription.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating subscription {SubscriptionId}", subscriptionId);
            return false;
        }
    }

    public async Task<bool> CancelSubscriptionAsync(Guid subscriptionId, Guid tenantId, bool immediately = false)
    {
        try
        {
            var subscription = await GetSubscriptionAsync(subscriptionId, tenantId);
            if (subscription == null)
                return false;

            if (immediately)
            {
                subscription.Status = SubscriptionStatus.Cancelled;
                subscription.EndDate = DateTime.UtcNow;
            }
            else
            {
                subscription.Status = SubscriptionStatus.Cancelled;
                subscription.EndDate = subscription.NextBillingDate;
            }

            if (!string.IsNullOrEmpty(subscription.StripeSubscriptionId))
            {
                var stripeService = new Stripe.SubscriptionService();
                await stripeService.CancelAsync(subscription.StripeSubscriptionId, new Stripe.SubscriptionCancelOptions
                {
                    InvoiceNow = immediately,
                    Prorate = !immediately
                });
            }

            subscription.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling subscription {SubscriptionId}", subscriptionId);
            return false;
        }
    }

    public async Task<bool> ReactivateSubscriptionAsync(Guid subscriptionId, Guid tenantId)
    {
        try
        {
            var subscription = await GetSubscriptionAsync(subscriptionId, tenantId);
            if (subscription == null || subscription.Status != SubscriptionStatus.Cancelled)
                return false;

            subscription.Status = SubscriptionStatus.Active;
            subscription.EndDate = null;
            subscription.NextBillingDate = subscription.BillingCycle == BillingCycle.Monthly 
                ? DateTime.UtcNow.AddMonths(1) 
                : DateTime.UtcNow.AddYears(1);
            subscription.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reactivating subscription {SubscriptionId}", subscriptionId);
            return false;
        }
    }

    public async Task<bool> UpgradeSubscriptionAsync(Guid subscriptionId, Guid newPlanId, Guid tenantId)
    {
        try
        {
            var subscription = await GetSubscriptionAsync(subscriptionId, tenantId);
            if (subscription == null)
                return false;

            var newPlan = await _planService.GetPlanAsync(newPlanId);
            if (newPlan == null)
                return false;

            var oldAmount = subscription.Amount;
            subscription.PlanId = newPlanId;
            subscription.Plan = newPlan;
            subscription.Amount = subscription.BillingCycle == BillingCycle.Monthly 
                ? newPlan.MonthlyPrice 
                : newPlan.YearlyPrice;

            var daysRemaining = (subscription.NextBillingDate - DateTime.UtcNow).Days;
            var totalDays = subscription.BillingCycle == BillingCycle.Monthly ? 30 : 365;
            var proratedAmount = (subscription.Amount - oldAmount) * daysRemaining / totalDays;

            if (proratedAmount > 0)
            {
                var invoice = await _billingService.CreateInvoiceAsync(subscriptionId, DateTime.UtcNow, subscription.NextBillingDate);
            }

            subscription.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error upgrading subscription {SubscriptionId}", subscriptionId);
            return false;
        }
    }

    public async Task<bool> DowngradeSubscriptionAsync(Guid subscriptionId, Guid newPlanId, Guid tenantId)
    {
        try
        {
            var subscription = await GetSubscriptionAsync(subscriptionId, tenantId);
            if (subscription == null)
                return false;

            var newPlan = await _planService.GetPlanAsync(newPlanId);
            if (newPlan == null)
                return false;

            subscription.Metadata["scheduled_plan_change"] = newPlanId.ToString();
            subscription.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downgrading subscription {SubscriptionId}", subscriptionId);
            return false;
        }
    }

    public async Task ProcessSubscriptionRenewalsAsync()
    {
        try
        {
            var subscriptionsToRenew = await _context.Set<Subscription>()
                .Where(s => s.Status == SubscriptionStatus.Active && 
                           s.NextBillingDate <= DateTime.UtcNow)
                .ToListAsync();

            foreach (var subscription in subscriptionsToRenew)
            {
                try
                {
                    var periodStart = subscription.NextBillingDate;
                    var periodEnd = subscription.BillingCycle == BillingCycle.Monthly 
                        ? periodStart.AddMonths(1) 
                        : periodStart.AddYears(1);

                    var invoice = await _billingService.CreateInvoiceAsync(subscription.Id, periodStart, periodEnd);
                    
                    var paymentSuccessful = await _billingService.PayInvoiceAsync(invoice.Id, subscription.TenantId);
                    
                    if (paymentSuccessful)
                    {
                        subscription.NextBillingDate = periodEnd;
                        subscription.UpdatedAt = DateTime.UtcNow;
                    }
                    else
                    {
                        subscription.Status = SubscriptionStatus.PastDue;
                        subscription.UpdatedAt = DateTime.UtcNow;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing renewal for subscription {SubscriptionId}", subscription.Id);
                    subscription.Status = SubscriptionStatus.PastDue;
                    subscription.UpdatedAt = DateTime.UtcNow;
                }
            }

            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing subscription renewals");
        }
    }

    public async Task ProcessTrialExpirationsAsync()
    {
        try
        {
            var expiredTrials = await _context.Set<Subscription>()
                .Where(s => s.Status == SubscriptionStatus.Trialing && 
                           s.TrialEndDate <= DateTime.UtcNow)
                .ToListAsync();

            foreach (var subscription in expiredTrials)
            {
                try
                {
                    var paymentMethod = await _paymentService.GetDefaultPaymentMethodAsync(subscription.TenantId);
                    
                    if (paymentMethod != null)
                    {
                        subscription.Status = SubscriptionStatus.Active;
                        subscription.NextBillingDate = subscription.BillingCycle == BillingCycle.Monthly 
                            ? DateTime.UtcNow.AddMonths(1) 
                            : DateTime.UtcNow.AddYears(1);
                    }
                    else
                    {
                        subscription.Status = SubscriptionStatus.Incomplete;
                    }

                    subscription.UpdatedAt = DateTime.UtcNow;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing trial expiration for subscription {SubscriptionId}", subscription.Id);
                }
            }

            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing trial expirations");
        }
    }

    public async Task<BillingStatistics> GetBillingStatisticsAsync(DateTime? startDate = null, DateTime? endDate = null)
    {
        try
        {
            var start = startDate ?? DateTime.UtcNow.AddDays(-30);
            var end = endDate ?? DateTime.UtcNow;

            var subscriptions = await _context.Set<Subscription>()
                .Include(s => s.Plan)
                .Where(s => s.CreatedAt >= start && s.CreatedAt <= end)
                .ToListAsync();

            var invoices = await _context.Set<Invoice>()
                .Where(i => i.IssueDate >= start && i.IssueDate <= end && i.Status == InvoiceStatus.Paid)
                .ToListAsync();

            var activeSubscriptions = subscriptions.Count(s => s.Status == SubscriptionStatus.Active);
            var trialSubscriptions = subscriptions.Count(s => s.Status == SubscriptionStatus.Trialing);
            var cancelledSubscriptions = subscriptions.Count(s => s.Status == SubscriptionStatus.Cancelled);

            var mrr = subscriptions
                .Where(s => s.Status == SubscriptionStatus.Active && s.BillingCycle == BillingCycle.Monthly)
                .Sum(s => s.Amount);

            var arr = subscriptions
                .Where(s => s.Status == SubscriptionStatus.Active)
                .Sum(s => s.BillingCycle == BillingCycle.Monthly ? s.Amount * 12 : s.Amount);

            var totalSubscriptions = activeSubscriptions + trialSubscriptions;
            var churnRate = totalSubscriptions > 0 ? (double)cancelledSubscriptions / totalSubscriptions * 100 : 0;
            var arpu = totalSubscriptions > 0 ? invoices.Sum(i => i.Total) / totalSubscriptions : 0;

            return new BillingStatistics
            {
                MonthlyRecurringRevenue = mrr,
                AnnualRecurringRevenue = arr,
                ActiveSubscriptions = activeSubscriptions,
                TrialSubscriptions = trialSubscriptions,
                CancelledSubscriptions = cancelledSubscriptions,
                ChurnRate = (decimal)churnRate,
                AverageRevenuePerUser = arpu,
                SubscriptionsByPlan = subscriptions.GroupBy(s => s.Plan.Name).ToDictionary(g => g.Key, g => g.Count()),
                RevenueByPlan = subscriptions.GroupBy(s => s.Plan.Name).ToDictionary(g => g.Key, g => g.Sum(s => s.Amount)),
                PeriodStart = start,
                PeriodEnd = end
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting billing statistics");
            throw;
        }
    }
}
