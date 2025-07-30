using Shared.Domain.Common;
using Shared.Domain.Enums;

namespace Shared.Domain.Entities;

public class Tenant : AggregateRoot
{
    public string Name { get; set; } = string.Empty;
    public string Subdomain { get; set; } = string.Empty;
    public string? CustomDomain { get; set; }
    public string Domain { get; set; } = string.Empty;
    public TenantStatus Status { get; set; } = TenantStatus.Active;
    public string DatabaseConnectionString { get; set; } = string.Empty;
    public DateTime? TrialEndsAt { get; set; }
    public string? LogoUrl { get; set; }
    public string PrimaryColor { get; set; } = "#3B82F6";
    public string SecondaryColor { get; set; } = "#64748B";
    public string DefaultLanguage { get; set; } = "en";
    public bool IsRtlEnabled { get; set; } = false;
    public string SubscriptionPlan { get; set; } = "Starter";
    public int MaxUsers { get; set; } = 25;
    public int MaxAgents { get; set; } = 5;
    public int MaxConversations { get; set; } = 1000;
    public Dictionary<string, object> Settings { get; set; } = new Dictionary<string, object>();
    
    public ICollection<UserTenant> UserTenants { get; set; } = new List<UserTenant>();
    public ICollection<Conversation> Conversations { get; set; } = new List<Conversation>();
}
