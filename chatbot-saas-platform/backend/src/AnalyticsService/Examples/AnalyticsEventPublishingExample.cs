using AnalyticsService.Services;
using Microsoft.Extensions.Logging;

namespace AnalyticsService.Examples;

/// <summary>
/// Example service showing how to publish analytics events using the new message bus pattern
/// This can be used as a reference for other services that want to publish analytics events
/// </summary>
public class AnalyticsEventPublishingExample
{
    private readonly IAnalyticsMessageBusService _analyticsMessageBus;
    private readonly ILogger<AnalyticsEventPublishingExample> _logger;

    public AnalyticsEventPublishingExample(
        IAnalyticsMessageBusService analyticsMessageBus,
        ILogger<AnalyticsEventPublishingExample> logger)
    {
        _analyticsMessageBus = analyticsMessageBus;
        _logger = logger;
    }

    /// <summary>
    /// Example: Publishing a conversation rating event from LiveAgentService
    /// </summary>
    public async Task PublishConversationRatingAsync(
        Guid conversationId,
        Guid? agentId,
        Guid? customerId,
        int rating,
        string? feedback,
        string ratedBy,
        string tenantId,
        string sessionId)
    {
        try
        {
            var ratingEvent = new ConversationRatingEvent
            {
                ConversationId = conversationId,
                AgentId = agentId,
                CustomerId = customerId,
                Rating = rating,
                Feedback = feedback,
                RatedBy = ratedBy,
                RatedAt = DateTime.UtcNow,
                TenantId = tenantId,
                SessionId = sessionId,
                Timestamp = DateTime.UtcNow
            };

            await _analyticsMessageBus.PublishAnalyticsEventAsync(ratingEvent);
            
            _logger.LogInformation("Published conversation rating event for conversation {ConversationId} with rating {Rating}", 
                conversationId, rating);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish conversation rating event for conversation {ConversationId}", conversationId);
        }
    }

    /// <summary>
    /// Example: Publishing an agent performance event
    /// </summary>
    public async Task PublishAgentPerformanceAsync(
        Guid agentId,
        string metricType,
        double value,
        string tenantId,
        string sessionId,
        TimeSpan? period = null,
        Dictionary<string, object>? performanceData = null)
    {
        try
        {
            var performanceEvent = new AgentPerformanceEvent
            {
                AgentId = agentId,
                MetricType = metricType,
                Value = value,
                MeasuredAt = DateTime.UtcNow,
                Period = period,
                PerformanceData = performanceData ?? new Dictionary<string, object>(),
                TenantId = tenantId,
                SessionId = sessionId,
                Timestamp = DateTime.UtcNow
            };

            await _analyticsMessageBus.PublishAnalyticsEventAsync(performanceEvent);
            
            _logger.LogInformation("Published agent performance event for agent {AgentId}, metric {MetricType}, value {Value}", 
                agentId, metricType, value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish agent performance event for agent {AgentId}", agentId);
        }
    }

    /// <summary>
    /// Example: Publishing a customer satisfaction event
    /// </summary>
    public async Task PublishCustomerSatisfactionAsync(
        Guid customerId,
        Guid conversationId,
        int satisfactionScore,
        string tenantId,
        string sessionId,
        string? feedbackText = null,
        List<string>? satisfactionCategories = null,
        bool? wouldRecommend = null,
        string? improvementSuggestions = null)
    {
        try
        {
            var satisfactionEvent = new CustomerSatisfactionEvent
            {
                CustomerId = customerId,
                ConversationId = conversationId,
                SatisfactionScore = satisfactionScore,
                FeedbackText = feedbackText,
                SatisfactionCategories = satisfactionCategories,
                WouldRecommend = wouldRecommend,
                ImprovementSuggestions = improvementSuggestions,
                SurveyCompletedAt = DateTime.UtcNow,
                TenantId = tenantId,
                SessionId = sessionId,
                Timestamp = DateTime.UtcNow
            };

            await _analyticsMessageBus.PublishAnalyticsEventAsync(satisfactionEvent);
            
            _logger.LogInformation("Published customer satisfaction event for customer {CustomerId}, conversation {ConversationId}, score {Score}", 
                customerId, conversationId, satisfactionScore);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish customer satisfaction event for customer {CustomerId}", customerId);
        }
    }
}