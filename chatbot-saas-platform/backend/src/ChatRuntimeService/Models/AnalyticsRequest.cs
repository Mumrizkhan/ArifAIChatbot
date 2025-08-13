namespace ChatRuntimeService.Models;

public class AnalyticsRequest
{
    public string? DateFrom { get; set; }
    public string? DateTo { get; set; }
    public string? TenantId { get; set; }
    public string? MetricType { get; set; }
}