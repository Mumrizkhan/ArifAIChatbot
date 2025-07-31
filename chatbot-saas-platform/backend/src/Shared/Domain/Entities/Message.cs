using Shared.Domain.Common;
using Shared.Domain.Enums;

namespace Shared.Domain.Entities;

public class Message : AuditableEntity
{
    public Guid ConversationId { get; set; }
    public Guid TenantId { get; set; }
    public Conversation Conversation { get; set; } = null!;
    public Tenant Tenant { get; set; } = null!;
    
    public string Content { get; set; } = string.Empty;
    public MessageType Type { get; set; } = MessageType.Text;
    public MessageSender Sender { get; set; } = MessageSender.Customer;
    public Guid? SenderId { get; set; }
    public User? SenderUser { get; set; }
    public bool IsRead { get; set; } = false;
    public DateTime? ReadAt { get; set; }
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
    public string? Metadata { get; set; }
    public string? AttachmentUrl { get; set; }
    public string? AttachmentType { get; set; }
    public string SenderType { get; set; }
}
