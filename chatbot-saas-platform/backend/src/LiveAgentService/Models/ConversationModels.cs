namespace LiveAgentService.Models;

public class UpdateConversationStatusRequest
{
    public string Status { get; set; } = string.Empty;
}

public class SendMessageRequest
{
    public string Content { get; set; } = string.Empty;
    public string? Type { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}

public class RateConversationRequest
{
    public int Rating { get; set; }
    public string? Feedback { get; set; }
}

public class MessageNotificationRequest
{
    public Guid ConversationId { get; set; }
    public Guid MessageId { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? Type { get; set; }
    public string Sender { get; set; } = string.Empty;
    public Guid? SenderId { get; set; }
    public string? SenderName { get; set; }
    public DateTime CreatedAt { get; set; }
}

//public class UpdateAgentProfileRequest
//{
//    public string? Name { get; set; }
//    public string? Email { get; set; }
//    public string? Phone { get; set; }
//    public string? Bio { get; set; }
//    public string? Location { get; set; }
//    public string? Timezone { get; set; }
//    public string? Language { get; set; }
//    public List<string>? Skills { get; set; }
//    public List<string>? Languages { get; set; }
//    public List<string>? Specializations { get; set; }
//}
