
using AnalyticsService.Services;
using Microsoft.Extensions.Logging;
using Shared.Domain.Enums;
using Shared.Domain.Events.AnalyticsServiceEvents;

namespace LiveAgentService.Services;

/// <summary>
/// Service to integrate live agent analytics events with the Analytics service
/// </summary>
public interface ILiveAgentAnalyticsService
{
    Task PublishAgentRequestedAsync(Guid conversationId, string? userId, string tenantId, string sessionId, 
        string? requestReason = null, string? previousChatbotConversationId = null, int? queuePosition = null, 
        string? department = null, string? priority = null);
    
    Task PublishAgentAssignedAsync(Guid conversationId, string agentId, string agentName, string? userId, 
        string tenantId, string sessionId, TimeSpan? waitTime = null, int? queuePosition = null, 
        string? assignmentMethod = null, string? department = null);
    
    Task PublishAgentJoinedAsync(Guid conversationId, string agentId, string agentName, string? userId, 
        string tenantId, string sessionId, TimeSpan? responseTime = null, bool isTransfer = false, 
        string? previousAgentId = null);
    
    Task PublishAgentLeftAsync(Guid conversationId, string agentId, string agentName, string? userId, 
        string tenantId, string sessionId, TimeSpan sessionDuration, LeaveReason leaveReason, int messagesSent = 0, 
        bool isTransfer = false, string? transferReason = null);
    
    Task PublishMessageSentAsync(Guid conversationId, string messageId, string agentId, string? userId, 
        string tenantId, string sessionId, MessageType messageType = MessageType.Text, int messageLength = 0, 
        TimeSpan? responseTime = null, bool isFirstMessage = false, string? messageIntent = null);
    
    Task PublishMessageReceivedAsync(Guid conversationId, string messageId, string agentId, string? userId, 
        string tenantId, string sessionId, MessageType messageType = MessageType.Text, int messageLength = 0, 
        string? detectedSentiment = null, string? detectedLanguage = null, bool requiresResponse = true);
    
    Task PublishEscalationAsync(Guid conversationId, string? fromAgentId, string? toAgentId, string? userId, 
        string tenantId, string sessionId, string escalationReason, TimeSpan conversationDurationBeforeEscalation, 
        int messageCountBeforeEscalation, string? fromDepartment = null, string? toDepartment = null);
    
    Task PublishTransferAsync(Guid conversationId, string? fromAgentId, string toAgentId, string? userId, 
        string tenantId, string sessionId, string transferReason, TimeSpan conversationDurationBeforeTransfer, 
        bool isWarmTransfer = false, string? transferNotes = null);
    
    Task PublishConversationEndedAsync(Guid conversationId, string agentId, string? userId, string tenantId, 
        string sessionId, ConversationEndedBy endedBy, TimeSpan conversationDuration, int messageCount, 
        int agentMessages, int userMessages, ResolutionStatus resolutionStatus, string? resolutionNotes = null, 
        bool requiresFollowUp = false);
    
    Task PublishFeedbackAsync(Guid conversationId, string agentId, string? userId, string tenantId, string sessionId, 
        int rating, SatisfactionLevel satisfaction, string? feedbackText = null, List<string>? feedbackCategories = null, 
        bool wouldRecommend = false, string? improvementSuggestions = null);
    
    Task PublishResponseTimeAsync(Guid conversationId, string messageId, string agentId, string? userId, 
        string tenantId, string sessionId, TimeSpan responseTime, bool isFirstResponse = false, 
        ResponseTimeCategory? category = null, string? responseQuality = null);
    
    Task PublishSessionDurationAsync(Guid conversationId, string agentId, string? userId, string tenantId, 
        string sessionId, DateTime startTime, DateTime endTime, TimeSpan duration, int activePeriods = 1, 
        TimeSpan? idleTime = null, SessionQuality? quality = null);
}

/// <summary>
/// Implementation of live agent analytics service
/// </summary>
public class LiveAgentAnalyticsService : ILiveAgentAnalyticsService
{
    private readonly IAnalyticsMessageBusService _analyticsMessageBus;
    private readonly ILogger<LiveAgentAnalyticsService> _logger;

    public LiveAgentAnalyticsService(IAnalyticsMessageBusService analyticsMessageBus, ILogger<LiveAgentAnalyticsService> logger)
    {
        _analyticsMessageBus = analyticsMessageBus;
        _logger = logger;
    }

