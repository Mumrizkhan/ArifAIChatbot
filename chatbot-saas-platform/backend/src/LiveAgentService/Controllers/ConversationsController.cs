using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LiveAgentService.Services;
using LiveAgentService.Models;
using Shared.Infrastructure.Services;
using Shared.Application.Common.Interfaces;
using Shared.Domain.Entities;

namespace LiveAgentService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ConversationsController : ControllerBase
{
    private readonly IAgentManagementService _agentManagementService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ITenantService _tenantService;
    private readonly IAgentRoutingService _agentRoutingService;
    private readonly IQueueManagementService _queueManagementService;
    private readonly IChatRuntimeIntegrationService _chatRuntimeIntegrationService;
    private readonly IConversationRatingService _conversationRatingService;
    private readonly ILogger<ConversationsController> _logger;

    public ConversationsController(
        IAgentManagementService agentManagementService,
        ICurrentUserService currentUserService,
        ITenantService tenantService,
        IAgentRoutingService agentRoutingService,
        IQueueManagementService queueManagementService,
        IChatRuntimeIntegrationService chatRuntimeIntegrationService,
        IConversationRatingService conversationRatingService,
        ILogger<ConversationsController> logger)
    {
        _agentManagementService = agentManagementService;
        _currentUserService = currentUserService;
        _tenantService = tenantService;
        _agentRoutingService = agentRoutingService;
        _queueManagementService = queueManagementService;
        _chatRuntimeIntegrationService = chatRuntimeIntegrationService;
        _conversationRatingService = conversationRatingService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetConversations([FromQuery] string? status, [FromQuery] string? priority, [FromQuery] int limit = 50)
    {
        try
        {
            var agentId = _currentUserService.UserId;
            if (!agentId.HasValue)
            {
                return Unauthorized();
            }

            var conversationAssignments = await _agentRoutingService.GetAgentConversationsAsync(agentId.Value);
            
            if (!string.IsNullOrEmpty(status))
            {
                conversationAssignments = conversationAssignments.Where(c => c.Status.ToString().Equals(status, StringComparison.OrdinalIgnoreCase)).ToList();
            }
            
            if (!string.IsNullOrEmpty(priority))
            {
                conversationAssignments = conversationAssignments.Where(c => c.Priority.ToString().Equals(priority, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            conversationAssignments = conversationAssignments.Take(limit).ToList();

            // Enrich with actual conversation data from ChatRuntimeService
            var enrichedConversations = new List<object>();
            
            foreach (var assignment in conversationAssignments)
            {
                var conversationDetails = await _chatRuntimeIntegrationService.GetConversationDetailsAsync(assignment.ConversationId);
                
                if (conversationDetails != null)
                {
                    // Get the latest messages to show preview and get real last message time
                    var messages = await _chatRuntimeIntegrationService.GetConversationMessagesAsync(assignment.ConversationId);
                    var lastMessage = messages.OrderByDescending(m => m.CreatedAt).FirstOrDefault();
                    var latestMessagePreview = lastMessage?.Content?.Length > 50 
                        ? lastMessage.Content.Substring(0, 50) + "..." 
                        : lastMessage?.Content ?? "";

                    // Get rating information
                    var rating = await _conversationRatingService.GetConversationRatingAsync(assignment.ConversationId);

                    enrichedConversations.Add(new
                    {
                        ConversationId = assignment.ConversationId,
                        AgentId = assignment.AgentId,
                        CustomerName = conversationDetails.CustomerName ?? "Unknown Customer",
                        CustomerEmail = conversationDetails.CustomerEmail,
                        Subject = conversationDetails.Subject ?? "No Subject",
                        Status = conversationDetails.Status,
                        Language = conversationDetails.Language,
                        AssignedAt = assignment.AssignedAt,
                        LastMessageAt = lastMessage?.CreatedAt ?? conversationDetails.UpdatedAt ?? conversationDetails.CreatedAt,
                        LastMessagePreview = latestMessagePreview,
                        LastMessageSender = lastMessage?.Sender ?? "System",
                        UnreadMessages = messages.Count(m => !m.IsRead && m.Sender != "Agent"), // Count unread non-agent messages
                        MessageCount = conversationDetails.MessageCount,
                        Priority = assignment.Priority,
                        CreatedAt = conversationDetails.CreatedAt,
                        UpdatedAt = conversationDetails.UpdatedAt,
                        Rating = rating?.Rating,
                        Feedback = rating?.Feedback,
                        HasRating = rating != null
                    });
                }
                else
                {
                    // Fallback to assignment data if conversation details unavailable
                    enrichedConversations.Add(new
                    {
                        ConversationId = assignment.ConversationId,
                        AgentId = assignment.AgentId,
                        CustomerName = assignment.CustomerName ?? "Unknown Customer",
                        Subject = assignment.Subject ?? "No Subject", 
                        Status = assignment.Status.ToString(),
                        AssignedAt = assignment.AssignedAt,
                        LastMessageAt = assignment.LastMessageAt,
                        LastMessagePreview = "Unable to load messages",
                        UnreadMessages = assignment.UnreadMessages,
                        Priority = assignment.Priority,
                        MessageCount = 0,
                        HasRating = false
                    });
                }
            }

            return Ok(enrichedConversations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting conversations");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("{conversationId}")]
    public async Task<IActionResult> GetConversation(Guid conversationId)
    {
        try
        {
            var agentId = _currentUserService.UserId;
            if (!agentId.HasValue)
            {
                return Unauthorized();
            }

            var conversations = await _agentRoutingService.GetAgentConversationsAsync(agentId.Value);
            var assignment = conversations.FirstOrDefault(c => c.ConversationId == conversationId);

            if (assignment == null)
            {
                return NotFound(new { message = "Conversation not found" });
            }

            // Get enriched conversation data from ChatRuntimeService
            var conversationDetails = await _chatRuntimeIntegrationService.GetConversationDetailsAsync(conversationId);
            var messages = await _chatRuntimeIntegrationService.GetConversationMessagesAsync(conversationId);

            if (conversationDetails == null)
            {
                return NotFound(new { message = "Conversation details not found" });
            }

            // Get rating information
            var rating = await _conversationRatingService.GetConversationRatingAsync(conversationId);

            return Ok(new
            {
                ConversationId = conversationId,
                AgentId = assignment.AgentId,
                CustomerName = conversationDetails.CustomerName,
                CustomerEmail = conversationDetails.CustomerEmail,
                CustomerPhone = conversationDetails.CustomerPhone,
                Subject = conversationDetails.Subject,
                Status = conversationDetails.Status,
                Language = conversationDetails.Language,
                Channel = conversationDetails.Channel,
                TenantId = conversationDetails.TenantId,
                AssignedAt = assignment.AssignedAt,
                CreatedAt = conversationDetails.CreatedAt,
                UpdatedAt = conversationDetails.UpdatedAt,
                MessageCount = conversationDetails.MessageCount,
                Priority = assignment.Priority,
                Rating = rating?.Rating,
                Feedback = rating?.Feedback,
                RatedAt = rating?.RatedAt,
                HasRating = rating != null,
                Messages = messages.Select(m => new 
                {
                    Id = m.Id,
                    ConversationId = m.ConversationId,
                    Content = m.Content,
                    Type = m.Type,
                    Sender = m.Sender,
                    SenderId = m.SenderId,
                    SenderName = m.SenderName,
                    CreatedAt = m.CreatedAt,
                    IsRead = m.IsRead
                }).OrderBy(m => m.CreatedAt).ToList()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting conversation {ConversationId}", conversationId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPut("{conversationId}/status")]
    public async Task<IActionResult> UpdateConversationStatus(Guid conversationId, [FromBody] UpdateConversationStatusRequest request)
    {
        try
        {
            var agentId = _currentUserService.UserId;
            if (!agentId.HasValue)
            {
                return Unauthorized();
            }

            _logger.LogInformation("Updating conversation {ConversationId} status to {Status}", conversationId, request.Status);

            // Update the conversation status in the database
            var updated = await _agentRoutingService.UpdateConversationStatusAsync(conversationId, request.Status, agentId.Value);

            if (!updated)
            {
                return NotFound(new { message = "Conversation not found or status update failed" });
            }

            // If the status is resolved, remove the conversation from the agent queue
            if (request.Status.Equals("Resolved", StringComparison.OrdinalIgnoreCase))
            {
                await _queueManagementService.RemoveFromQueueAsync(conversationId);
                _logger.LogInformation("Conversation {ConversationId} removed from the agent queue as it is resolved", conversationId);
            }

            return Ok(new { conversationId, status = request.Status });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating conversation status for {ConversationId}", conversationId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost("{conversationId}/messages")]
    public async Task<IActionResult> SendMessage(Guid conversationId, [FromBody] SendMessageRequest request)
    {
        try
        {
            var agentId = _currentUserService.UserId;
            if (!agentId.HasValue)
            {
                return Unauthorized();
            }

            var success = await _chatRuntimeIntegrationService.SendMessageAsync(
                conversationId, 
                request.Content, 
                request.Type ?? "text", 
                agentId.Value);

            if (success)
            {
                var response = new
                {
                    id = Guid.NewGuid(),
                    conversationId,
                    content = request.Content,
                    sender = "Agent",
                    senderId = agentId.Value,
                    timestamp = DateTime.UtcNow,
                    type = request.Type ?? "text",
                    metadata = request.Metadata,
                    success = true
                };

                _logger.LogInformation("Agent {AgentId} sent message to conversation {ConversationId}", agentId, conversationId);
                return Ok(response);
            }

            return StatusCode(500, new { message = "Failed to send message" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending message to conversation {ConversationId}", conversationId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("{conversationId}/messages")]
    public async Task<IActionResult> GetMessages(Guid conversationId)
    {
        try
        {
            var agentId = _currentUserService.UserId;
            if (!agentId.HasValue)
            {
                return Unauthorized();
            }

            // Verify agent has access to this conversation
            var conversations = await _agentRoutingService.GetAgentConversationsAsync(agentId.Value);
            var hasAccess = conversations.Any(c => c.ConversationId == conversationId);

            if (!hasAccess)
            {
                return Forbid();
            }

            // Get messages from ChatRuntimeService
            var messages = await _chatRuntimeIntegrationService.GetConversationMessagesAsync(conversationId);

            var formattedMessages = messages.Select(m => new 
            {
                Id = m.Id,
                ConversationId = m.ConversationId,
                Content = m.Content,
                Type = m.Type,
                Sender = m.Sender,
                SenderId = m.SenderId,
                SenderName = m.SenderName,
                CreatedAt = m.CreatedAt,
                IsRead = m.IsRead,
               // AttachmentUrl = m.AttachmentUrl,
               // Metadata = m.Metadata
            }).OrderBy(m => m.CreatedAt).ToList();

            return Ok(formattedMessages);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting messages for conversation {ConversationId}", conversationId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPut("{conversationId}/assign")]
    public async Task<IActionResult> AssignConversation(Guid conversationId, [FromBody] AssignConversationRequest request)
    {
        try
        {
            var assigned = await _agentRoutingService.AssignConversationToAgentAsync(conversationId, request.AgentId);

            if (assigned)
            {
                await _queueManagementService.RemoveFromQueueAsync(conversationId);
                
                var result = new
                {
                    id = conversationId,
                    assignedAgent = new
                    {
                        id = request.AgentId,
                        name = "Agent"
                    }
                };

                return Ok(result);
            }

            return BadRequest(new { message = "Failed to assign conversation" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning conversation {ConversationId}", conversationId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPut("{conversationId}/transfer")]
    public async Task<IActionResult> TransferConversation(Guid conversationId, [FromBody] TransferConversationRequest request)
    {
        try
        {
            var agentId = _currentUserService.UserId;
            if (!agentId.HasValue)
            {
                return Unauthorized();
            }

            var transferred = await _agentRoutingService.TransferConversationAsync(conversationId, agentId.Value, request.ToAgentId);

            if (transferred)
            {
                var result = new
                {
                    id = conversationId,
                    assignedAgent = new
                    {
                        id = request.ToAgentId,
                        name = "Agent"
                    }
                };

                return Ok(result);
            }

            return BadRequest(new { message = "Failed to transfer conversation" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error transferring conversation {ConversationId}", conversationId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPut("{conversationId}/rate")]
    public async Task<IActionResult> RateConversation(Guid conversationId, [FromBody] RateConversationRequest request)
    {
        try
        {
            var agentId = _currentUserService.UserId;
            if (!agentId.HasValue)
            {
                return Unauthorized(new { message = "Agent authentication required" });
            }

            // Validate the rating range
            if (request.Rating < 1 || request.Rating > 5)
            {
                return BadRequest(new { message = "Rating must be between 1 and 5" });
            }

            _logger.LogInformation("Agent {AgentId} is rating conversation {ConversationId} with rating {Rating}", 
                agentId.Value, conversationId, request.Rating);

            // Use the rating service to handle the rating
            var success = await _conversationRatingService.RateConversationAsync(conversationId, request, agentId.Value);

            if (success)
            {
                // Get updated rating information
                var ratingResult = await _conversationRatingService.GetConversationRatingAsync(conversationId);

                return Ok(new { 
                    conversationId, 
                    rating = request.Rating, 
                    feedback = request.Feedback,
                    ratedBy = agentId.Value,
                    ratedAt = DateTime.UtcNow,
                    message = "Conversation rated successfully"
                });
            }
            else
            {
                return BadRequest(new { message = "Failed to rate conversation. Please check if you have permission to rate this conversation." });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rating conversation {ConversationId}", conversationId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("{conversationId}/rating")]
    public async Task<IActionResult> GetConversationRating(Guid conversationId)
    {
        try
        {
            var agentId = _currentUserService.UserId;
            if (!agentId.HasValue)
            {
                return Unauthorized();
            }

            var rating = await _conversationRatingService.GetConversationRatingAsync(conversationId);

            if (rating == null)
            {
                return NotFound(new { message = "No rating found for this conversation" });
            }

            return Ok(rating);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting rating for conversation {ConversationId}", conversationId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("ratings/statistics")]
    public async Task<IActionResult> GetRatingStatistics([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
    {
        try
        {
            var tenantId = _tenantService.GetCurrentTenantId();
            var statistics = await _conversationRatingService.GetRatingStatisticsAsync(tenantId, startDate, endDate);

            return Ok(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting rating statistics");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("agent/{agentId}/ratings")]
    public async Task<IActionResult> GetAgentRatings(Guid agentId, [FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
    {
        try
        {
            var currentAgentId = _currentUserService.UserId;
            if (!currentAgentId.HasValue)
            {
                return Unauthorized();
            }

            // Agents can only view their own ratings unless they have admin privileges
            if (currentAgentId.Value != agentId)
            {
                // TODO: Add admin role check here
                return Forbid("You can only view your own ratings");
            }

            var ratings = await _conversationRatingService.GetAgentRatingsAsync(agentId, startDate, endDate);

            return Ok(new
            {
                agentId,
                ratings,
                totalRatings = ratings.Count,
                averageRating = ratings.Any() ? Math.Round(ratings.Average(r => r.Rating), 2) : 0,
                periodStart = startDate,
                periodEnd = endDate
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting ratings for agent {AgentId}", agentId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost("{id}/feedback-message")]
    public async Task<IActionResult> SendFeedbackMessage(Guid id, [FromBody] SendFeedbackMessageRequest request)
    {
        try
        {
            var agentId = _currentUserService.UserId;
            if (!agentId.HasValue)
            {
                return Unauthorized();
            }

            // Call ChatRuntimeService to send the feedback message
            var result = await _chatRuntimeIntegrationService.SendFeedbackMessageAsync(id, request);
            
            if (result.IsSuccess)
            {
                return Ok(result.Data);
            }
            else
            {
                _logger.LogError("Failed to send feedback message: {Error}", result.ErrorMessage);
                return BadRequest(new { message = result.ErrorMessage });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending feedback message for conversation {ConversationId}", id);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }
}
