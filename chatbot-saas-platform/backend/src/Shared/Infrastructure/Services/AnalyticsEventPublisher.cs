using Microsoft.Extensions.Logging;
using Shared.Domain.Enums;
using Shared.Domain.Events.AnalyticsServiceEvents;
using Shared.Infrastructure.Messaging;

namespace Shared.Infrastructure.Services;

/// <summary>
/// Service interface for publishing analytics events
/// </summary>
public interface IAnalyticsEventPublisher
{
    // Chatbot Analytics Events
    Task PublishChatbotConversationStartedAsync(string conversationId, string userId, string tenantId, string sessionId, 
        string? initialMessage = null, Dictionary<string, object>? metadata = null);
    
    Task PublishChatbotMessageSentAsync(string conversationId, string messageId, string messageText, string tenantId, string sessionId,
        MessageType messageType = MessageType.Text, bool isIntentMatch = false, string? matchedIntent = null, 
        double? confidenceScore = null, string? responseSource = null, Dictionary<string, object>? metadata = null);
    
    Task PublishChatbotMessageReceivedAsync(string conversationId, string messageId, string messageText, string userId, 
        string tenantId, string sessionId, MessageType messageType = MessageType.Text, string? detectedLanguage = null, 
        string? detectedSentiment = null, Dictionary<string, object>? metadata = null);
    
    Task PublishChatbotIntentProcessedAsync(string conversationId, string messageId, string userMessage, string userId,
        string tenantId, string sessionId, string? detectedIntent = null, double? confidenceScore = null, 
        List<string>? extractedEntities = null, TimeSpan? processingTime = null, string? responseStrategy = null, 
        Dictionary<string, object>? metadata = null);
    
    Task PublishChatbotEscalationRequestedAsync(string conversationId, string userId, string tenantId, string sessionId,
        string? escalationReason = null, string? triggerMessage = null, bool isAutomaticEscalation = false, 
        int messageCountBeforeEscalation = 0, TimeSpan? conversationDurationBeforeEscalation = null, 
        Dictionary<string, object>? metadata = null);
    
    Task PublishChatbotFeedbackAsync(string conversationId, string messageId, string userId, string tenantId, string sessionId,
        FeedbackType feedbackType, int? rating = null, string? feedbackText = null, bool isHelpful = false, 
        Dictionary<string, object>? metadata = null);
    
    Task PublishChatbotConversationEndedAsync(string conversationId, string userId, string tenantId, string sessionId,
        ConversationEndReason endReason, TimeSpan conversationDuration, int messageCount, int chatbotMessages, 
        int userMessages, bool wasEscalated = false, bool wasResolved = false, string? lastBotMessage = null, 
        string? lastUserMessage = null, Dictionary<string, object>? metadata = null);

    // Live Agent Analytics Events
    Task PublishLiveAgentRequestedAsync(string conversationId, string userId, string tenantId, string sessionId,
        string? requestReason = null, string? previousChatbotConversationId = null, int? queuePosition = null, 
        string? department = null, string? priority = null, Dictionary<string, object>? metadata = null);
    
    Task PublishLiveAgentAssignedAsync(string conversationId, string agentId, string agentName, string userId, 
        string tenantId, string sessionId, TimeSpan? waitTime = null, int? queuePosition = null, 
        string? assignmentMethod = null, string? department = null, Dictionary<string, object>? metadata = null);
    
    Task PublishLiveAgentJoinedAsync(string conversationId, string agentId, string agentName, string userId, 
        string tenantId, string sessionId, TimeSpan? responseTime = null, bool isTransfer = false, 
        string? previousAgentId = null, Dictionary<string, object>? metadata = null);
    
    Task PublishLiveAgentLeftAsync(string conversationId, string agentId, string agentName, string userId, 
        string tenantId, string sessionId, TimeSpan sessionDuration, LeaveReason leaveReason, int messagesSent = 0, 
        bool isTransfer = false, string? transferReason = null, Dictionary<string, object>? metadata = null);
    
