using Shared.Domain.Common;
using Shared.Domain.Enums;

namespace Shared.Domain.Entities;

public class UserTenant : AuditableEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;
    
    public TenantRole Role { get; set; } = TenantRole.Member;
    public bool IsActive { get; set; } = true;
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
}
