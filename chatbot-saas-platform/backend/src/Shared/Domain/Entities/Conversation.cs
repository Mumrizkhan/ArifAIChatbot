using Shared.Domain.Common;
using Shared.Domain.Enums;

namespace Shared.Domain.Entities;

public class Conversation : AggregateRoot
{
    public Guid TenantId { get; set; }
    public Guid? UserId { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerEmail { get; set; }
    public string? CustomerPhone { get; set; }
    public ConversationStatus Status { get; set; } = ConversationStatus.Active;
    public ConversationChannel Channel { get; set; } = ConversationChannel.Widget;
    public string? Subject { get; set; }
    public DateTime? EndedAt { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public Guid? AssignedAgentId { get; set; }
    public User? AssignedAgent { get; set; }
    public User? User { get; set; }
    public Tenant Tenant { get; set; } = null!;
    public string Language { get; set; } = "en";
    public int MessageCount { get; set; } = 0;
    public TimeSpan? ResponseTime { get; set; }
    public int? CustomerSatisfactionRating { get; set; }
    public string? CustomerFeedback { get; set; }
    public int? Rating { get; set; }
    public List<string> Tags { get; set; } = new List<string>();
    
    public ICollection<Message> Messages { get; set; } = new List<Message>();
    public int? AverageResponseTime { get; set; }
    public int? Priority { get; set; }
    public Guid ChatbotConfigId { get; set; }
    public DateTime AssignedAt { get; set; }
    public DateTime EscalatedAt { get; set; }
    public DateTime? LastMessageAt { get; set; }
}
