namespace TenantManagementService.Models;

public class SystemSettingsDto
{
    public Dictionary<string, object> SystemSettings { get; set; } = new();
    public Dictionary<string, object> NotificationSettings { get; set; } = new();
    public Dictionary<string, object> IntegrationSettings { get; set; } = new();
    public DateTime LastUpdated { get; set; }
    public string? UpdatedBy { get; set; }
}

public class UpdateSystemSettingsRequest
{
    public Dictionary<string, object>? SystemSettings { get; set; }
    public Dictionary<string, object>? NotificationSettings { get; set; }
    public Dictionary<string, object>? IntegrationSettings { get; set; }
}
