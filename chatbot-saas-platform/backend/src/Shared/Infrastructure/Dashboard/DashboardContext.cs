namespace Shared.Infrastructure.Dashboard;

public class DashboardContext
{
    public string? UserId { get; set; }
    public string? TenantId { get; set; }
    public Dictionary<string, object> Properties { get; set; } = new();
}
