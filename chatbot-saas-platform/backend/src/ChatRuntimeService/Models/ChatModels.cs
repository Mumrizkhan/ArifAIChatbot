namespace ChatRuntimeService.Models;

public class ConversationDto
{
    public Guid Id { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerEmail { get; set; }
    public string? Subject { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Channel { get; set; }
    public string? Language { get; set; }
    public Guid TenantId { get; set; }
    public Guid? UserId { get; set; }
    public Guid? AssignedAgentId { get; set; }
    public string CreatedAt { get; set; } = string.Empty; // ISO string format
    public string? UpdatedAt { get; set; } // ISO string format
    public int MessageCount { get; set; }
    public List<MessageDto> Messages { get; set; } = new();
}

public class ConversationDetailDto : ConversationDto
{
    public string? CustomerPhone { get; set; }
    public string? EndedAt { get; set; } // ISO string format
    public int? CustomerSatisfactionRating { get; set; }
    public string? CustomerFeedback { get; set; }
    public string? UserName { get; set; }
    public string? AgentName { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class MessageDto
{
    public Guid Id { get; set; }
    public Guid ConversationId { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? Type { get; set; }
    public string? Sender { get; set; }
    public string SenderType { get; set; } = string.Empty;
    public Guid? SenderId { get; set; }
    public string CreatedAt { get; set; } = string.Empty; // ISO string format
    public bool IsRead { get; set; }
    public string? AttachmentUrl { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class CreateConversationRequest
{
    public Guid? UserId { get; set; }
    public string? InitialMessage { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class AssignConversationRequest
{
    public Guid AgentId { get; set; }
}

public class UpdateStatusRequest
{
    public string Status { get; set; } = string.Empty;
}

public class SendMessageRequest
{
    public string Content { get; set; } = string.Empty;
    public string SenderType { get; set; } = string.Empty;
    public Guid? SenderId { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class SendChatMessageRequest
{
    public Guid? ConversationId { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Language { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class StartConversationRequest
{
    public string? InitialMessage { get; set; }
    public string? Language { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class PaginatedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}
