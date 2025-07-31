using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Application.Common.Interfaces;
using SubscriptionService.Services;
using SubscriptionService.Models;

namespace SubscriptionService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SubscriptionsController : ControllerBase
{
    private readonly ISubscriptionService _subscriptionService;
    private readonly IBillingService _billingService;
    private readonly IUsageTrackingService _usageTrackingService;
    private readonly IPaymentService _paymentService;
    private readonly IPlanService _planService;
    private readonly ITenantService _tenantService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<SubscriptionsController> _logger;

    public SubscriptionsController(
        ISubscriptionService subscriptionService,
        IBillingService billingService,
        IUsageTrackingService usageTrackingService,
        IPaymentService paymentService,
        IPlanService planService,
        ITenantService tenantService,
        ICurrentUserService currentUserService,
        ILogger<SubscriptionsController> logger)
    {
        _subscriptionService = subscriptionService;
        _billingService = billingService;
        _usageTrackingService = usageTrackingService;
        _paymentService = paymentService;
        _planService = planService;
        _tenantService = tenantService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> CreateSubscription([FromBody] CreateSubscriptionRequest request)
    {
        try
        {
            var tenantId = _tenantService.GetCurrentTenantId();
            var subscription = await _subscriptionService.CreateSubscriptionAsync(request, tenantId);

            return Ok(new
            {
                subscription.Id,
                subscription.PlanId,
                Status = subscription.Status.ToString(),
                subscription.StartDate,
                subscription.NextBillingDate,
                subscription.Amount,
                subscription.Currency,
                IsTrialActive = subscription.IsTrialActive,
                subscription.TrialEndDate,
                Message = "Subscription created successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating subscription");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetSubscriptions()
    {
        try
        {
            var tenantId = _tenantService.GetCurrentTenantId();
            var subscriptions = await _subscriptionService.GetSubscriptionsAsync(tenantId);

            return Ok(subscriptions.Select(s => new
            {
                s.Id,
                s.PlanId,
                PlanName = s.Plan.Name,
                Status = s.Status.ToString(),
                s.StartDate,
                s.EndDate,
                s.NextBillingDate,
                BillingCycle = s.BillingCycle.ToString(),
                s.Amount,
                s.Currency,
                IsTrialActive = s.IsTrialActive,
                s.TrialEndDate,
                s.CreatedAt
            }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting subscriptions");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("active")]
    public async Task<IActionResult> GetActiveSubscription()
    {
        try
        {
            var tenantId = _tenantService.GetCurrentTenantId();
            var subscription = await _subscriptionService.GetActiveSubscriptionAsync(tenantId);

            if (subscription == null)
            {
                return NotFound(new { message = "No active subscription found" });
            }

            return Ok(new
            {
                subscription.Id,
                subscription.PlanId,
                PlanName = subscription.Plan.Name,
                PlanType = subscription.Plan.Type.ToString(),
                Status = subscription.Status.ToString(),
                subscription.StartDate,
                subscription.EndDate,
                subscription.NextBillingDate,
                BillingCycle = subscription.BillingCycle.ToString(),
                subscription.Amount,
                subscription.Currency,
                IsTrialActive = subscription.IsTrialActive,
                subscription.TrialEndDate,
                Features = subscription.Plan.Features,
                Limits = subscription.Plan.Limits
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active subscription");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("{subscriptionId}")]
    public async Task<IActionResult> GetSubscription(Guid subscriptionId)
    {
        try
        {
            var tenantId = _tenantService.GetCurrentTenantId();
            var subscription = await _subscriptionService.GetSubscriptionAsync(subscriptionId, tenantId);

            if (subscription == null)
            {
                return NotFound(new { message = "Subscription not found" });
            }

            return Ok(new
            {
                subscription.Id,
                subscription.PlanId,
                PlanName = subscription.Plan.Name,
                Status = subscription.Status.ToString(),
                subscription.StartDate,
                subscription.EndDate,
                subscription.NextBillingDate,
                BillingCycle = subscription.BillingCycle.ToString(),
                subscription.Amount,
                subscription.Currency,
                IsTrialActive = subscription.IsTrialActive,
                subscription.TrialEndDate,
                subscription.Metadata,
                InvoiceCount = subscription.Invoices.Count,
                subscription.CreatedAt,
                subscription.UpdatedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting subscription {SubscriptionId}", subscriptionId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPut("{subscriptionId}")]
    public async Task<IActionResult> UpdateSubscription(Guid subscriptionId, [FromBody] UpdateSubscriptionRequest request)
    {
        try
        {
            var tenantId = _tenantService.GetCurrentTenantId();
            var updated = await _subscriptionService.UpdateSubscriptionAsync(subscriptionId, request, tenantId);

            if (!updated)
            {
                return NotFound(new { message = "Subscription not found" });
            }

            return Ok(new { message = "Subscription updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating subscription {SubscriptionId}", subscriptionId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost("{subscriptionId}/cancel")]
    public async Task<IActionResult> CancelSubscription(Guid subscriptionId, [FromQuery] bool immediately = false)
    {
        try
        {
            var tenantId = _tenantService.GetCurrentTenantId();
            var cancelled = await _subscriptionService.CancelSubscriptionAsync(subscriptionId, tenantId, immediately);

            if (!cancelled)
            {
                return NotFound(new { message = "Subscription not found" });
            }

            return Ok(new { message = "Subscription cancelled successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling subscription {SubscriptionId}", subscriptionId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost("{subscriptionId}/reactivate")]
    public async Task<IActionResult> ReactivateSubscription(Guid subscriptionId)
    {
        try
        {
            var tenantId = _tenantService.GetCurrentTenantId();
            var reactivated = await _subscriptionService.ReactivateSubscriptionAsync(subscriptionId, tenantId);

            if (!reactivated)
            {
                return NotFound(new { message = "Subscription not found or cannot be reactivated" });
            }

            return Ok(new { message = "Subscription reactivated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reactivating subscription {SubscriptionId}", subscriptionId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost("{subscriptionId}/upgrade")]
    public async Task<IActionResult> UpgradeSubscription(Guid subscriptionId, [FromBody] UpgradeSubscriptionRequest request)
    {
        try
        {
            var tenantId = _tenantService.GetCurrentTenantId();
            var upgraded = await _subscriptionService.UpgradeSubscriptionAsync(subscriptionId, request.NewPlanId, tenantId);

            if (!upgraded)
            {
                return NotFound(new { message = "Subscription not found or cannot be upgraded" });
            }

            return Ok(new { message = "Subscription upgraded successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error upgrading subscription {SubscriptionId}", subscriptionId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost("{subscriptionId}/downgrade")]
    public async Task<IActionResult> DowngradeSubscription(Guid subscriptionId, [FromBody] DowngradeSubscriptionRequest request)
    {
        try
        {
            var tenantId = _tenantService.GetCurrentTenantId();
            var downgraded = await _subscriptionService.DowngradeSubscriptionAsync(subscriptionId, request.NewPlanId, tenantId);

            if (!downgraded)
            {
                return NotFound(new { message = "Subscription not found or cannot be downgraded" });
            }

            return Ok(new { message = "Subscription downgrade scheduled for next billing cycle" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downgrading subscription {SubscriptionId}", subscriptionId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("{subscriptionId}/invoices")]
    public async Task<IActionResult> GetSubscriptionInvoices(Guid subscriptionId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        try
        {
            var tenantId = _tenantService.GetCurrentTenantId();
            var invoices = await _billingService.GetInvoicesAsync(tenantId, page, pageSize);

            return Ok(invoices.Where(i => i.SubscriptionId == subscriptionId).Select(i => new
            {
                i.Id,
                i.InvoiceNumber,
                Status = i.Status.ToString(),
                i.Subtotal,
                i.TaxAmount,
                i.DiscountAmount,
                i.Total,
                i.Currency,
                i.IssueDate,
                i.DueDate,
                i.PaidDate,
                i.PeriodStart,
                i.PeriodEnd
            }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting invoices for subscription {SubscriptionId}", subscriptionId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("usage")]
    public async Task<IActionResult> GetUsage([FromQuery] string? metricName, [FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
    {
        try
        {
            var tenantId = _tenantService.GetCurrentTenantId();
            var usage = await _usageTrackingService.GetUsageAsync(tenantId, metricName, startDate, endDate);

            return Ok(usage.Select(u => new
            {
                u.Id,
                u.MetricName,
                u.Quantity,
                u.RecordedAt,
                u.PeriodStart,
                u.PeriodEnd,
                u.Metadata
            }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting usage");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("usage/summary")]
    public async Task<IActionResult> GetUsageSummary([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
    {
        try
        {
            var tenantId = _tenantService.GetCurrentTenantId();
            var summary = await _usageTrackingService.GetUsageSummaryAsync(tenantId, startDate, endDate);

            return Ok(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting usage summary");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost("usage")]
    public async Task<IActionResult> RecordUsage([FromBody] RecordUsageRequest request)
    {
        try
        {
            var tenantId = _tenantService.GetCurrentTenantId();
            await _usageTrackingService.RecordUsageAsync(tenantId, request.MetricName, request.Quantity, request.Metadata);

            return Ok(new { message = "Usage recorded successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording usage");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("features")]
    public async Task<IActionResult> GetTenantFeatures()
    {
        try
        {
            var tenantId = _tenantService.GetCurrentTenantId();
            var features = await _planService.GetTenantFeaturesAsync(tenantId);

            return Ok(features);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting tenant features");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("features/{featureName}/check")]
    public async Task<IActionResult> CheckFeatureAccess(string featureName)
    {
        try
        {
            var tenantId = _tenantService.GetCurrentTenantId();
            var hasAccess = await _planService.CheckFeatureAccessAsync(tenantId, featureName);

            return Ok(new { featureName, hasAccess });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking feature access for {FeatureName}", featureName);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("statistics")]
    public async Task<IActionResult> GetBillingStatistics([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
    {
        try
        {
            var statistics = await _subscriptionService.GetBillingStatisticsAsync(startDate, endDate);
            return Ok(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting billing statistics");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost("apply-coupon")]
    public async Task<IActionResult> ApplyCoupon([FromBody] ApplyCouponRequest request)
    {
        try
        {
            var tenantId = _tenantService.GetCurrentTenantId();
            var result = await _billingService.ApplyCouponAsync(request.CouponCode, tenantId);

            if (result.IsValid)
            {
                return Ok(new
                {
                    CouponCode = request.CouponCode,
                    DiscountAmount = result.DiscountAmount,
                    DiscountPercentage = result.DiscountPercentage,
                    Message = "Coupon applied successfully"
                });
            }

            return BadRequest(new { message = result.ErrorMessage ?? "Invalid coupon code" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying coupon");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("tax-rates")]
    public async Task<IActionResult> GetTaxRates([FromQuery] string? country, [FromQuery] string? state)
    {
        try
        {
            var taxRates = await _billingService.GetTaxRatesAsync(country, state);
            return Ok(taxRates);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting tax rates");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost("preview-change")]
    public async Task<IActionResult> PreviewSubscriptionChange([FromBody] PreviewChangeRequest request)
    {
        try
        {
            var tenantId = _tenantService.GetCurrentTenantId();
            var preview = await _subscriptionService.PreviewSubscriptionChangeAsync(
                request.NewPlanId, tenantId, request.BillingCycle);

            return Ok(new
            {
                CurrentPlan = preview.CurrentPlan,
                NewPlan = preview.NewPlan,
                ProrationAmount = preview.ProrationAmount,
                NextBillingAmount = preview.NextBillingAmount,
                EffectiveDate = preview.EffectiveDate
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error previewing subscription change");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }
}

public class ApplyCouponRequest
{
    public string CouponCode { get; set; } = string.Empty;
}

public class PreviewChangeRequest
{
    public Guid NewPlanId { get; set; }
    public string BillingCycle { get; set; } = "monthly";
}

public class UpgradeSubscriptionRequest
{
    public Guid NewPlanId { get; set; }
}

public class DowngradeSubscriptionRequest
{
    public Guid NewPlanId { get; set; }
}

public class RecordUsageRequest
{
    public string MetricName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}
