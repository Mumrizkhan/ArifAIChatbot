# Analytics Events Integration Guide

## Overview

This document describes the comprehensive analytics events integration for both Chatbot and Live Agent interactions in the SaaS platform. The integration follows the same pattern as the NotificationMessageBusService, providing scalable and reliable analytics event processing.

## Architecture

```
ChatRuntimeService/LiveAgentService ? Analytics Events ? Message Bus ? AnalyticsService ? Database
                                                    ?
                            Background Processing ? Reports & Dashboards
```

## Analytics Events Supported

### Chatbot Events

| Event Type | Purpose | Key Data |
|------------|---------|----------|
| `chatbot.conversation.started` | Track new conversation initiation | Initial message, user agent, IP, referrer |
| `chatbot.message.sent` | Track bot responses | Message content, intent match, confidence, source |
| `chatbot.message.received` | Track user messages | Message content, length, detected language/sentiment |
| `chatbot.intent.processed` | Track NLP processing | Detected intent, confidence, entities, processing time |
| `chatbot.escalation.requested` | Track escalation requests | Reason, trigger message, conversation duration |
| `chatbot.feedback` | Track user feedback | Rating, feedback type, helpfulness |
| `chatbot.conversation.ended` | Track conversation completion | End reason, duration, message counts, resolution status |

### Live Agent Events

| Event Type | Purpose | Key Data |
|------------|---------|----------|
| `liveagent.requested` | Track agent support requests | Reason, queue position, department, priority |
| `liveagent.assigned` | Track agent assignments | Wait time, assignment method, department |
| `liveagent.joined` | Track agent joining conversations | Response time, transfer status |
| `liveagent.left` | Track agent leaving conversations | Session duration, message count, leave reason |
| `liveagent.message.sent` | Track agent messages | Response time, message intent, quality |
| `liveagent.message.received` | Track customer messages to agents | Sentiment, language, response requirement |
| `liveagent.escalation` | Track escalations between agents | Duration, message count, departments |
| `liveagent.transfer` | Track agent transfers | Transfer reason, warm/cold transfer, notes |
| `liveagent.conversation.ended` | Track conversation completion | Resolution status, follow-up requirements |
| `liveagent.feedback` | Track customer satisfaction | Rating, satisfaction level, categories |
| `liveagent.responsetime` | Track response time metrics | Response category, quality assessment |
| `liveagent.session.duration` | Track agent session metrics | Active periods, idle time, session quality |

## Implementation Components

### Core Services

#### 1. ChatbotAnalyticsService (`ChatRuntimeService`)
- **Location**: `src/ChatRuntimeService/Services/ChatbotAnalyticsService.cs`
- **Purpose**: Publishes chatbot-related analytics events
- **Key Methods**:
  - `PublishConversationStartedAsync()`
  - `PublishMessageSentAsync()`
  - `PublishMessageReceivedAsync()`
  - `PublishIntentProcessedAsync()`
  - `PublishEscalationRequestedAsync()`
  - `PublishFeedbackAsync()`
  - `PublishConversationEndedAsync()`

#### 2. LiveAgentAnalyticsService (`LiveAgentService`)
- **Location**: `src/LiveAgentService/Services/LiveAgentAnalyticsService.cs`
- **Purpose**: Publishes live agent-related analytics events
- **Key Methods**:
  - `PublishAgentRequestedAsync()`
  - `PublishAgentAssignedAsync()`
  - `PublishAgentJoinedAsync()`
  - `PublishAgentLeftAsync()`
  - `PublishMessageSentAsync()`
  - `PublishMessageReceivedAsync()`
  - `PublishEscalationAsync()`
  - `PublishTransferAsync()`
  - `PublishConversationEndedAsync()`
  - `PublishFeedbackAsync()`
  - `PublishResponseTimeAsync()`
  - `PublishSessionDurationAsync()`

#### 3. AnalyticsMessageBusService (`AnalyticsService`)
- **Location**: `src/AnalyticsService/Services/AnalyticsMessageBusService.cs`
- **Purpose**: Handles incoming analytics events and processes them
- **Features**:
  - Subscribes to all analytics events
  - Background processing using `Task.Run()`
  - Error handling and logging
  - Event data transformation

### Integration Points

