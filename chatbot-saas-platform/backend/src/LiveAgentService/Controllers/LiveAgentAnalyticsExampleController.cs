using Microsoft.AspNetCore.Mvc;
using LiveAgentService.Services;
using AnalyticsService.Events;

namespace LiveAgentService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LiveAgentAnalyticsExampleController : ControllerBase
{
    private readonly ILiveAgentAnalyticsService _analyticsService;
    private readonly ILogger<LiveAgentAnalyticsExampleController> _logger;

    public LiveAgentAnalyticsExampleController(
        ILiveAgentAnalyticsService analyticsService,
        ILogger<LiveAgentAnalyticsExampleController> logger)
    {
        _analyticsService = analyticsService;
        _logger = logger;
    }

    /// <summary>
    /// Example: Request live agent support
    /// </summary>
    [HttpPost("request-agent")]
    public async Task<IActionResult> RequestAgentWithAnalytics([FromBody] RequestAgentAnalyticsRequest request)
    {
        try
        {
            var sessionId = Guid.NewGuid().ToString();

            // Publish agent requested event
            await _analyticsService.PublishAgentRequestedAsync(
                request.ConversationId,
                request.UserId,
                request.TenantId,
                sessionId,
                request.RequestReason,
                request.PreviousChatbotConversationId,
                request.QueuePosition,
                request.Department,
                request.Priority
            );

            return Ok(new { Message = "Agent request tracked", SessionId = sessionId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error requesting agent with analytics");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Example: Assign agent to conversation
    /// </summary>
    [HttpPost("assign-agent")]
    public async Task<IActionResult> AssignAgentWithAnalytics([FromBody] AssignAgentAnalyticsRequest request)
    {
        try
        {
            // Publish agent assigned event
            await _analyticsService.PublishAgentAssignedAsync(
                request.ConversationId,
                request.AgentId,
                request.AgentName,
                request.UserId,
                request.TenantId,
                request.SessionId,
                request.WaitTime,
                request.QueuePosition,
                request.AssignmentMethod,
                request.Department
            );

            return Ok(new { Message = "Agent assignment tracked" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning agent with analytics");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Example: Track agent message with response time
    /// </summary>
    [HttpPost("agent-message")]
    public async Task<IActionResult> SendAgentMessageWithAnalytics([FromBody] AgentMessageAnalyticsRequest request)
    {
        try
        {
            var messageId = Guid.NewGuid().ToString();

            // Publish message sent event
            await _analyticsService.PublishMessageSentAsync(
                request.ConversationId,
                messageId,
                request.AgentId,
                request.UserId,
                request.TenantId,
                request.SessionId,
                request.MessageType,
                request.MessageLength,
                request.ResponseTime,
                request.IsFirstMessage,
                request.MessageIntent
            );

            // Also publish response time event if provided
            if (request.ResponseTime.HasValue)
            {
                await _analyticsService.PublishResponseTimeAsync(
                    request.ConversationId,
                    messageId,
                    request.AgentId,
                    request.UserId,
                    request.TenantId,
                    request.SessionId,
                    request.ResponseTime.Value,
                    request.IsFirstMessage,
                    null, // Let service calculate category
                    request.ResponseQuality
                );
            }

            return Ok(new { MessageId = messageId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending agent message with analytics");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Example: End conversation with detailed analytics
    /// </summary>
    [HttpPost("end-conversation")]
    public async Task<IActionResult> EndConversationWithAnalytics([FromBody] EndConversationAnalyticsRequest request)
    {
        try
        {
            // Publish conversation ended event
            await _analyticsService.PublishConversationEndedAsync(
                request.ConversationId,
                request.AgentId,
                request.UserId,
                request.TenantId,
                request.SessionId,
                request.EndedBy,
                request.ConversationDuration,
                request.MessageCount,
                request.AgentMessages,
                request.UserMessages,
                request.ResolutionStatus,
                request.ResolutionNotes,
                request.RequiresFollowUp
            );

            return Ok(new { Message = "Conversation end tracked" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ending conversation with analytics");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Example: Submit customer feedback
    /// </summary>
    [HttpPost("customer-feedback")]
    public async Task<IActionResult> SubmitCustomerFeedbackWithAnalytics([FromBody] CustomerFeedbackAnalyticsRequest request)
    {
        try
        {
            // Publish feedback event
            await _analyticsService.PublishFeedbackAsync(
                request.ConversationId,
                request.AgentId,
                request.UserId,
                request.TenantId,
                request.SessionId,
                request.Rating,
                request.Satisfaction,
                request.FeedbackText,
                request.FeedbackCategories,
                request.WouldRecommend,
                request.ImprovementSuggestions
            );

            return Ok(new { Message = "Customer feedback tracked" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting customer feedback with analytics");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Example: Track agent session duration
    /// </summary>
    [HttpPost("session-duration")]
    public async Task<IActionResult> TrackSessionDurationWithAnalytics([FromBody] SessionDurationAnalyticsRequest request)
    {
        try
        {
            // Publish session duration event
            await _analyticsService.PublishSessionDurationAsync(
                request.ConversationId,
                request.AgentId,
                request.UserId,
                request.TenantId,
                request.SessionId,
                request.StartTime,
                request.EndTime,
                request.Duration,
                request.ActivePeriods,
                request.IdleTime,
                request.Quality
            );

            return Ok(new { Message = "Session duration tracked" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error tracking session duration with analytics");
            return StatusCode(500, "Internal server error");
        }
    }
}

// Request DTOs
public record RequestAgentAnalyticsRequest
{
    public required Guid ConversationId { get; init; }
    public string? UserId { get; init; }
    public required string TenantId { get; init; }
    public string? RequestReason { get; init; }
    public string? PreviousChatbotConversationId { get; init; }
    public int? QueuePosition { get; init; }
    public string? Department { get; init; }
    public string? Priority { get; init; }
}

public record AssignAgentAnalyticsRequest
{
    public required Guid ConversationId { get; init; }
    public required string AgentId { get; init; }
    public required string AgentName { get; init; }
    public string? UserId { get; init; }
    public required string TenantId { get; init; }
    public required string SessionId { get; init; }
    public TimeSpan? WaitTime { get; init; }
    public int? QueuePosition { get; init; }
    public string? AssignmentMethod { get; init; }
    public string? Department { get; init; }
}

public record AgentMessageAnalyticsRequest
{
    public required Guid ConversationId { get; init; }
    public required string AgentId { get; init; }
    public string? UserId { get; init; }
    public required string TenantId { get; init; }
    public required string SessionId { get; init; }
    public MessageType MessageType { get; init; } = MessageType.Text;
    public int MessageLength { get; init; }
    public TimeSpan? ResponseTime { get; init; }
    public bool IsFirstMessage { get; init; }
    public string? MessageIntent { get; init; }
    public string? ResponseQuality { get; init; }
}

public record EndConversationAnalyticsRequest
{
    public required Guid ConversationId { get; init; }
    public required string AgentId { get; init; }
    public string? UserId { get; init; }
    public required string TenantId { get; init; }
    public required string SessionId { get; init; }
    public required ConversationEndedBy EndedBy { get; init; }
    public required TimeSpan ConversationDuration { get; init; }
    public required int MessageCount { get; init; }
    public required int AgentMessages { get; init; }
    public required int UserMessages { get; init; }
    public required ResolutionStatus ResolutionStatus { get; init; }
    public string? ResolutionNotes { get; init; }
    public bool RequiresFollowUp { get; init; }
}

public record CustomerFeedbackAnalyticsRequest
{
    public required Guid ConversationId { get; init; }
    public required string AgentId { get; init; }
    public string? UserId { get; init; }
    public required string TenantId { get; init; }
    public required string SessionId { get; init; }
    public required int Rating { get; init; }
    public required SatisfactionLevel Satisfaction { get; init; }
    public string? FeedbackText { get; init; }
    public List<string>? FeedbackCategories { get; init; }
    public bool WouldRecommend { get; init; }
    public string? ImprovementSuggestions { get; init; }
}

public record SessionDurationAnalyticsRequest
{
    public required Guid ConversationId { get; init; }
    public required string AgentId { get; init; }
    public string? UserId { get; init; }
    public required string TenantId { get; init; }
    public required string SessionId { get; init; }
    public required DateTime StartTime { get; init; }
    public required DateTime EndTime { get; init; }
    public required TimeSpan Duration { get; init; }
    public int ActivePeriods { get; init; } = 1;
    public TimeSpan? IdleTime { get; init; }
    public SessionQuality? Quality { get; init; }
}