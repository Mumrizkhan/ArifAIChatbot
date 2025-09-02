using Shared.Domain.Common;
using Shared.Domain.Enums;

namespace Shared.Domain.Entities;

public class ConversationAssignment : AuditableEntity
{
    public Guid Id { get; set; }
    public Guid ConversationId { get; set; }
    public Guid AgentId { get; set; }
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UnassignedAt { get; set; }
    public ConversationStatus Status { get; set; } = ConversationStatus.Active;
    public ConversationPriority Priority { get; set; } = ConversationPriority.Normal;
    public string? CustomerName { get; set; }
    public string? Subject { get; set; }
    public DateTime? LastMessageAt { get; set; }
    public int UnreadMessages { get; set; } = 0;
    public Guid TenantId { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public Conversation? Conversation { get; set; }
    public User? Agent { get; set; }
    public Tenant? Tenant { get; set; }
}