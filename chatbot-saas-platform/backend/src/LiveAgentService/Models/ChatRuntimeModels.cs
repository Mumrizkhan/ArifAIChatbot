namespace LiveAgentService.Models;

public class ConversationDetailsDto
{
    public Guid Id { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerEmail { get; set; }
    public string? CustomerPhone { get; set; }
    public string? Subject { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Channel { get; set; }
    public string? Language { get; set; }
    public Guid TenantId { get; set; }
    public Guid? AssignedAgentId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int MessageCount { get; set; }
}

public class MessageDto
{
    public Guid Id { get; set; }
    public Guid ConversationId { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? Type { get; set; }
    public string? Sender { get; set; }
    public Guid? SenderId { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsRead { get; set; }
    public string? SenderName { get; set; }
}