#### ChatRuntimeService Integration
```csharp
// In ChatService.cs
public class ChatService : IChatService
{
    private readonly IChatbotAnalyticsService _chatbotAnalytics;
    
    public async Task<Conversation> StartConversationAsync(StartConversationRequest request, Guid tenantId)
    {
        // ... business logic ...
        
        // Publish analytics event
        await _chatbotAnalytics.PublishConversationStartedAsync(
            conversation.Id,
            request.CustomerEmail,
            tenantId.ToString(),
            sessionId,
            request.InitialMessage,
            request.UserAgent,
            request.IpAddress,
            request.Referrer
        );
        
        // ... rest of method ...
    }
}
```

#### LiveAgentService Integration
```csharp
// In AgentHub.cs
public class AgentHub : Hub
{
    private readonly ILiveAgentAnalyticsService _liveAgentAnalytics;
    
    public async Task SendMessage(string conversationId, string content, string messageType = "text")
    {
        // ... business logic ...
        
        // Publish analytics event
        await _liveAgentAnalytics.PublishMessageSentAsync(
            convId,
            message.Id.ToString(),
            userId.ToString(),
            conversation.CustomerEmail,
            tenantId.ToString(),
            sessionId,
            MessageType.Text,
            content.Length,
            responseTime,
            isFirstMessage,
            DetermineMessageIntent(content)
        );
        
        // ... rest of method ...
    }
}
```

## Configuration

### Service Registration

#### ChatRuntimeService (`Program.cs`)
```csharp
// Add Analytics Integration
builder.Services.AddScoped<IChatbotAnalyticsService, ChatbotAnalyticsService>();
builder.Services.AddScoped<IAnalyticsMessageBusService, AnalyticsMessageBusService>();

// Initialize Analytics Message Bus
using (var scope = app.Services.CreateScope())
{
    var analyticsMessageBus = scope.ServiceProvider.GetRequiredService<IAnalyticsMessageBusService>();
    analyticsMessageBus.InitializeSubscriptions();
}
```

#### LiveAgentService (`Program.cs`)
```csharp
// Add Analytics Integration
builder.Services.AddScoped<ILiveAgentAnalyticsService, LiveAgentAnalyticsService>();
builder.Services.AddScoped<IAnalyticsMessageBusService, AnalyticsMessageBusService>();

// Initialize Analytics Message Bus
using (var scope = app.Services.CreateScope())
{
    var analyticsMessageBus = scope.ServiceProvider.GetRequiredService<IAnalyticsMessageBusService>();
    analyticsMessageBus.InitializeSubscriptions();
}
```

#### AnalyticsService (`Program.cs`)
```csharp
// Add Analytics Message Bus Services
builder.Services.AddAnalyticsMessageBus();

// Initialize Analytics Message Bus
app.InitializeAnalyticsMessageBus();
```

## Usage Examples

### Chatbot Analytics

#### Track Conversation Start
```csharp
await _chatbotAnalytics.PublishConversationStartedAsync(
    conversationId: Guid.NewGuid(),
    userId: "user@example.com",
    tenantId: "tenant-123",
    sessionId: "session-456",
    initialMessage: "Hello, I need help",
    userAgent: "Mozilla/5.0...",
    ipAddress: "192.168.1.1",
    referrer: "https://website.com"
);
```

#### Track Bot Response
```csharp
await _chatbotAnalytics.PublishMessageSentAsync(
    conversationId: conversationId,
    messageId: "msg-123",
    messageText: "How can I help you today?",
    tenantId: "tenant-123",
    sessionId: "session-456",
    isIntentMatch: true,
    matchedIntent: "greeting",
    confidenceScore: 0.95,
    responseSource: "knowledge_base"
);
```

#### Track Intent Processing
```csharp
await _chatbotAnalytics.PublishIntentProcessedAsync(
    conversationId: conversationId,
    messageId: "msg-124",
    userMessage: "I need help with billing",
    userId: "user@example.com",
    tenantId: "tenant-123",
    sessionId: "session-456",
    detectedIntent: "billing_inquiry",
    confidenceScore: 0.87,
    extractedEntities: new List<string> { "billing", "invoice" },
    processingTime: TimeSpan.FromMilliseconds(150),
    responseStrategy: "search"
);
```