    Task PublishLiveAgentMessageSentAsync(string conversationId, string messageId, string agentId, string userId, 
        string tenantId, string sessionId, MessageType messageType = MessageType.Text, int messageLength = 0, 
        TimeSpan? responseTime = null, bool isFirstMessage = false, string? messageIntent = null, 
        Dictionary<string, object>? metadata = null);
    
    Task PublishLiveAgentMessageReceivedAsync(string conversationId, string messageId, string agentId, string userId, 
        string tenantId, string sessionId, MessageType messageType = MessageType.Text, int messageLength = 0, 
        string? detectedSentiment = null, string? detectedLanguage = null, bool requiresResponse = true, 
        Dictionary<string, object>? metadata = null);
    
    Task PublishLiveAgentEscalationAsync(string conversationId, string? fromAgentId, string? toAgentId, string userId, 
        string tenantId, string sessionId, string escalationReason, TimeSpan conversationDurationBeforeEscalation, 
        int messageCountBeforeEscalation, string? fromDepartment = null, string? toDepartment = null, 
        Dictionary<string, object>? metadata = null);
    
    Task PublishLiveAgentTransferAsync(string conversationId, string? fromAgentId, string toAgentId, string userId, 
        string tenantId, string sessionId, string transferReason, TimeSpan conversationDurationBeforeTransfer, 
        bool isWarmTransfer = false, string? transferNotes = null, Dictionary<string, object>? metadata = null);
    
    Task PublishLiveAgentConversationEndedAsync(string conversationId, string agentId, string userId, string tenantId, 
        string sessionId, ConversationEndedBy endedBy, TimeSpan conversationDuration, int messageCount, 
        int agentMessages, int userMessages, ResolutionStatus resolutionStatus, string? resolutionNotes = null, 
        bool requiresFollowUp = false, Dictionary<string, object>? metadata = null);
    
    Task PublishLiveAgentFeedbackAsync(string conversationId, string agentId, string userId, string tenantId, string sessionId,
        int rating, SatisfactionLevel satisfaction, string? feedbackText = null, List<string>? feedbackCategories = null, 
        bool wouldRecommend = false, string? improvementSuggestions = null, Dictionary<string, object>? metadata = null);
    
    Task PublishLiveAgentResponseTimeAsync(string conversationId, string messageId, string agentId, string userId, 
        string tenantId, string sessionId, TimeSpan responseTime, bool isFirstResponse = false, 
        ResponseTimeCategory? category = null, string? responseQuality = null, Dictionary<string, object>? metadata = null);
    
    Task PublishLiveAgentSessionDurationAsync(string conversationId, string agentId, string userId, string tenantId, 
        string sessionId, DateTime startTime, DateTime endTime, TimeSpan duration, int activePeriods = 1, 
        TimeSpan? idleTime = null, SessionQuality? quality = null, Dictionary<string, object>? metadata = null);
}

/// <summary>
/// Implementation of analytics event publisher using RabbitMQ message bus
/// </summary>
public class AnalyticsEventPublisher : IAnalyticsEventPublisher
{
    private readonly IMessageBus _messageBus;
    private readonly ILogger<AnalyticsEventPublisher> _logger;

    public AnalyticsEventPublisher(IMessageBus messageBus, ILogger<AnalyticsEventPublisher> logger)
    {
        _messageBus = messageBus;
        _logger = logger;
    }

    #region Chatbot Analytics Events

