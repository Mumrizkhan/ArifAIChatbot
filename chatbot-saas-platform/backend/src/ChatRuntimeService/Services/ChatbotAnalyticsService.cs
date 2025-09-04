
using AnalyticsService.Services;
using Microsoft.Extensions.Logging;
using Shared.Domain.Enums;
using Shared.Domain.Events.AnalyticsServiceEvents;

namespace ChatRuntimeService.Services;

/// <summary>
/// Service to integrate chatbot analytics events with the Analytics service
/// </summary>
public interface IChatbotAnalyticsService
{
    Task PublishConversationStartedAsync(Guid conversationId, string? userId, string tenantId, string sessionId, 
        string? initialMessage = null, string? userAgent = null, string? ipAddress = null, string? referrer = null);
    
    Task PublishMessageSentAsync(Guid conversationId, string messageId, string messageText, string tenantId, 
        string sessionId, bool isIntentMatch = false, string? matchedIntent = null, double? confidenceScore = null, 
        string? responseSource = null);
    
    Task PublishMessageReceivedAsync(Guid conversationId, string messageId, string messageText, string? userId, 
        string tenantId, string sessionId, string? detectedLanguage = null, string? detectedSentiment = null);
    
    Task PublishIntentProcessedAsync(Guid conversationId, string messageId, string userMessage, string? userId, 
        string tenantId, string sessionId, string? detectedIntent = null, double? confidenceScore = null, 
        List<string>? extractedEntities = null, TimeSpan? processingTime = null, string? responseStrategy = null);
    
    Task PublishEscalationRequestedAsync(Guid conversationId, string? userId, string tenantId, string sessionId, 
        string? escalationReason = null, string? triggerMessage = null, bool isAutomaticEscalation = false, 
        int messageCountBeforeEscalation = 0, TimeSpan? conversationDurationBeforeEscalation = null);
    
    Task PublishFeedbackAsync(Guid conversationId, string messageId, string? userId, string tenantId, string sessionId, 
        FeedbackType feedbackType, int? rating = null, string? feedbackText = null, bool isHelpful = false);
    
    Task PublishConversationEndedAsync(Guid conversationId, string? userId, string tenantId, string sessionId, 
        ConversationEndReason endReason, TimeSpan conversationDuration, int messageCount, int chatbotMessages, 
        int userMessages, bool wasEscalated = false, bool wasResolved = false, string? lastBotMessage = null, 
        string? lastUserMessage = null);
}

/// <summary>
/// Implementation of chatbot analytics service
/// </summary>
public class ChatbotAnalyticsService : IChatbotAnalyticsService
{
    private readonly IAnalyticsMessageBusService _analyticsMessageBus;
    private readonly ILogger<ChatbotAnalyticsService> _logger;

    public ChatbotAnalyticsService(IAnalyticsMessageBusService analyticsMessageBus, ILogger<ChatbotAnalyticsService> logger)
    {
        _analyticsMessageBus = analyticsMessageBus;
        _logger = logger;
    }

