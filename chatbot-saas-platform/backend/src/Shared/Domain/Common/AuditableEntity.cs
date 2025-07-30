using System.ComponentModel.DataAnnotations;

namespace Shared.Domain.Common;

public abstract class AuditableEntity : Entity
{
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    public Guid TenantId { get; set; }
}