    public async Task PublishChatbotConversationStartedAsync(string conversationId, string userId, string tenantId, 
        string sessionId, string? initialMessage = null, Dictionary<string, object>? metadata = null)
    {
        try
        {
            var @event = new ChatbotConversationStartedEvent
            {
                ConversationId = conversationId,
                UserId = userId,
                TenantId = tenantId,
                SessionId = sessionId,
                InitialMessage = initialMessage,
                Metadata = metadata
            };

            await _messageBus.PublishAsync(@event);
            _logger.LogDebug("Published chatbot conversation started event for conversation {ConversationId}", conversationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish chatbot conversation started event for conversation {ConversationId}", conversationId);
            // Don't throw - analytics shouldn't break business logic
        }
    }

    public async Task PublishChatbotMessageSentAsync(string conversationId, string messageId, string messageText, 
        string tenantId, string sessionId, MessageType messageType = MessageType.Text, bool isIntentMatch = false, 
        string? matchedIntent = null, double? confidenceScore = null, string? responseSource = null, 
        Dictionary<string, object>? metadata = null)
    {
        try
        {
            var @event = new ChatbotMessageSentEvent
            {
                ConversationId = conversationId,
                MessageId = messageId,
                MessageText = messageText,
                TenantId = tenantId,
                SessionId = sessionId,
                MessageType = messageType,
                IsIntentMatch = isIntentMatch,
                MatchedIntent = matchedIntent,
                ConfidenceScore = confidenceScore,
                ResponseSource = responseSource,
                Metadata = metadata
            };

            await _messageBus.PublishAsync(@event);
            _logger.LogDebug("Published chatbot message sent event for message {MessageId}", messageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish chatbot message sent event for message {MessageId}", messageId);
        }
    }

    public async Task PublishChatbotMessageReceivedAsync(string conversationId, string messageId, string messageText, 
        string userId, string tenantId, string sessionId, MessageType messageType = MessageType.Text, 
        string? detectedLanguage = null, string? detectedSentiment = null, Dictionary<string, object>? metadata = null)
    {
        try
        {
            var @event = new ChatbotMessageReceivedEvent
            {
                ConversationId = conversationId,
                MessageId = messageId,
                MessageText = messageText,
                UserId = userId,
                TenantId = tenantId,
                SessionId = sessionId,
                MessageType = messageType,
                MessageLength = messageText.Length,
                DetectedLanguage = detectedLanguage,
                DetectedSentiment = detectedSentiment,
                Metadata = metadata
            };

            await _messageBus.PublishAsync(@event);
            _logger.LogDebug("Published chatbot message received event for message {MessageId}", messageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish chatbot message received event for message {MessageId}", messageId);
        }
    }

    public async Task PublishChatbotIntentProcessedAsync(string conversationId, string messageId, string userMessage, 
        string userId, string tenantId, string sessionId, string? detectedIntent = null, double? confidenceScore = null, 
        List<string>? extractedEntities = null, TimeSpan? processingTime = null, string? responseStrategy = null, 
        Dictionary<string, object>? metadata = null)
    {
        try
        {
            var @event = new ChatbotIntentProcessedEvent
            {
                ConversationId = conversationId,
                MessageId = messageId,
                UserMessage = userMessage,
                UserId = userId,
                TenantId = tenantId,
                SessionId = sessionId,
                DetectedIntent = detectedIntent,
                ConfidenceScore = confidenceScore,
                ExtractedEntities = extractedEntities,
                ProcessingTime = processingTime ?? TimeSpan.Zero,
                ResponseStrategy = responseStrategy,
                Metadata = metadata
            };

            await _messageBus.PublishAsync(@event);
            _logger.LogDebug("Published chatbot intent processed event for message {MessageId}", messageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish chatbot intent processed event for message {MessageId}", messageId);
        }
    }

    public async Task PublishChatbotEscalationRequestedAsync(string conversationId, string userId, string tenantId, 
        string sessionId, string? escalationReason = null, string? triggerMessage = null, bool isAutomaticEscalation = false, 
        int messageCountBeforeEscalation = 0, TimeSpan? conversationDurationBeforeEscalation = null, 
        Dictionary<string, object>? metadata = null)
    {
        try
        {
            var @event = new ChatbotEscalationRequestedEvent
            {
                ConversationId = conversationId,
                UserId = userId,
                TenantId = tenantId,
                SessionId = sessionId,
                EscalationReason = escalationReason,
                TriggerMessage = triggerMessage,
                IsAutomaticEscalation = isAutomaticEscalation,
                MessageCountBeforeEscalation = messageCountBeforeEscalation,
                ConversationDurationBeforeEscalation = conversationDurationBeforeEscalation ?? TimeSpan.Zero,
                Metadata = metadata
            };

            await _messageBus.PublishAsync(@event);
            _logger.LogDebug("Published chatbot escalation requested event for conversation {ConversationId}", conversationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish chatbot escalation requested event for conversation {ConversationId}", conversationId);
        }
    }

    public async Task PublishChatbotFeedbackAsync(string conversationId, string messageId, string userId, string tenantId, 
        string sessionId, FeedbackType feedbackType, int? rating = null, string? feedbackText = null, bool isHelpful = false, 
        Dictionary<string, object>? metadata = null)
    {
        try
        {
            var @event = new ChatbotFeedbackEvent
            {
                ConversationId = conversationId,
                MessageId = messageId,
                UserId = userId,
                TenantId = tenantId,
                SessionId = sessionId,
                FeedbackType = feedbackType,
                Rating = rating,
                FeedbackText = feedbackText,
                IsHelpful = isHelpful,
                Metadata = metadata
            };

            await _messageBus.PublishAsync(@event);
            _logger.LogDebug("Published chatbot feedback event for message {MessageId}", messageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish chatbot feedback event for message {MessageId}", messageId);
        }
    }

    public async Task PublishChatbotConversationEndedAsync(string conversationId, string userId, string tenantId, 
        string sessionId, ConversationEndReason endReason, TimeSpan conversationDuration, int messageCount, 
        int chatbotMessages, int userMessages, bool wasEscalated = false, bool wasResolved = false, 
        string? lastBotMessage = null, string? lastUserMessage = null, Dictionary<string, object>? metadata = null)
    {
        try
        {
            var @event = new ChatbotConversationEndedEvent
            {
                ConversationId = conversationId,
                UserId = userId,
                TenantId = tenantId,
                SessionId = sessionId,
                EndReason = endReason,
                ConversationDuration = conversationDuration,
                MessageCount = messageCount,
                ChatbotMessages = chatbotMessages,
                UserMessages = userMessages,
                WasEscalated = wasEscalated,
                WasResolved = wasResolved,
                LastBotMessage = lastBotMessage,
                LastUserMessage = lastUserMessage,
                Metadata = metadata
            };

            await _messageBus.PublishAsync(@event);
            _logger.LogDebug("Published chatbot conversation ended event for conversation {ConversationId}", conversationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish chatbot conversation ended event for conversation {ConversationId}", conversationId);
        }
    }

    #endregion

    #region Live Agent Analytics Events

    public async Task PublishLiveAgentRequestedAsync(string conversationId, string userId, string tenantId, string sessionId,
        string? requestReason = null, string? previousChatbotConversationId = null, int? queuePosition = null, 
        string? department = null, string? priority = null, Dictionary<string, object>? metadata = null)
    {
        try
        {
            var @event = new LiveAgentRequestedEvent
            {
                ConversationId = conversationId,
                UserId = userId,
                TenantId = tenantId,
                SessionId = sessionId,
                RequestReason = requestReason,
                PreviousChatbotConversationId = previousChatbotConversationId,
                QueuePosition = queuePosition,
                Department = department,
                Priority = priority,
                Metadata = metadata
            };

            await _messageBus.PublishAsync(@event);
            _logger.LogDebug("Published live agent requested event for conversation {ConversationId}", conversationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish live agent requested event for conversation {ConversationId}", conversationId);
        }
    }

    public async Task PublishLiveAgentAssignedAsync(string conversationId, string agentId, string agentName, string userId, 
        string tenantId, string sessionId, TimeSpan? waitTime = null, int? queuePosition = null, 
        string? assignmentMethod = null, string? department = null, Dictionary<string, object>? metadata = null)
    {
        try
        {
            var @event = new LiveAgentAssignedEvent
            {
                ConversationId = conversationId,
                AgentId = agentId,
                AgentName = agentName,
                UserId = userId,
                TenantId = tenantId,
                SessionId = sessionId,
                WaitTime = waitTime,
                QueuePosition = queuePosition,
                AssignmentMethod = assignmentMethod,
                Department = department,
                Metadata = metadata
            };

            await _messageBus.PublishAsync(@event);
            _logger.LogDebug("Published live agent assigned event for conversation {ConversationId}, agent {AgentId}", conversationId, agentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish live agent assigned event for conversation {ConversationId}, agent {AgentId}", conversationId, agentId);
        }
    }

    // Continue with remaining live agent event methods...
    public async Task PublishLiveAgentJoinedAsync(string conversationId, string agentId, string agentName, string userId, 
        string tenantId, string sessionId, TimeSpan? responseTime = null, bool isTransfer = false, 
        string? previousAgentId = null, Dictionary<string, object>? metadata = null)
    {
        try
        {
            var @event = new LiveAgentJoinedEvent
            {
                ConversationId = conversationId,
                AgentId = agentId,
                AgentName = agentName,
                UserId = userId,
                TenantId = tenantId,
                SessionId = sessionId,
                ResponseTime = responseTime,
                IsTransfer = isTransfer,
                PreviousAgentId = previousAgentId,
                Metadata = metadata
            };

            await _messageBus.PublishAsync(@event);
            _logger.LogDebug("Published live agent joined event for conversation {ConversationId}, agent {AgentId}", conversationId, agentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish live agent joined event for conversation {ConversationId}, agent {AgentId}", conversationId, agentId);
        }
    }

    public async Task PublishLiveAgentLeftAsync(string conversationId, string agentId, string agentName, string userId, 
        string tenantId, string sessionId, TimeSpan sessionDuration, LeaveReason leaveReason, int messagesSent = 0, 
        bool isTransfer = false, string? transferReason = null, Dictionary<string, object>? metadata = null)
    {
        try
        {
            var @event = new LiveAgentLeftEvent
            {
                ConversationId = conversationId,
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
                Metadata = metadata
            };

            await _messageBus.PublishAsync(@event);
            _logger.LogDebug("Published live agent left event for conversation {ConversationId}, agent {AgentId}", conversationId, agentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish live agent left event for conversation {ConversationId}, agent {AgentId}", conversationId, agentId);
        }
    }

    public async Task PublishLiveAgentMessageSentAsync(string conversationId, string messageId, string agentId, string userId, 
        string tenantId, string sessionId, MessageType messageType = MessageType.Text, int messageLength = 0, 
        TimeSpan? responseTime = null, bool isFirstMessage = false, string? messageIntent = null, 
        Dictionary<string, object>? metadata = null)
    {
        try
        {
            var @event = new LiveAgentMessageSentEvent
            {
                ConversationId = conversationId,
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
                Metadata = metadata
            };

            await _messageBus.PublishAsync(@event);
            _logger.LogDebug("Published live agent message sent event for message {MessageId}", messageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish live agent message sent event for message {MessageId}", messageId);
        }
    }

    public async Task PublishLiveAgentMessageReceivedAsync(string conversationId, string messageId, string agentId, 
        string userId, string tenantId, string sessionId, MessageType messageType = MessageType.Text, 
        int messageLength = 0, string? detectedSentiment = null, string? detectedLanguage = null, 
        bool requiresResponse = true, Dictionary<string, object>? metadata = null)
    {
        try
        {
            var @event = new LiveAgentMessageReceivedEvent
            {
                ConversationId = conversationId,
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
                Metadata = metadata
            };

            await _messageBus.PublishAsync(@event);
            _logger.LogDebug("Published live agent message received event for message {MessageId}", messageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish live agent message received event for message {MessageId}", messageId);
        }
    }

    public async Task PublishLiveAgentEscalationAsync(string conversationId, string? fromAgentId, string? toAgentId, 
        string userId, string tenantId, string sessionId, string escalationReason, 
        TimeSpan conversationDurationBeforeEscalation, int messageCountBeforeEscalation, string? fromDepartment = null, 
        string? toDepartment = null, Dictionary<string, object>? metadata = null)
    {
        try
        {
            var @event = new LiveAgentEscalationEvent
            {
                ConversationId = conversationId,
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
                Metadata = metadata
            };

            await _messageBus.PublishAsync(@event);
            _logger.LogDebug("Published live agent escalation event for conversation {ConversationId}", conversationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish live agent escalation event for conversation {ConversationId}", conversationId);
        }
    }

    public async Task PublishLiveAgentTransferAsync(string conversationId, string? fromAgentId, string toAgentId, 
        string userId, string tenantId, string sessionId, string transferReason, 
        TimeSpan conversationDurationBeforeTransfer, bool isWarmTransfer = false, string? transferNotes = null, 
        Dictionary<string, object>? metadata = null)
    {
        try
        {
            var @event = new LiveAgentTransferEvent
            {
                ConversationId = conversationId,
                FromAgentId = fromAgentId,
                ToAgentId = toAgentId,
                UserId = userId,
                TenantId = tenantId,
                SessionId = sessionId,
                TransferReason = transferReason,
                ConversationDurationBeforeTransfer = conversationDurationBeforeTransfer,
                IsWarmTransfer = isWarmTransfer,
                TransferNotes = transferNotes,
                Metadata = metadata
            };

            await _messageBus.PublishAsync(@event);
            _logger.LogDebug("Published live agent transfer event for conversation {ConversationId}", conversationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish live agent transfer event for conversation {ConversationId}", conversationId);
        }
    }

    public async Task PublishLiveAgentConversationEndedAsync(string conversationId, string agentId, string userId, 
        string tenantId, string sessionId, ConversationEndedBy endedBy, TimeSpan conversationDuration, int messageCount, 
        int agentMessages, int userMessages, ResolutionStatus resolutionStatus, string? resolutionNotes = null, 
        bool requiresFollowUp = false, Dictionary<string, object>? metadata = null)
    {
        try
        {
            var @event = new LiveAgentConversationEndedEvent
            {
                ConversationId = conversationId,
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
                Metadata = metadata
            };

            await _messageBus.PublishAsync(@event);
            _logger.LogDebug("Published live agent conversation ended event for conversation {ConversationId}", conversationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish live agent conversation ended event for conversation {ConversationId}", conversationId);
        }
    }

    public async Task PublishLiveAgentFeedbackAsync(string conversationId, string agentId, string userId, string tenantId, 
        string sessionId, int rating, SatisfactionLevel satisfaction, string? feedbackText = null, 
        List<string>? feedbackCategories = null, bool wouldRecommend = false, string? improvementSuggestions = null, 
        Dictionary<string, object>? metadata = null)
    {
        try
        {
            var @event = new LiveAgentFeedbackEvent
            {
                ConversationId = conversationId,
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
                Metadata = metadata
            };

            await _messageBus.PublishAsync(@event);
            _logger.LogDebug("Published live agent feedback event for conversation {ConversationId}", conversationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish live agent feedback event for conversation {ConversationId}", conversationId);
        }
    }

    public async Task PublishLiveAgentResponseTimeAsync(string conversationId, string messageId, string agentId, 
        string userId, string tenantId, string sessionId, TimeSpan responseTime, bool isFirstResponse = false, 
        ResponseTimeCategory? category = null, string? responseQuality = null, Dictionary<string, object>? metadata = null)
    {
        try
        {
            var calculatedCategory = category ?? CalculateResponseTimeCategory(responseTime);
            
            var @event = new LiveAgentResponseTimeEvent
            {
                ConversationId = conversationId,
                MessageId = messageId,
                AgentId = agentId,
                UserId = userId,
                TenantId = tenantId,
                SessionId = sessionId,
                ResponseTime = responseTime,
                IsFirstResponse = isFirstResponse,
                Category = calculatedCategory,
                ResponseQuality = responseQuality,
                Metadata = metadata
            };

            await _messageBus.PublishAsync(@event);
            _logger.LogDebug("Published live agent response time event for message {MessageId}", messageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish live agent response time event for message {MessageId}", messageId);
        }
    }

    public async Task PublishLiveAgentSessionDurationAsync(string conversationId, string agentId, string userId, 
        string tenantId, string sessionId, DateTime startTime, DateTime endTime, TimeSpan duration, int activePeriods = 1, 
        TimeSpan? idleTime = null, SessionQuality? quality = null, Dictionary<string, object>? metadata = null)
    {
        try
        {
            var @event = new LiveAgentSessionDurationEvent
            {
                ConversationId = conversationId,
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
                Metadata = metadata
            };

            await _messageBus.PublishAsync(@event);
            _logger.LogDebug("Published live agent session duration event for conversation {ConversationId}", conversationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish live agent session duration event for conversation {ConversationId}", conversationId);
        }
    }

    #endregion

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
