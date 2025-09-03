using System.Text.Json;

namespace AnalyticsService.Events;

/// <summary>
/// Base interface for all analytics events
/// </summary>
public interface IAnalyticsEvent
{
    string EventType { get; }
    string? ConversationId { get; }
    string? AgentId { get; }
    string? UserId { get; }
    string TenantId { get; }
    DateTime Timestamp { get; }
    string SessionId { get; }
    Dictionary<string, object>? Metadata { get; }
}

/// <summary>
/// Base analytics event record
/// </summary>
public abstract record AnalyticsEventBase : IAnalyticsEvent
{
    public abstract string EventType { get; }
    public string? ConversationId { get; init; }
    public string? AgentId { get; init; }
    public string? UserId { get; init; }
    public required string TenantId { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public required string SessionId { get; init; }
    public Dictionary<string, object>? Metadata { get; init; }
}

#region Chatbot Analytics Events

/// <summary>
/// User started a new conversation with chatbot
/// </summary>
public record ChatbotConversationStartedEvent : AnalyticsEventBase
{
    public override string EventType => "chatbot.conversation.started";
    public string? InitialMessage { get; init; }
    public string? UserAgent { get; init; }
    public string? IpAddress { get; init; }
    public string? Referrer { get; init; }
}

/// <summary>
/// Chatbot sent a message to user
/// </summary>
public record ChatbotMessageSentEvent : AnalyticsEventBase
{
    public override string EventType => "chatbot.message.sent";
    public required string MessageId { get; init; }
    public required string MessageText { get; init; }
    public MessageType MessageType { get; init; } = MessageType.Text;
    public bool IsIntentMatch { get; init; }
    public string? MatchedIntent { get; init; }
    public double? ConfidenceScore { get; init; }
    public string? ResponseSource { get; init; } // knowledge_base, intent, fallback
}

/// <summary>
/// User sent a message to chatbot
/// </summary>
public record ChatbotMessageReceivedEvent : AnalyticsEventBase
{
    public override string EventType => "chatbot.message.received";
    public required string MessageId { get; init; }
    public required string MessageText { get; init; }
    public MessageType MessageType { get; init; } = MessageType.Text;
    public int MessageLength { get; init; }
    public string? DetectedLanguage { get; init; }
    public string? DetectedSentiment { get; init; }
}

/// <summary>
/// Chatbot processed user intent
/// </summary>
public record ChatbotIntentProcessedEvent : AnalyticsEventBase
{
    public override string EventType => "chatbot.intent.processed";
    public required string MessageId { get; init; }
    public required string UserMessage { get; init; }
    public string? DetectedIntent { get; init; }
    public double? ConfidenceScore { get; init; }
    public List<string>? ExtractedEntities { get; init; }
    public TimeSpan ProcessingTime { get; init; }
    public string? ResponseStrategy { get; init; } // direct, search, escalate
}

/// <summary>
/// User requested live agent support
/// </summary>
public record ChatbotEscalationRequestedEvent : AnalyticsEventBase
{
    public override string EventType => "chatbot.escalation.requested";
    public string? EscalationReason { get; init; }
    public string? TriggerMessage { get; init; }
    public bool IsAutomaticEscalation { get; init; }
    public int MessageCountBeforeEscalation { get; init; }
    public TimeSpan ConversationDurationBeforeEscalation { get; init; }
}

/// <summary>
/// User provided feedback on chatbot response
/// </summary>
public record ChatbotFeedbackEvent : AnalyticsEventBase
{
    public override string EventType => "chatbot.feedback";
    public required string MessageId { get; init; }
    public FeedbackType FeedbackType { get; init; }
    public int? Rating { get; init; } // 1-5 scale
    public string? FeedbackText { get; init; }
    public bool IsHelpful { get; init; }
}

/// <summary>
/// Chatbot conversation ended
/// </summary>
public record ChatbotConversationEndedEvent : AnalyticsEventBase
{
    public override string EventType => "chatbot.conversation.ended";
    public ConversationEndReason EndReason { get; init; }
    public TimeSpan ConversationDuration { get; init; }
    public int MessageCount { get; init; }
    public int ChatbotMessages { get; init; }
    public int UserMessages { get; init; }
    public bool WasEscalated { get; init; }
    public bool WasResolved { get; init; }
    public string? LastBotMessage { get; init; }
    public string? LastUserMessage { get; init; }
}

#endregion

#region Live Agent Analytics Events

/// <summary>
/// User requested live agent support
/// </summary>
public record LiveAgentRequestedEvent : AnalyticsEventBase
{
    public override string EventType => "live_agent.requested";
    public string? RequestReason { get; init; }
    public string? PreviousChatbotConversationId { get; init; }
    public int? QueuePosition { get; init; }
    public string? Department { get; init; }
    public string? Priority { get; init; }
}

/// <summary>
/// Agent was assigned to conversation
/// </summary>
public record LiveAgentAssignedEvent : AnalyticsEventBase
{
    public override string EventType => "live_agent.assigned";
    public required string AgentName { get; init; }
    public TimeSpan? WaitTime { get; init; }
    public int? QueuePosition { get; init; }
    public string? AssignmentMethod { get; init; } // round_robin, skill_based, manual
    public string? Department { get; init; }
}

/// <summary>
/// Agent joined the conversation
/// </summary>
public record LiveAgentJoinedEvent : AnalyticsEventBase
{
    public override string EventType => "live_agent.joined";
    public required string AgentName { get; init; }
    public TimeSpan? ResponseTime { get; init; }
    public bool IsTransfer { get; init; }
    public string? PreviousAgentId { get; init; }
}

/// <summary>
/// Agent left the conversation
/// </summary>
public record LiveAgentLeftEvent : AnalyticsEventBase
{
    public override string EventType => "live_agent.left";
    public required string AgentName { get; init; }
    public TimeSpan SessionDuration { get; init; }
    public LeaveReason LeaveReason { get; init; }
    public int MessagesSent { get; init; }
    public bool IsTransfer { get; init; }
    public string? TransferReason { get; init; }
}

/// <summary>
/// Live agent sent a message
/// </summary>
public record LiveAgentMessageSentEvent : AnalyticsEventBase
{
    public override string EventType => "live_agent.message.sent";
    public required string MessageId { get; init; }
    public MessageType MessageType { get; init; } = MessageType.Text;
    public int MessageLength { get; init; }
    public TimeSpan? ResponseTime { get; init; }
    public bool IsFirstMessage { get; init; }
    public string? MessageIntent { get; init; } // greeting, information, resolution, etc.
}

/// <summary>
/// User sent message to live agent
/// </summary>
public record LiveAgentMessageReceivedEvent : AnalyticsEventBase
{
    public override string EventType => "live_agent.message.received";
    public required string MessageId { get; init; }
    public MessageType MessageType { get; init; } = MessageType.Text;
    public int MessageLength { get; init; }
    public string? DetectedSentiment { get; init; }
    public string? DetectedLanguage { get; init; }
    public bool RequiresResponse { get; init; }
}

/// <summary>
/// Conversation was escalated to another agent or department
/// </summary>
public record LiveAgentEscalationEvent : AnalyticsEventBase
{
    public override string EventType => "live_agent.escalation";
    public string? FromAgentId { get; init; }
    public string? ToAgentId { get; init; }
    public string? FromDepartment { get; init; }
    public string? ToDepartment { get; init; }
    public required string EscalationReason { get; init; }
    public TimeSpan ConversationDurationBeforeEscalation { get; init; }
    public int MessageCountBeforeEscalation { get; init; }
}

/// <summary>
/// Conversation was transferred to another agent
/// </summary>
public record LiveAgentTransferEvent : AnalyticsEventBase
{
    public override string EventType => "live_agent.transfer";
    public string? FromAgentId { get; init; }
    public required string ToAgentId { get; init; }
    public required string TransferReason { get; init; }
    public TimeSpan ConversationDurationBeforeTransfer { get; init; }
    public bool IsWarmTransfer { get; init; }
    public string? TransferNotes { get; init; }
}

/// <summary>
/// Live agent conversation ended
/// </summary>
public record LiveAgentConversationEndedEvent : AnalyticsEventBase
{
    public override string EventType => "live_agent.conversation.ended";
    public ConversationEndedBy EndedBy { get; init; }
    public TimeSpan ConversationDuration { get; init; }
    public int MessageCount { get; init; }
    public int AgentMessages { get; init; }
    public int UserMessages { get; init; }
    public ResolutionStatus ResolutionStatus { get; init; }
    public string? ResolutionNotes { get; init; }
    public bool RequiresFollowUp { get; init; }
}

/// <summary>
/// Customer feedback on live agent support
/// </summary>
public record LiveAgentFeedbackEvent : AnalyticsEventBase
{
    public override string EventType => "live_agent.feedback";
    public int Rating { get; init; } // 1-5 scale
    public string? FeedbackText { get; init; }
    public SatisfactionLevel Satisfaction { get; init; }
    public List<string>? FeedbackCategories { get; init; } // helpful, professional, knowledgeable, etc.
    public bool WouldRecommend { get; init; }
    public string? ImprovementSuggestions { get; init; }
}

/// <summary>
/// Agent response time measurement
/// </summary>
public record LiveAgentResponseTimeEvent : AnalyticsEventBase
{
    public override string EventType => "live_agent.response_time";
    public required string MessageId { get; init; }
    public TimeSpan ResponseTime { get; init; }
    public bool IsFirstResponse { get; init; }
    public ResponseTimeCategory Category { get; init; }
    public string? ResponseQuality { get; init; } // resolving, partial, clarifying
}

/// <summary>
/// Session duration tracking for live agent
/// </summary>
public record LiveAgentSessionDurationEvent : AnalyticsEventBase
{
    public override string EventType => "live_agent.session_duration";
    public DateTime StartTime { get; init; }
    public DateTime EndTime { get; init; }
    public TimeSpan Duration { get; init; }
    public int ActivePeriods { get; init; } // Number of active engagement periods
    public TimeSpan IdleTime { get; init; }
    public SessionQuality Quality { get; init; }
}

#endregion

#region Enums

public enum MessageType
{
    Text,
    Image,
    File,
    Audio,
    Video,
    Location,
    Contact,
    QuickReply,
    Button,
    Carousel,
    List
}

public enum FeedbackType
{
    Thumbsup,
    Thumbsdown,
    Rating,
    Text,
    Survey
}

public enum ConversationEndReason
{
    UserLeft,
    Timeout,
    Resolved,
    Escalated,
    Error,
    Abandoned
}

public enum ConversationEndedBy
{
    User,
    Agent,
    System,
    Timeout
}

public enum ResolutionStatus
{
    Resolved,
    Unresolved,
    PartiallyResolved,
    Escalated,
    RequiresFollowUp
}

public enum SatisfactionLevel
{
    VerySatisfied,
    Satisfied,
    Neutral,
    Dissatisfied,
    VeryDissatisfied
}

public enum LeaveReason
{
    ConversationEnded,
    Transfer,
    ShiftEnded,
    Technical,
    Break,
    Emergency
}

public enum ResponseTimeCategory
{
    Instant,      // < 10 seconds
    Quick,        // 10-30 seconds
    Normal,       // 30 seconds - 2 minutes
    Slow,         // 2-5 minutes
    VerySlow      // > 5 minutes
}

public enum SessionQuality
{
    Excellent,    // High engagement, quick responses
    Good,         // Good engagement, reasonable responses
    Average,      // Moderate engagement
    Poor,         // Low engagement, slow responses
    VeryPoor      // Very low engagement, very slow responses
}

#endregion
