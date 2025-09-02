using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SubscriptionService.Services;
using SubscriptionService.Models;
using Shared.Domain.Entities;

namespace SubscriptionService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PlansController : ControllerBase
{
    private readonly IPlanService _planService;
    private readonly ILogger<PlansController> _logger;

    public PlansController(
        IPlanService planService,
        ILogger<PlansController> logger)
    {
        _planService = planService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetPlans([FromQuery] bool publicOnly = true)
    {
        try
        {
            var plans = await _planService.GetPlansAsync(publicOnly);

            return Ok(plans.Select(p => new
            {
                p.Id,
                p.Name,
                p.Description,
                p.MonthlyPrice,
                p.YearlyPrice,
                p.Currency,
                Type = p.Type.ToString(),
                p.Features,
                p.Limits,
                p.TrialDays,
                p.IsActive,
                p.IsPublic,
                p.SortOrder
            }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting plans");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("{planId}")]
    public async Task<IActionResult> GetPlan(Guid planId)
    {
        try
        {
            var plan = await _planService.GetPlanAsync(planId);

            if (plan == null)
            {
                return NotFound(new { message = "Plan not found" });
            }

            return Ok(new
            {
                plan.Id,
                plan.Name,
                plan.Description,
                plan.MonthlyPrice,
                plan.YearlyPrice,
                plan.Currency,
                Type = plan.Type.ToString(),
                plan.Features,
                plan.Limits,
                plan.StripePriceIdMonthly,
                plan.StripePriceIdYearly,
                plan.TrialDays,
                plan.IsActive,
                plan.IsPublic,
                plan.SortOrder,
                plan.CreatedAt,
                plan.UpdatedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting plan {PlanId}", planId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreatePlan([FromBody] Plan plan)
    {
        try
        {
            var createdPlan = await _planService.CreatePlanAsync(plan);

            return Ok(new
            {
                createdPlan.Id,
                createdPlan.Name,
                createdPlan.Description,
                createdPlan.MonthlyPrice,
                createdPlan.YearlyPrice,
                createdPlan.Currency,
                Type = createdPlan.Type.ToString(),
                createdPlan.Features,
                createdPlan.Limits,
                createdPlan.StripePriceIdMonthly,
                createdPlan.StripePriceIdYearly,
                createdPlan.TrialDays,
                createdPlan.IsActive,
                createdPlan.IsPublic,
                createdPlan.SortOrder,
                createdPlan.CreatedAt,
                createdPlan.UpdatedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating plan");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPut("{planId}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdatePlan(Guid planId, [FromBody] Plan plan)
    {
        try
        {
            plan.Id = planId;
            var updated = await _planService.UpdatePlanAsync(plan);

            if (!updated)
            {
                return NotFound(new { message = "Plan not found" });
            }

            return Ok(new { message = "Plan updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating plan {PlanId}", planId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpDelete("{planId}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeletePlan(Guid planId)
    {
        try
        {
            var deleted = await _planService.DeletePlanAsync(planId);

            if (!deleted)
            {
                return NotFound(new { message = "Plan not found" });
            }

            return Ok(new { message = "Plan deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting plan {PlanId}", planId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }
}
