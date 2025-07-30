using Microsoft.EntityFrameworkCore;
using Shared.Application.Common.Interfaces;
using SubscriptionService.Services;
using SubscriptionService.Models;
using Shared.Domain.Entities;

namespace SubscriptionService.Services;

public class PlanService : IPlanService
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<PlanService> _logger;

    public PlanService(
        IApplicationDbContext context,
        ILogger<PlanService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<Plan>> GetPlansAsync(bool publicOnly = true)
    {
        try
        {
            var query = _context.Set<Plan>().AsQueryable();

            if (publicOnly)
            {
                query = query.Where(p => p.IsPublic && p.IsActive);
            }

            return await query
                .OrderBy(p => p.SortOrder)
                .ThenBy(p => p.MonthlyPrice)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting plans");
            throw;
        }
    }

    public async Task<Plan?> GetPlanAsync(Guid planId)
    {
        try
        {
            return await _context.Set<Plan>()
                .FirstOrDefaultAsync(p => p.Id == planId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting plan {PlanId}", planId);
            throw;
        }
    }

    public async Task<Plan> CreatePlanAsync(Plan plan)
    {
        try
        {
            plan.Id = Guid.NewGuid();
            plan.CreatedAt = DateTime.UtcNow;

            _context.Set<Plan>().Add(plan);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Plan {PlanId} created: {PlanName}", plan.Id, plan.Name);
            return plan;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating plan {PlanName}", plan.Name);
            throw;
        }
    }

    public async Task<bool> UpdatePlanAsync(Plan plan)
    {
        try
        {
            var existingPlan = await GetPlanAsync(plan.Id);
            if (existingPlan == null)
                return false;

            existingPlan.Name = plan.Name;
            existingPlan.Description = plan.Description;
            existingPlan.MonthlyPrice = plan.MonthlyPrice;
            existingPlan.YearlyPrice = plan.YearlyPrice;
            existingPlan.Currency = plan.Currency;
            existingPlan.IsActive = plan.IsActive;
            existingPlan.IsPublic = plan.IsPublic;
            existingPlan.Type = plan.Type;
            existingPlan.Features = plan.Features;
            existingPlan.Limits = plan.Limits;
            existingPlan.StripePriceIdMonthly = plan.StripePriceIdMonthly;
            existingPlan.StripePriceIdYearly = plan.StripePriceIdYearly;
            existingPlan.TrialDays = plan.TrialDays;
            existingPlan.SortOrder = plan.SortOrder;
            existingPlan.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating plan {PlanId}", plan.Id);
            return false;
        }
    }

    public async Task<bool> DeletePlanAsync(Guid planId)
    {
        try
        {
            var plan = await GetPlanAsync(planId);
            if (plan == null)
                return false;

            var hasActiveSubscriptions = await _context.Set<Subscription>()
                .AnyAsync(s => s.PlanId == planId && 
                    (s.Status == SubscriptionStatus.Active || s.Status == SubscriptionStatus.Trialing));

            if (hasActiveSubscriptions)
            {
                plan.IsActive = false;
                plan.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                _context.Set<Plan>().Remove(plan);
            }

            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting plan {PlanId}", planId);
            return false;
        }
    }

    public async Task<bool> CheckFeatureAccessAsync(Guid tenantId, string featureName)
    {
        try
        {
            var subscription = await _context.Set<Subscription>()
                .Include(s => s.Plan)
                .FirstOrDefaultAsync(s => s.TenantId == tenantId && 
                    (s.Status == SubscriptionStatus.Active || s.Status == SubscriptionStatus.Trialing));

            if (subscription == null)
                return false;

            if (subscription.Plan.Features.TryGetValue(featureName, out var feature))
            {
                return feature.IsEnabled;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking feature access for tenant {TenantId}, feature {FeatureName}", tenantId, featureName);
            return false;
        }
    }

    public async Task<int?> GetFeatureLimitAsync(Guid tenantId, string featureName)
    {
        try
        {
            var subscription = await _context.Set<Subscription>()
                .Include(s => s.Plan)
                .FirstOrDefaultAsync(s => s.TenantId == tenantId && 
                    (s.Status == SubscriptionStatus.Active || s.Status == SubscriptionStatus.Trialing));

            if (subscription == null)
                return null;

            if (subscription.Plan.Limits.TryGetValue(featureName, out var limit))
            {
                return limit;
            }

            if (subscription.Plan.Features.TryGetValue(featureName, out var feature) && feature.Limit.HasValue)
            {
                return feature.Limit.Value;
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting feature limit for tenant {TenantId}, feature {FeatureName}", tenantId, featureName);
            return null;
        }
    }

    public async Task<Dictionary<string, bool>> GetTenantFeaturesAsync(Guid tenantId)
    {
        try
        {
            var subscription = await _context.Set<Subscription>()
                .Include(s => s.Plan)
                .FirstOrDefaultAsync(s => s.TenantId == tenantId && 
                    (s.Status == SubscriptionStatus.Active || s.Status == SubscriptionStatus.Trialing));

            if (subscription == null)
                return new Dictionary<string, bool>();

            return subscription.Plan.Features.ToDictionary(
                kvp => kvp.Key, 
                kvp => kvp.Value.IsEnabled);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting tenant features for tenant {TenantId}", tenantId);
            return new Dictionary<string, bool>();
        }
    }
}