### Live Agent Analytics

#### Track Agent Assignment
```csharp
await _liveAgentAnalytics.PublishAgentAssignedAsync(
    conversationId: conversationId,
    agentId: "agent-123",
    agentName: "John Doe",
    userId: "customer@example.com",
    tenantId: "tenant-123",
    sessionId: "session-789",
    waitTime: TimeSpan.FromMinutes(2),
    queuePosition: 3,
    assignmentMethod: "skill_based",
    department: "support"
);
```

#### Track Agent Message with Response Time
```csharp
await _liveAgentAnalytics.PublishMessageSentAsync(
    conversationId: conversationId,
    messageId: "msg-200",
    agentId: "agent-123",
    userId: "customer@example.com",
    tenantId: "tenant-123",
    sessionId: "session-789",
    messageType: MessageType.Text,
    messageLength: 45,
    responseTime: TimeSpan.FromSeconds(30),
    isFirstMessage: true,
    messageIntent: "greeting"
);

await _liveAgentAnalytics.PublishResponseTimeAsync(
    conversationId: conversationId,
    messageId: "msg-200",
    agentId: "agent-123",
    userId: "customer@example.com",
    tenantId: "tenant-123",
    sessionId: "session-789",
    responseTime: TimeSpan.FromSeconds(30),
    isFirstResponse: true,
    category: ResponseTimeCategory.Quick,
    responseQuality: "helpful"
);
```

#### Track Customer Feedback
```csharp
await _liveAgentAnalytics.PublishFeedbackAsync(
    conversationId: conversationId,
    agentId: "agent-123",
    userId: "customer@example.com",
    tenantId: "tenant-123",
    sessionId: "session-789",
    rating: 5,
    satisfaction: SatisfactionLevel.VerySatisfied,
    feedbackText: "Great service!",
    feedbackCategories: new List<string> { "helpful", "professional", "quick" },
    wouldRecommend: true,
    improvementSuggestions: null
);
```

## Event Processing Flow

1. **Event Generation**: Services generate analytics events during user interactions
2. **Message Publishing**: Events are published to RabbitMQ message bus
3. **Event Subscription**: AnalyticsService subscribes to all analytics events
4. **Background Processing**: Events are processed asynchronously using `Task.Run()`
5. **Data Storage**: Processed events are stored in the analytics database
6. **Dashboard Updates**: Analytics data becomes available for reporting and dashboards

## Error Handling

- Analytics events are designed to be **non-blocking**
- Failed analytics events do not interrupt business logic
- Comprehensive logging for troubleshooting
- Retry mechanisms for transient failures
- Circuit breaker patterns for message bus reliability

## Performance Considerations

- **Asynchronous Processing**: All analytics events are processed asynchronously
- **Background Tasks**: Using `Task.Run()` to prevent blocking main threads
- **Batch Processing**: Events can be batched for improved performance
- **Message Bus Queuing**: RabbitMQ provides reliable message queuing
- **Database Optimization**: Efficient storage and indexing strategies

## Monitoring and Observability

### Metrics to Monitor
- Event publication rates
- Processing latencies
- Failed event counts
- Message queue depths
- Database performance

### Logging
- Event publication success/failure
- Processing times
- Error rates and details
- Business metric calculations

## Future Enhancements

1. **Real-time Dashboards**: WebSocket-based real-time analytics updates
2. **Event Aggregation**: Pre-computed metrics for better performance
3. **Alerting**: Automated alerts based on analytics thresholds
4. **Machine Learning**: Predictive analytics and anomaly detection
5. **Data Retention**: Automated cleanup and archival policies

## Troubleshooting

### Common Issues

1. **Events not appearing**: Check message bus connectivity and subscriptions
2. **Performance issues**: Monitor queue depths and processing times
3. **Data inconsistencies**: Verify event ordering and processing logic
4. **Missing events**: Check error logs and retry mechanisms

### Debug Tools

- Analytics event example controllers for testing
- Message bus monitoring tools
- Database query tools for event verification
- Logging aggregation for error analysis

## Conclusion

The analytics events integration provides comprehensive tracking of both chatbot and live agent interactions, enabling detailed insights into customer engagement, agent performance, and system efficiency. The system is designed for scalability, reliability, and ease of use while maintaining high performance standards.