    public async Task PublishConversationStartedAsync(Guid conversationId, string? userId, string tenantId, string sessionId, 
        string? initialMessage = null, string? userAgent = null, string? ipAddress = null, string? referrer = null)
    {
        try
        {
            var analyticsEvent = new ChatbotConversationStartedEvent
            {
                ConversationId = conversationId.ToString(),
                UserId = userId,
                TenantId = tenantId,
                SessionId = sessionId,
                InitialMessage = initialMessage,
                UserAgent = userAgent,
                IpAddress = ipAddress,
                Referrer = referrer,
                Timestamp = DateTime.UtcNow
            };

            await _analyticsMessageBus.PublishAnalyticsEventAsync(analyticsEvent);
            _logger.LogDebug("Published chatbot conversation started event for conversation {ConversationId}", conversationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish chatbot conversation started event for conversation {ConversationId}", conversationId);
        }
    }

    public async Task PublishMessageSentAsync(Guid conversationId, string messageId, string messageText, string tenantId, 
        string sessionId, bool isIntentMatch = false, string? matchedIntent = null, double? confidenceScore = null, 
        string? responseSource = null)
    {
        try
        {
            var analyticsEvent = new ChatbotMessageSentEvent
            {
                ConversationId = conversationId.ToString(),
                MessageId = messageId,
                MessageText = messageText,
                TenantId = tenantId,
                SessionId = sessionId,
                MessageType = MessageType.Text,
                IsIntentMatch = isIntentMatch,
                MatchedIntent = matchedIntent,
                ConfidenceScore = confidenceScore,
                ResponseSource = responseSource,
                Timestamp = DateTime.UtcNow
            };

            await _analyticsMessageBus.PublishAnalyticsEventAsync(analyticsEvent);
            _logger.LogDebug("Published chatbot message sent event for message {MessageId}", messageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish chatbot message sent event for message {MessageId}", messageId);
        }
    }

    public async Task PublishMessageReceivedAsync(Guid conversationId, string messageId, string messageText, string? userId, 
        string tenantId, string sessionId, string? detectedLanguage = null, string? detectedSentiment = null)
    {
        try
        {
            var analyticsEvent = new ChatbotMessageReceivedEvent
            {
                ConversationId = conversationId.ToString(),
                MessageId = messageId,
                MessageText = messageText,
                UserId = userId,
                TenantId = tenantId,
                SessionId = sessionId,
                MessageType = MessageType.Text,
                MessageLength = messageText.Length,
                DetectedLanguage = detectedLanguage,
                DetectedSentiment = detectedSentiment,
                Timestamp = DateTime.UtcNow
            };

            await _analyticsMessageBus.PublishAnalyticsEventAsync(analyticsEvent);
            _logger.LogDebug("Published chatbot message received event for message {MessageId}", messageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish chatbot message received event for message {MessageId}", messageId);
        }
    }

    public async Task PublishIntentProcessedAsync(Guid conversationId, string messageId, string userMessage, string? userId, 
        string tenantId, string sessionId, string? detectedIntent = null, double? confidenceScore = null, 
        List<string>? extractedEntities = null, TimeSpan? processingTime = null, string? responseStrategy = null)
    {
        try
        {
            var analyticsEvent = new ChatbotIntentProcessedEvent
            {
                ConversationId = conversationId.ToString(),
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
                Timestamp = DateTime.UtcNow
            };

            await _analyticsMessageBus.PublishAnalyticsEventAsync(analyticsEvent);
            _logger.LogDebug("Published chatbot intent processed event for message {MessageId}", messageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish chatbot intent processed event for message {MessageId}", messageId);
        }
    }

    public async Task PublishEscalationRequestedAsync(Guid conversationId, string? userId, string tenantId, string sessionId, 
        string? escalationReason = null, string? triggerMessage = null, bool isAutomaticEscalation = false, 
        int messageCountBeforeEscalation = 0, TimeSpan? conversationDurationBeforeEscalation = null)
    {
        try
        {
            var analyticsEvent = new ChatbotEscalationRequestedEvent
            {
                ConversationId = conversationId.ToString(),
                UserId = userId,
                TenantId = tenantId,
                SessionId = sessionId,
                EscalationReason = escalationReason,
                TriggerMessage = triggerMessage,
                IsAutomaticEscalation = isAutomaticEscalation,
                MessageCountBeforeEscalation = messageCountBeforeEscalation,
                ConversationDurationBeforeEscalation = conversationDurationBeforeEscalation ?? TimeSpan.Zero,
                Timestamp = DateTime.UtcNow
            };

            await _analyticsMessageBus.PublishAnalyticsEventAsync(analyticsEvent);
            _logger.LogDebug("Published chatbot escalation requested event for conversation {ConversationId}", conversationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish chatbot escalation requested event for conversation {ConversationId}", conversationId);
        }
    }

    public async Task PublishFeedbackAsync(Guid conversationId, string messageId, string? userId, string tenantId, string sessionId, 
        FeedbackType feedbackType, int? rating = null, string? feedbackText = null, bool isHelpful = false)
    {
        try
        {
            var analyticsEvent = new ChatbotFeedbackEvent
            {
                ConversationId = conversationId.ToString(),
                MessageId = messageId,
                UserId = userId,
                TenantId = tenantId,
                SessionId = sessionId,
                FeedbackType = feedbackType,
                Rating = rating,
                FeedbackText = feedbackText,
                IsHelpful = isHelpful,
                Timestamp = DateTime.UtcNow
            };

            await _analyticsMessageBus.PublishAnalyticsEventAsync(analyticsEvent);
            _logger.LogDebug("Published chatbot feedback event for message {MessageId}", messageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish chatbot feedback event for message {MessageId}", messageId);
        }
    }

    public async Task PublishConversationEndedAsync(Guid conversationId, string? userId, string tenantId, string sessionId, 
        ConversationEndReason endReason, TimeSpan conversationDuration, int messageCount, int chatbotMessages, 
        int userMessages, bool wasEscalated = false, bool wasResolved = false, string? lastBotMessage = null, 
        string? lastUserMessage = null)
    {
        try
        {
            var analyticsEvent = new ChatbotConversationEndedEvent
            {
                ConversationId = conversationId.ToString(),
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
                Timestamp = DateTime.UtcNow
            };

            await _analyticsMessageBus.PublishAnalyticsEventAsync(analyticsEvent);
            _logger.LogDebug("Published chatbot conversation ended event for conversation {ConversationId}", conversationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish chatbot conversation ended event for conversation {ConversationId}", conversationId);
        }
    }
}