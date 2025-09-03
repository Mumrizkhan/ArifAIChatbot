using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Shared.Infrastructure.Messaging;
using AnalyticsService.Events;
using AnalyticsService.Models;
using System.Text.Json;

namespace AnalyticsService.Services;

/// <summary>
/// Service for handling analytics events through message bus similar to NotificationMessageBusService
/// Uses background task processing instead of Hangfire for simplicity
/// </summary>
public interface IAnalyticsMessageBusService
{
    void InitializeSubscriptions();
    Task PublishAnalyticsEventAsync<T>(T analyticsEvent) where T : class, IAnalyticsEvent;
}

/// <summary>
/// Implementation of analytics message bus service using background task processing
/// </summary>
public class AnalyticsMessageBusService : IAnalyticsMessageBusService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IMessageBus _messageBus;
    private readonly ILogger<AnalyticsMessageBusService> _logger;

    public AnalyticsMessageBusService(
        IServiceProvider serviceProvider,
        IMessageBus messageBus,
        ILogger<AnalyticsMessageBusService> logger)
    {
        _serviceProvider = serviceProvider;
        _messageBus = messageBus;
        _logger = logger;
    }

    /// <summary>
    /// Initialize message bus subscriptions using background task processing
    /// </summary>
    public void InitializeSubscriptions()
    {
        _logger.LogInformation("Initializing Analytics Message Bus subscriptions");

        try
        {
            // Subscribe to chatbot analytics events
            SubscribeToChatbotEvents();
            
            // Subscribe to live agent analytics events
            SubscribeToLiveAgentEvents();
            
            // Subscribe to conversation rating events
            SubscribeToRatingEvents();

            _logger.LogInformation("Analytics message bus subscriptions initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize analytics message bus subscriptions");
            throw;
        }
    }

    /// <summary>
    /// Publish an analytics event to the message bus
    /// </summary>
    public async Task PublishAnalyticsEventAsync<T>(T analyticsEvent) where T : class, IAnalyticsEvent
    {
        try
        {
            await _messageBus.PublishAsync(analyticsEvent, analyticsEvent.EventType, "analytics");
            _logger.LogDebug("Published analytics event {EventType} for conversation {ConversationId}", 
                analyticsEvent.EventType, analyticsEvent.ConversationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish analytics event {EventType}", analyticsEvent.EventType);
            // Don't throw - analytics shouldn't break business logic
        }
    }

    #region Private Subscription Methods

    private void SubscribeToChatbotEvents()
    {
        // Chatbot conversation started
        _messageBus.SubscribeAsync<ChatbotConversationStartedEvent>(
            async (chatbotEvent) =>
            {
                _ = Task.Run(() => ProcessChatbotConversationStartedAsync(chatbotEvent));
                return true;
            },
            "analytics-service.chatbot-conversation-started",
            "analytics",
            "chatbot.conversation.started"
        );

        // Chatbot message sent
        _messageBus.SubscribeAsync<ChatbotMessageSentEvent>(
            async (messageEvent) =>
            {
                _ = Task.Run(() => ProcessChatbotMessageSentAsync(messageEvent));
                return true;
            },
            "analytics-service.chatbot-message-sent",
            "analytics",
            "chatbot.message.sent"
        );

        // Chatbot message received
        _messageBus.SubscribeAsync<ChatbotMessageReceivedEvent>(
            async (messageEvent) =>
            {
                _ = Task.Run(() => ProcessEventAsync(messageEvent, "chatbot message received"));
                return true;
            },
            "analytics-service.chatbot-message-received",
            "analytics",
            "chatbot.message.received"
        );

        // Chatbot intent processed
        _messageBus.SubscribeAsync<ChatbotIntentProcessedEvent>(
            async (intentEvent) =>
            {
                _ = Task.Run(() => ProcessEventAsync(intentEvent, "chatbot intent processed"));
                return true;
            },
            "analytics-service.chatbot-intent-processed",
            "analytics",
            "chatbot.intent.processed"
        );

        // Chatbot escalation requested
        _messageBus.SubscribeAsync<ChatbotEscalationRequestedEvent>(
            async (escalationEvent) =>
            {
                _ = Task.Run(() => ProcessEventAsync(escalationEvent, "chatbot escalation requested"));
                return true;
            },
            "analytics-service.chatbot-escalation-requested",
            "analytics",
            "chatbot.escalation.requested"
        );

        // Chatbot feedback
        _messageBus.SubscribeAsync<ChatbotFeedbackEvent>(
            async (feedbackEvent) =>
            {
                _ = Task.Run(() => ProcessEventAsync(feedbackEvent, "chatbot feedback"));
                return true;
            },
            "analytics-service.chatbot-feedback",
            "analytics",
            "chatbot.feedback"
        );

        // Chatbot conversation ended
        _messageBus.SubscribeAsync<ChatbotConversationEndedEvent>(
            async (endedEvent) =>
            {
                _ = Task.Run(() => ProcessEventAsync(endedEvent, "chatbot conversation ended"));
                return true;
            },
            "analytics-service.chatbot-conversation-ended",
            "analytics",
            "chatbot.conversation.ended"
        );

        _logger.LogDebug("Subscribed to all chatbot analytics events");
    }

    private void SubscribeToLiveAgentEvents()
    {
        // Live agent requested
        _messageBus.SubscribeAsync<LiveAgentRequestedEvent>(
            async (agentEvent) =>
            {
                _ = Task.Run(() => ProcessLiveAgentRequestedAsync(agentEvent));
                return true;
            },
            "analytics-service.live-agent-requested",
            "analytics",
            "liveagent.requested"
        );

        // Live agent assigned
        _messageBus.SubscribeAsync<LiveAgentAssignedEvent>(
            async (assignedEvent) =>
            {
                _ = Task.Run(() => ProcessLiveAgentAssignedAsync(assignedEvent));
                return true;
            },
            "analytics-service.live-agent-assigned",
            "analytics",
            "liveagent.assigned"
        );

        // Live agent joined
        _messageBus.SubscribeAsync<LiveAgentJoinedEvent>(
            async (joinedEvent) =>
            {
                _ = Task.Run(() => ProcessEventAsync(joinedEvent, "live agent joined"));
                return true;
            },
            "analytics-service.live-agent-joined",
            "analytics",
            "liveagent.joined"
        );

        // Live agent left
        _messageBus.SubscribeAsync<LiveAgentLeftEvent>(
            async (leftEvent) =>
            {
                _ = Task.Run(() => ProcessEventAsync(leftEvent, "live agent left"));
                return true;
            },
            "analytics-service.live-agent-left",
            "analytics",
            "liveagent.left"
        );

        // Live agent message sent
        _messageBus.SubscribeAsync<LiveAgentMessageSentEvent>(
            async (messageEvent) =>
            {
                _ = Task.Run(() => ProcessEventAsync(messageEvent, "live agent message sent"));
                return true;
            },
            "analytics-service.live-agent-message-sent",
            "analytics",
            "liveagent.message.sent"
        );

        // Live agent message received
        _messageBus.SubscribeAsync<LiveAgentMessageReceivedEvent>(
            async (messageEvent) =>
            {
                _ = Task.Run(() => ProcessEventAsync(messageEvent, "live agent message received"));
                return true;
            },
            "analytics-service.live-agent-message-received",
            "analytics",
            "liveagent.message.received"
        );

        // Live agent escalation
        _messageBus.SubscribeAsync<LiveAgentEscalationEvent>(
            async (escalationEvent) =>
            {
                _ = Task.Run(() => ProcessEventAsync(escalationEvent, "live agent escalation"));
                return true;
            },
            "analytics-service.live-agent-escalation",
            "analytics",
            "liveagent.escalation"
        );

        // Live agent transfer
        _messageBus.SubscribeAsync<LiveAgentTransferEvent>(
            async (transferEvent) =>
            {
                _ = Task.Run(() => ProcessEventAsync(transferEvent, "live agent transfer"));
                return true;
            },
            "analytics-service.live-agent-transfer",
            "analytics",
            "liveagent.transfer"
        );

        // Live agent conversation ended
        _messageBus.SubscribeAsync<LiveAgentConversationEndedEvent>(
            async (endedEvent) =>
            {
                _ = Task.Run(() => ProcessEventAsync(endedEvent, "live agent conversation ended"));
                return true;
            },
            "analytics-service.live-agent-conversation-ended",
            "analytics",
            "liveagent.conversation.ended"
        );

        // Live agent feedback
        _messageBus.SubscribeAsync<LiveAgentFeedbackEvent>(
            async (feedbackEvent) =>
            {
                _ = Task.Run(() => ProcessEventAsync(feedbackEvent, "live agent feedback"));
                return true;
            },
            "analytics-service.live-agent-feedback",
            "analytics",
            "liveagent.feedback"
        );

        // Live agent response time
        _messageBus.SubscribeAsync<LiveAgentResponseTimeEvent>(
            async (responseTimeEvent) =>
            {
                _ = Task.Run(() => ProcessEventAsync(responseTimeEvent, "live agent response time"));
                return true;
            },
            "analytics-service.live-agent-response-time",
            "analytics",
            "liveagent.responsetime"
        );

        // Live agent session duration
        _messageBus.SubscribeAsync<LiveAgentSessionDurationEvent>(
            async (sessionEvent) =>
            {
                _ = Task.Run(() => ProcessEventAsync(sessionEvent, "live agent session duration"));
                return true;
            },
            "analytics-service.live-agent-session-duration",
            "analytics",
            "liveagent.session.duration"
        );

        _logger.LogDebug("Subscribed to all live agent analytics events");
    }

    private void SubscribeToRatingEvents()
    {
        // Subscribe to conversation rating events from LiveAgentService
        _messageBus.SubscribeAsync<ConversationRatingEvent>(
            async (ratingEvent) =>
            {
                _ = Task.Run(() => ProcessConversationRatingAsync(ratingEvent));
                return true;
            },
            "analytics-service.conversation-rating",
            "analytics",
            "conversation.rating"
        );

        _logger.LogDebug("Subscribed to rating analytics events");
    }

    #endregion

    #region Event Processing Methods

    public async Task ProcessChatbotConversationStartedAsync(ChatbotConversationStartedEvent chatbotEvent)
    {
        try
        {
            _logger.LogInformation("Processing chatbot conversation started event: {ConversationId}", chatbotEvent.ConversationId);
            
            using var scope = _serviceProvider.CreateScope();
            var analyticsService = scope.ServiceProvider.GetRequiredService<IAnalyticsService>();
            
            var analyticsEventRequest = new AnalyticsEventBatchRequest
            {
                Events = new List<AnalyticsEventDto>
                {
                    new AnalyticsEventDto
                    {
                        EventType = chatbotEvent.EventType,
                        EventData = new Dictionary<string, object>
                        {
                            ["initialMessage"] = chatbotEvent.InitialMessage ?? "",
                            ["userAgent"] = chatbotEvent.UserAgent ?? "",
                            ["ipAddress"] = chatbotEvent.IpAddress ?? "",
                            ["referrer"] = chatbotEvent.Referrer ?? ""
                        },
                        Timestamp = chatbotEvent.Timestamp,
                        ConversationId = chatbotEvent.ConversationId,
                        UserId = chatbotEvent.UserId,
                        SessionId = chatbotEvent.SessionId,
                        Metadata = chatbotEvent.Metadata ?? new Dictionary<string, object>()
                    }
                }
            };
            
            await analyticsService.TrackEventsBatchAsync(analyticsEventRequest, chatbotEvent.TenantId);
            
            _logger.LogInformation("Successfully processed chatbot conversation started event: {ConversationId}", chatbotEvent.ConversationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process chatbot conversation started event: {ConversationId}", chatbotEvent.ConversationId);
        }
    }

    public async Task ProcessChatbotMessageSentAsync(ChatbotMessageSentEvent messageEvent)
    {
        try
        {
            _logger.LogInformation("Processing chatbot message sent event: {MessageId}", messageEvent.MessageId);
            
            using var scope = _serviceProvider.CreateScope();
            var analyticsService = scope.ServiceProvider.GetRequiredService<IAnalyticsService>();
            
            var analyticsEventRequest = new AnalyticsEventBatchRequest
            {
                Events = new List<AnalyticsEventDto>
                {
                    new AnalyticsEventDto
                    {
                        EventType = messageEvent.EventType,
                        EventData = new Dictionary<string, object>
                        {
                            ["messageId"] = messageEvent.MessageId,
                            ["messageText"] = messageEvent.MessageText,
                            ["messageType"] = messageEvent.MessageType.ToString(),
                            ["isIntentMatch"] = messageEvent.IsIntentMatch,
                            ["matchedIntent"] = messageEvent.MatchedIntent ?? "",
                            ["confidenceScore"] = messageEvent.ConfidenceScore ?? 0,
                            ["responseSource"] = messageEvent.ResponseSource ?? ""
                        },
                        Timestamp = messageEvent.Timestamp,
                        ConversationId = messageEvent.ConversationId,
                        UserId = messageEvent.UserId,
                        SessionId = messageEvent.SessionId,
                        Metadata = messageEvent.Metadata ?? new Dictionary<string, object>()
                    }
                }
            };
            
            await analyticsService.TrackEventsBatchAsync(analyticsEventRequest, messageEvent.TenantId);
            
            _logger.LogInformation("Successfully processed chatbot message sent event: {MessageId}", messageEvent.MessageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process chatbot message sent event: {MessageId}", messageEvent.MessageId);
        }
    }

    public async Task ProcessLiveAgentRequestedAsync(LiveAgentRequestedEvent agentEvent)
    {
        await ProcessEventAsync(agentEvent, "live agent requested");
    }

    public async Task ProcessLiveAgentAssignedAsync(LiveAgentAssignedEvent assignedEvent)
    {
        await ProcessEventAsync(assignedEvent, "live agent assigned");
    }

    public async Task ProcessConversationRatingAsync(ConversationRatingEvent ratingEvent)
    {
        try
        {
            _logger.LogInformation("Processing conversation rating event: {ConversationId}", ratingEvent.ConversationId);
            
            using var scope = _serviceProvider.CreateScope();
            var analyticsService = scope.ServiceProvider.GetRequiredService<IAnalyticsService>();
            
            var analyticsEventRequest = new AnalyticsEventBatchRequest
            {
                Events = new List<AnalyticsEventDto>
                {
                    new AnalyticsEventDto
                    {
                        EventType = ratingEvent.EventType,
                        EventData = new Dictionary<string, object>
                        {
                            ["conversationId"] = ratingEvent.ConversationId.ToString(),
                            ["agentId"] = ratingEvent.AgentId?.ToString() ?? "",
                            ["customerId"] = ratingEvent.CustomerId?.ToString() ?? "",
                            ["rating"] = ratingEvent.Rating,
                            ["feedback"] = ratingEvent.Feedback ?? "",
                            ["ratedBy"] = ratingEvent.RatedBy,
                            ["ratedAt"] = ratingEvent.RatedAt,
                            ["conversationDurationMs"] = ratingEvent.ConversationDuration?.TotalMilliseconds ?? 0,
                            ["messageCount"] = ratingEvent.MessageCount ?? 0,
                            ["wasResolved"] = ratingEvent.WasResolved ?? false,
                            ["resolutionCategory"] = ratingEvent.ResolutionCategory ?? ""
                        },
                        Timestamp = ratingEvent.Timestamp,
                        ConversationId = ratingEvent.ConversationId.ToString(),
                        AgentId = ratingEvent.AgentId?.ToString(),
                        UserId = ratingEvent.CustomerId?.ToString(),
                        SessionId = ratingEvent.SessionId,
                        Metadata = ratingEvent.Metadata ?? new Dictionary<string, object>()
                    }
                }
            };
            
            await analyticsService.TrackEventsBatchAsync(analyticsEventRequest, ratingEvent.TenantId);
            
            _logger.LogInformation("Successfully processed conversation rating event: {ConversationId}", ratingEvent.ConversationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process conversation rating event: {ConversationId}", ratingEvent.ConversationId);
        }
    }

    #endregion

    #region Helper Methods

    private async Task ProcessEventAsync(IAnalyticsEvent analyticsEvent, string eventDescription)
    {
        try
        {
            _logger.LogDebug("Processing {EventDescription} event: {ConversationId}", eventDescription, analyticsEvent.ConversationId);
            
            using var scope = _serviceProvider.CreateScope();
            var analyticsService = scope.ServiceProvider.GetRequiredService<IAnalyticsService>();
            
            var analyticsEventRequest = new AnalyticsEventBatchRequest
            {
                Events = new List<AnalyticsEventDto>
                {
                    new AnalyticsEventDto
                    {
                        EventType = analyticsEvent.EventType,
                        EventData = new Dictionary<string, object>
                        {
                            ["eventDescription"] = eventDescription,
                            ["processed"] = true
                        },
                        Timestamp = analyticsEvent.Timestamp,
                        ConversationId = analyticsEvent.ConversationId,
                        AgentId = analyticsEvent.AgentId,
                        UserId = analyticsEvent.UserId,
                        SessionId = analyticsEvent.SessionId,
                        Metadata = analyticsEvent.Metadata ?? new Dictionary<string, object>()
                    }
                }
            };
            
            await analyticsService.TrackEventsBatchAsync(analyticsEventRequest, analyticsEvent.TenantId);
            
            _logger.LogDebug("Successfully processed {EventDescription} event: {ConversationId}", eventDescription, analyticsEvent.ConversationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process {EventDescription} event: {ConversationId}", eventDescription, analyticsEvent.ConversationId);
        }
    }

    #endregion
}

/// <summary>
/// Rating specific events that can be published by other services
/// </summary>
public record ConversationRatingEvent : AnalyticsEventBase
{
    public override string EventType => "conversation.rated";
    
    public required Guid ConversationId { get; init; }
    public Guid? AgentId { get; init; }
    public Guid? CustomerId { get; init; }
    public required int Rating { get; init; } // 1-5 scale
    public string? Feedback { get; init; }
    public required string RatedBy { get; init; } // "Customer" or "Agent"
    public required DateTime RatedAt { get; init; }
    public TimeSpan? ConversationDuration { get; init; }
    public int? MessageCount { get; init; }
    public bool? WasResolved { get; init; }
    public string? ResolutionCategory { get; init; }
}

public record AgentPerformanceEvent : AnalyticsEventBase
{
    public override string EventType => "agent.performance";
    
    public required Guid AgentId { get; init; }
    public required string MetricType { get; init; } // "rating", "response_time", "resolution_rate"
    public required double Value { get; init; }
    public required DateTime MeasuredAt { get; init; }
    public TimeSpan? Period { get; init; }
    public Dictionary<string, object> PerformanceData { get; init; } = new();
}

public record CustomerSatisfactionEvent : AnalyticsEventBase
{
    public override string EventType => "customer.satisfaction";
    
    public required Guid CustomerId { get; init; }
    public required Guid ConversationId { get; init; }
    public required int SatisfactionScore { get; init; } // 1-5 scale
    public string? FeedbackText { get; init; }
    public List<string>? SatisfactionCategories { get; init; }
    public bool? WouldRecommend { get; init; }
    public string? ImprovementSuggestions { get; init; }
    public required DateTime SurveyCompletedAt { get; init; }
}