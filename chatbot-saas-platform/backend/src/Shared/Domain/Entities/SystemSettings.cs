using Shared.Domain.Common;

namespace Shared.Domain.Entities;

public class SystemSettings : AuditableEntity
{
    public Dictionary<string, object>? Settings { get; set; } = new Dictionary<string, object>();
    public string Category { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