    public async Task PublishAgentRequestedAsync(Guid conversationId, string? userId, string tenantId, string sessionId, 
        string? requestReason = null, string? previousChatbotConversationId = null, int? queuePosition = null, 
        string? department = null, string? priority = null)
    {
        try
        {
            var analyticsEvent = new LiveAgentRequestedEvent
            {
                ConversationId = conversationId.ToString(),
                UserId = userId,
                TenantId = tenantId,
                SessionId = sessionId,
                RequestReason = requestReason,
                PreviousChatbotConversationId = previousChatbotConversationId,
                QueuePosition = queuePosition,
                Department = department,
                Priority = priority,
                Timestamp = DateTime.UtcNow
            };

            await _analyticsMessageBus.PublishAnalyticsEventAsync(analyticsEvent);
            _logger.LogDebug("Published live agent requested event for conversation {ConversationId}", conversationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish live agent requested event for conversation {ConversationId}", conversationId);
        }
    }

    public async Task PublishAgentAssignedAsync(Guid conversationId, string agentId, string agentName, string? userId, 
        string tenantId, string sessionId, TimeSpan? waitTime = null, int? queuePosition = null, 
        string? assignmentMethod = null, string? department = null)
    {
        try
        {
            var analyticsEvent = new LiveAgentAssignedEvent
            {
                ConversationId = conversationId.ToString(),
                AgentId = agentId,
                AgentName = agentName,
                UserId = userId,
                TenantId = tenantId,
                SessionId = sessionId,
                WaitTime = waitTime,
                QueuePosition = queuePosition,
                AssignmentMethod = assignmentMethod,
                Department = department,
                Timestamp = DateTime.UtcNow
            };

            await _analyticsMessageBus.PublishAnalyticsEventAsync(analyticsEvent);
            _logger.LogDebug("Published live agent assigned event for conversation {ConversationId}, agent {AgentId}", conversationId, agentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish live agent assigned event for conversation {ConversationId}, agent {AgentId}", conversationId, agentId);
        }
    }

    public async Task PublishAgentJoinedAsync(Guid conversationId, string agentId, string agentName, string? userId, 
        string tenantId, string sessionId, TimeSpan? responseTime = null, bool isTransfer = false, 
        string? previousAgentId = null)
    {
        try
        {
            var analyticsEvent = new LiveAgentJoinedEvent
            {
                ConversationId = conversationId.ToString(),
                AgentId = agentId,
                AgentName = agentName,
                UserId = userId,
                TenantId = tenantId,
                SessionId = sessionId,
                ResponseTime = responseTime,
                IsTransfer = isTransfer,
                PreviousAgentId = previousAgentId,
                Timestamp = DateTime.UtcNow
            };

            await _analyticsMessageBus.PublishAnalyticsEventAsync(analyticsEvent);
            _logger.LogDebug("Published live agent joined event for conversation {ConversationId}, agent {AgentId}", conversationId, agentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish live agent joined event for conversation {ConversationId}, agent {AgentId}", conversationId, agentId);
        }
    }

    public async Task PublishAgentLeftAsync(Guid conversationId, string agentId, string agentName, string? userId, 
        string tenantId, string sessionId, TimeSpan sessionDuration, LeaveReason leaveReason, int messagesSent = 0, 
        bool isTransfer = false, string? transferReason = null)
    {
        try
        {
            var analyticsEvent = new LiveAgentLeftEvent
            {
                ConversationId = conversationId.ToString(),
                AgentId = agentId,
                AgentName = agentName,
                UserId = userId,
                TenantId = tenantId,
                SessionId = sessionId,
                SessionDuration = sessionDuration,
                LeaveReason = leaveReason,
                MessagesSent = messagesSent,
                IsTransfer = isTransfer,
                TransferReason = transferReason,
                Timestamp = DateTime.UtcNow
            };

            await _analyticsMessageBus.PublishAnalyticsEventAsync(analyticsEvent);
            _logger.LogDebug("Published live agent left event for conversation {ConversationId}, agent {AgentId}", conversationId, agentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish live agent left event for conversation {ConversationId}, agent {AgentId}", conversationId, agentId);
        }
    }

    public async Task PublishMessageSentAsync(Guid conversationId, string messageId, string agentId, string? userId, 
        string tenantId, string sessionId, MessageType messageType = MessageType.Text, int messageLength = 0, 
        TimeSpan? responseTime = null, bool isFirstMessage = false, string? messageIntent = null)
    {
        try
        {
            var analyticsEvent = new LiveAgentMessageSentEvent
            {
                ConversationId = conversationId.ToString(),
                MessageId = messageId,
                AgentId = agentId,
                UserId = userId,
                TenantId = tenantId,
                SessionId = sessionId,
                MessageType = messageType,
                MessageLength = messageLength,
                ResponseTime = responseTime,
                IsFirstMessage = isFirstMessage,
                MessageIntent = messageIntent,
                Timestamp = DateTime.UtcNow
            };

            await _analyticsMessageBus.PublishAnalyticsEventAsync(analyticsEvent);
            _logger.LogDebug("Published live agent message sent event for message {MessageId}", messageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish live agent message sent event for message {MessageId}", messageId);
        }
    }

    public async Task PublishMessageReceivedAsync(Guid conversationId, string messageId, string agentId, string? userId, 
        string tenantId, string sessionId, MessageType messageType = MessageType.Text, int messageLength = 0, 
        string? detectedSentiment = null, string? detectedLanguage = null, bool requiresResponse = true)
    {
        try
        {
            var analyticsEvent = new LiveAgentMessageReceivedEvent
            {
                ConversationId = conversationId.ToString(),
                MessageId = messageId,
                AgentId = agentId,
                UserId = userId,
                TenantId = tenantId,
                SessionId = sessionId,
                MessageType = messageType,
                MessageLength = messageLength,
                DetectedSentiment = detectedSentiment,
                DetectedLanguage = detectedLanguage,
                RequiresResponse = requiresResponse,
                Timestamp = DateTime.UtcNow
            };

            await _analyticsMessageBus.PublishAnalyticsEventAsync(analyticsEvent);
            _logger.LogDebug("Published live agent message received event for message {MessageId}", messageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish live agent message received event for message {MessageId}", messageId);
        }
    }

    public async Task PublishEscalationAsync(Guid conversationId, string? fromAgentId, string? toAgentId, string? userId, 
        string tenantId, string sessionId, string escalationReason, TimeSpan conversationDurationBeforeEscalation, 
        int messageCountBeforeEscalation, string? fromDepartment = null, string? toDepartment = null)
    {
        try
        {
            var analyticsEvent = new LiveAgentEscalationEvent
            {
                ConversationId = conversationId.ToString(),
                FromAgentId = fromAgentId,
                ToAgentId = toAgentId,
                UserId = userId,
                TenantId = tenantId,
                SessionId = sessionId,
                EscalationReason = escalationReason,
                ConversationDurationBeforeEscalation = conversationDurationBeforeEscalation,
                MessageCountBeforeEscalation = messageCountBeforeEscalation,
                FromDepartment = fromDepartment,
                ToDepartment = toDepartment,
                Timestamp = DateTime.UtcNow
            };

            await _analyticsMessageBus.PublishAnalyticsEventAsync(analyticsEvent);
            _logger.LogDebug("Published live agent escalation event for conversation {ConversationId}", conversationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish live agent escalation event for conversation {ConversationId}", conversationId);
        }
    }

    public async Task PublishTransferAsync(Guid conversationId, string? fromAgentId, string toAgentId, string? userId, 
        string tenantId, string sessionId, string transferReason, TimeSpan conversationDurationBeforeTransfer, 
        bool isWarmTransfer = false, string? transferNotes = null)
    {
        try
        {
            var analyticsEvent = new LiveAgentTransferEvent
            {
                ConversationId = conversationId.ToString(),
                FromAgentId = fromAgentId,
                ToAgentId = toAgentId,
                UserId = userId,
                TenantId = tenantId,
                SessionId = sessionId,
                TransferReason = transferReason,
                ConversationDurationBeforeTransfer = conversationDurationBeforeTransfer,
                IsWarmTransfer = isWarmTransfer,
                TransferNotes = transferNotes,
                Timestamp = DateTime.UtcNow
            };

            await _analyticsMessageBus.PublishAnalyticsEventAsync(analyticsEvent);
            _logger.LogDebug("Published live agent transfer event for conversation {ConversationId}", conversationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish live agent transfer event for conversation {ConversationId}", conversationId);
        }
    }

    public async Task PublishConversationEndedAsync(Guid conversationId, string agentId, string? userId, string tenantId, 
        string sessionId, ConversationEndedBy endedBy, TimeSpan conversationDuration, int messageCount, 
        int agentMessages, int userMessages, ResolutionStatus resolutionStatus, string? resolutionNotes = null, 
        bool requiresFollowUp = false)
    {
        try
        {
            var analyticsEvent = new LiveAgentConversationEndedEvent
            {
                ConversationId = conversationId.ToString(),
                AgentId = agentId,
                UserId = userId,
                TenantId = tenantId,
                SessionId = sessionId,
                EndedBy = endedBy,
                ConversationDuration = conversationDuration,
                MessageCount = messageCount,
                AgentMessages = agentMessages,
                UserMessages = userMessages,
                ResolutionStatus = resolutionStatus,
                ResolutionNotes = resolutionNotes,
                RequiresFollowUp = requiresFollowUp,
                Timestamp = DateTime.UtcNow
            };

            await _analyticsMessageBus.PublishAnalyticsEventAsync(analyticsEvent);
            _logger.LogDebug("Published live agent conversation ended event for conversation {ConversationId}", conversationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish live agent conversation ended event for conversation {ConversationId}", conversationId);
        }
    }

    public async Task PublishFeedbackAsync(Guid conversationId, string agentId, string? userId, string tenantId, string sessionId, 
        int rating, SatisfactionLevel satisfaction, string? feedbackText = null, List<string>? feedbackCategories = null, 
        bool wouldRecommend = false, string? improvementSuggestions = null)
    {
        try
        {
            var analyticsEvent = new LiveAgentFeedbackEvent
            {
                ConversationId = conversationId.ToString(),
                AgentId = agentId,
                UserId = userId,
                TenantId = tenantId,
                SessionId = sessionId,
                Rating = rating,
                Satisfaction = satisfaction,
                FeedbackText = feedbackText,
                FeedbackCategories = feedbackCategories,
                WouldRecommend = wouldRecommend,
                ImprovementSuggestions = improvementSuggestions,
                Timestamp = DateTime.UtcNow
            };

            await _analyticsMessageBus.PublishAnalyticsEventAsync(analyticsEvent);
            _logger.LogDebug("Published live agent feedback event for conversation {ConversationId}", conversationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish live agent feedback event for conversation {ConversationId}", conversationId);
        }
    }

    public async Task PublishResponseTimeAsync(Guid conversationId, string messageId, string agentId, string? userId, 
        string tenantId, string sessionId, TimeSpan responseTime, bool isFirstResponse = false, 
        ResponseTimeCategory? category = null, string? responseQuality = null)
    {
        try
        {
            var calculatedCategory = category ?? CalculateResponseTimeCategory(responseTime);
            
            var analyticsEvent = new LiveAgentResponseTimeEvent
            {
                ConversationId = conversationId.ToString(),
                MessageId = messageId,
                AgentId = agentId,
                UserId = userId,
                TenantId = tenantId,
                SessionId = sessionId,
                ResponseTime = responseTime,
                IsFirstResponse = isFirstResponse,
                Category = calculatedCategory,
                ResponseQuality = responseQuality,
                Timestamp = DateTime.UtcNow
            };

            await _analyticsMessageBus.PublishAnalyticsEventAsync(analyticsEvent);
            _logger.LogDebug("Published live agent response time event for message {MessageId}", messageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish live agent response time event for message {MessageId}", messageId);
        }
    }

    public async Task PublishSessionDurationAsync(Guid conversationId, string agentId, string? userId, string tenantId, 
        string sessionId, DateTime startTime, DateTime endTime, TimeSpan duration, int activePeriods = 1, 
        TimeSpan? idleTime = null, SessionQuality? quality = null)
    {
        try
        {
            var analyticsEvent = new LiveAgentSessionDurationEvent
            {
                ConversationId = conversationId.ToString(),
                AgentId = agentId,
                UserId = userId,
                TenantId = tenantId,
                SessionId = sessionId,
                StartTime = startTime,
                EndTime = endTime,
                Duration = duration,
                ActivePeriods = activePeriods,
                IdleTime = idleTime ?? TimeSpan.Zero,
                Quality = quality ?? SessionQuality.Average,
                Timestamp = DateTime.UtcNow
            };

            await _analyticsMessageBus.PublishAnalyticsEventAsync(analyticsEvent);
            _logger.LogDebug("Published live agent session duration event for conversation {ConversationId}", conversationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish live agent session duration event for conversation {ConversationId}", conversationId);
        }
    }

    private static ResponseTimeCategory CalculateResponseTimeCategory(TimeSpan responseTime)
    {
        return responseTime.TotalSeconds switch
        {
            < 10 => ResponseTimeCategory.Instant,
            < 30 => ResponseTimeCategory.Quick,
            < 120 => ResponseTimeCategory.Normal,
            < 300 => ResponseTimeCategory.Slow,
            _ => ResponseTimeCategory.VerySlow
        };
    }
}