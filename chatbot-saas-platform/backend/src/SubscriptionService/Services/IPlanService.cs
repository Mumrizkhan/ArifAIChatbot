using Shared.Domain.Entities;
using SubscriptionService.Models;

namespace SubscriptionService.Services;

public interface IPlanService
{
    Task<List<Plan>> GetPlansAsync(bool publicOnly = true);
    Task<Plan?> GetPlanAsync(Guid planId);
    Task<Plan> CreatePlanAsync(Plan plan);
    Task<bool> UpdatePlanAsync(Plan plan);
    Task<bool> DeletePlanAsync(Guid planId);
    Task<bool> CheckFeatureAccessAsync(Guid tenantId, string featureName);
    Task<int?> GetFeatureLimitAsync(Guid tenantId, string featureName);
    Task<Dictionary<string, bool>> GetTenantFeaturesAsync(Guid tenantId);
}
