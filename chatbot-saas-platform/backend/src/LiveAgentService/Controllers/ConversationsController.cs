using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LiveAgentService.Services;
using LiveAgentService.Models;
using Shared.Infrastructure.Services;
using Shared.Application.Common.Interfaces;

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
    private readonly ILogger<ConversationsController> _logger;

    public ConversationsController(
        IAgentManagementService agentManagementService,
        ICurrentUserService currentUserService,
        ITenantService tenantService,
        IAgentRoutingService agentRoutingService,
        IQueueManagementService queueManagementService,
        IChatRuntimeIntegrationService chatRuntimeIntegrationService,
        ILogger<ConversationsController> logger)
    {
        _agentManagementService = agentManagementService;
        _currentUserService = currentUserService;
        _tenantService = tenantService;
        _agentRoutingService = agentRoutingService;
        _queueManagementService = queueManagementService;
        _chatRuntimeIntegrationService = chatRuntimeIntegrationService;
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
                        UpdatedAt = conversationDetails.UpdatedAt
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
                        MessageCount = 0
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
                    sender = "agent",
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
            _logger.LogInformation("Rating conversation {ConversationId} with rating {Rating}", conversationId, request.Rating);

            return Ok(new { conversationId, rating = request.Rating, feedback = request.Feedback });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rating conversation {ConversationId}", conversationId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }
}
