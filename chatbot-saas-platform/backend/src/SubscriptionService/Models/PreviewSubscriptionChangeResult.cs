using Shared.Domain.Entities;

public class PreviewSubscriptionChangeResult
{
    public Plan? CurrentPlan { get; set; }
    public Plan? NewPlan { get; set; }
    public decimal ProrationAmount { get; set; }
    public decimal NextBillingAmount { get; set; }
    public DateTime EffectiveDate { get; set; }
}