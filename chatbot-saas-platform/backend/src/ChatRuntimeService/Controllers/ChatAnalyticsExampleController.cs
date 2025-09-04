using Microsoft.AspNetCore.Mvc;
using ChatRuntimeService.Services;

using Shared.Application.Common.Models;

namespace ChatRuntimeService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatAnalyticsExampleController : ControllerBase
{
    private readonly IChatbotAnalyticsService _analyticsService;
    private readonly ILogger<ChatAnalyticsExampleController> _logger;

    public ChatAnalyticsExampleController(
        IChatbotAnalyticsService analyticsService,
        ILogger<ChatAnalyticsExampleController> logger)
    {
        _analyticsService = analyticsService;
        _logger = logger;
    }

    /// <summary>
    /// Example: Start a conversation and track analytics
    /// </summary>
    [HttpPost("start-conversation")]
    public async Task<IActionResult> StartConversationWithAnalytics([FromBody] StartConversationAnalyticsRequest request)
    {
        try
        {
            var conversationId = Guid.NewGuid();
            var sessionId = Guid.NewGuid().ToString();

            // Publish conversation started event
            await _analyticsService.PublishConversationStartedAsync(
                conversationId,
                request.UserId,
                request.TenantId,
                sessionId,
                request.InitialMessage,
                Request.Headers.UserAgent,
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                Request.Headers.Referer
            );

            return Ok(new { ConversationId = conversationId, SessionId = sessionId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting conversation with analytics");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Example: Send a bot message with analytics
    /// </summary>
    [HttpPost("send-bot-message")]
    public async Task<IActionResult> SendBotMessageWithAnalytics([FromBody] SendBotMessageAnalyticsRequest request)
    {
        try
        {
            var messageId = Guid.NewGuid().ToString();

            // Publish message sent event
            await _analyticsService.PublishMessageSentAsync(
                request.ConversationId,
                messageId,
                request.MessageText,
                request.TenantId,
                request.SessionId,
                request.IsIntentMatch,
                request.MatchedIntent,
                request.ConfidenceScore,
                request.ResponseSource
            );

            return Ok(new { MessageId = messageId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending bot message with analytics");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Example: Process user feedback
    /// </summary>
    [HttpPost("feedback")]
    public async Task<IActionResult> SubmitFeedbackWithAnalytics([FromBody] FeedbackAnalyticsRequest request)
    {
        try
        {
            // Publish feedback event
            await _analyticsService.PublishFeedbackAsync(
                request.ConversationId,
                request.MessageId,
                request.UserId,
                request.TenantId,
                request.SessionId,               
                request.FeedbackType,
                request.Rating,
                request.FeedbackText,
                request.IsHelpful
            );

            return Ok(new { Message = "Feedback submitted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting feedback with analytics");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Example: Process intent recognition
    /// </summary>
    [HttpPost("process-intent")]
    public async Task<IActionResult> ProcessIntentWithAnalytics([FromBody] ProcessIntentAnalyticsRequest request)
    {
        try
        {
            var processingStartTime = DateTime.UtcNow;
            
            // Simulate intent processing...
            await Task.Delay(100); // Simulate processing time
            
            var processingTime = DateTime.UtcNow - processingStartTime;

            // Publish intent processed event
            await _analyticsService.PublishIntentProcessedAsync(
                request.ConversationId,
                request.MessageId,
                request.UserMessage,
                request.UserId,
                request.TenantId,
                request.SessionId,
                request.DetectedIntent,
                request.ConfidenceScore,
                request.ExtractedEntities,
                processingTime,
                request.ResponseStrategy
            );

            return Ok(new { ProcessingTime = processingTime.TotalMilliseconds });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing intent with analytics");
            return StatusCode(500, "Internal server error");
        }
    }
}