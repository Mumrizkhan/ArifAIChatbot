using LiveAgentService.Models;
using LiveAgentService.Services;
using LiveAgentService.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Shared.Application.Common.Interfaces;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Services;

namespace LiveAgentService.Controllers;

[ApiController]
[Route("api/[controller]")]
//[Authorize]
public class AgentsController : ControllerBase
{
    private readonly IAgentManagementService _agentManagementService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ITenantService _tenantService;
    private readonly IAgentRoutingService _agentRoutingService;
    private readonly IQueueManagementService _queueManagementService;
    private readonly IHubContext<AgentHub> _hubContext;
    private readonly IChatRuntimeIntegrationService _chatRuntimeIntegrationService;
    private readonly ILogger<AgentsController> _logger;

    public AgentsController(
        IAgentManagementService agentManagementService,
        ICurrentUserService currentUserService,
        ITenantService tenantService,
        IAgentRoutingService agentRoutingService,
        IQueueManagementService queueManagementService,
        IHubContext<AgentHub> hubContext,
        IChatRuntimeIntegrationService chatRuntimeIntegrationService,
        ILogger<AgentsController> logger)
    {
        _agentManagementService = agentManagementService;
        _currentUserService = currentUserService;
        _tenantService = tenantService;
        _agentRoutingService = agentRoutingService;
        _queueManagementService = queueManagementService;
        _hubContext = hubContext;
        _chatRuntimeIntegrationService = chatRuntimeIntegrationService;
        _logger = logger;
    }

    [HttpGet("workloads")]
    public async Task<IActionResult> GetAgentWorkloads()
    {
        try
        {
            var tenantId = _tenantService.GetCurrentTenantId();
            var workloads = await _agentRoutingService.GetAgentWorkloadsAsync(tenantId);
            
            return Ok(workloads);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting agent workloads");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("my-conversations")]
    public async Task<IActionResult> GetMyConversations()
    {
        try
        {
            var agentId = _currentUserService.UserId;
            if (!agentId.HasValue)
            {
                return Unauthorized();
            }

            var conversations = await _agentRoutingService.GetAgentConversationsAsync(agentId.Value);
            return Ok(conversations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting agent conversations");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }
 

    [HttpGet("available")]
    public async Task<IActionResult> GetAvailableAgent([FromQuery] Guid tenantId, [FromQuery] string? language = null)
    {
        try
        {
            var agentId = await _agentRoutingService.FindAvailableAgentAsync(tenantId, null, language);

            if (agentId == null)
            {
                return NotFound(new { Message = "No available agent found." });
            }

            return Ok(new { AgentId = agentId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding available agent for tenant {TenantId}", tenantId);
            return StatusCode(500, new { Message = "An error occurred while finding an available agent." });
        }
    }

    [HttpPost("status")]
    public async Task<IActionResult> UpdateStatus([FromBody] UpdateStatusRequest request)
    {
        try
        {
            var agentId = _currentUserService.UserId;
            if (!agentId.HasValue)
            {
                return Unauthorized();
            }

            if (Enum.TryParse<AgentStatus>(request.Status, out var status))
            {
                var updated = await _agentRoutingService.SetAgentStatusAsync(agentId.Value, status);
                if (updated)
                {
                    return Ok(new { message = "Status updated successfully" });
                }
            }

            return BadRequest(new { message = "Invalid status" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating agent status");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPut("{agentId}/status")]
    public async Task<IActionResult> UpdateAgentStatus(Guid agentId, [FromBody] UpdateStatusRequest request)
    {
        try
        {
            if (Enum.TryParse<AgentStatus>(request.Status.FirstCharToUpper(), out var status))
            {
                var updated = await _agentRoutingService.SetAgentStatusAsync(agentId, status);
                if (updated)
                {
                    return Ok(new { message = "Status updated successfully" });
                }
            }

            return BadRequest(new { message = "Invalid status" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating agent status");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("status")]
    public async Task<IActionResult> GetStatus()
    {
        try
        {
            var agentId = _currentUserService.UserId;
            if (!agentId.HasValue)
            {
                return Unauthorized();
            }

            var status = await _agentRoutingService.GetAgentStatusAsync(agentId.Value);
            return Ok(new { Status = status.ToString() });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting agent status");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost("notify-escalation")]
    public async Task<IActionResult> NotifyEscalation([FromBody] EscalationNotificationRequest request)
    {
        try
        {
            var tenantId = _tenantService.GetCurrentTenantId();

            // Send ConversationAssigned event to all agents in the tenant
            await _agentRoutingService.NotifyAgentsOfEscalationAsync(tenantId, new
            {
                ConversationId = request.ConversationId,
                CustomerName = request.CustomerName,
                CustomerEmail = request.CustomerEmail,
                Subject = request.Subject,
                Language = request.Language,
                Status = "pending",
                Timestamp = DateTime.UtcNow.ToString("O")
            });

            return Ok(new { message = "Escalation notification sent to agents" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending escalation notification for conversation {ConversationId}", request.ConversationId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost("notify-message")]
    public async Task<IActionResult> NotifyMessage([FromBody] MessageNotificationRequest request)
    {
        try
        {
            var tenantId = _tenantService.GetCurrentTenantId();

            // Send MessageReceived event to all agents in the conversation
            await _hubContext.Clients.Group($"conversation_{request.ConversationId}")
                .SendAsync("MessageReceived", new
                {
                    Id = request.MessageId,
                    ConversationId = request.ConversationId,
                    Content = request.Content,
                    Type = request.Type ?? "text",
                    Sender = request.Sender,
                    SenderId = request.SenderId,
                    SenderName = request.SenderName,
                    CreatedAt = request.CreatedAt,
                    Timestamp = DateTime.UtcNow.ToString("O")
                });

            return Ok(new { message = "Message notification sent to agents" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending message notification for conversation {ConversationId}", request.ConversationId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost("assign-conversation")]
    public async Task<IActionResult> AssignConversation([FromBody] AssignConversationRequest request)
    {
        try
        {
            var assigned = await _agentRoutingService.AssignConversationToAgentAsync(
                request.ConversationId, request.AgentId);

            if (assigned)
            {
                await _queueManagementService.RemoveFromQueueAsync(request.ConversationId);
                return Ok(new { message = "Conversation assigned successfully" });
            }

            return BadRequest(new { message = "Failed to assign conversation" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning conversation");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost("transfer-conversation")]
    public async Task<IActionResult> TransferConversation([FromBody] TransferConversationRequest request)
    {
        try
        {
            var agentId = _currentUserService.UserId;
            if (!agentId.HasValue)
            {
                return Unauthorized();
            }

            var transferred = await _agentRoutingService.TransferConversationAsync(
                request.ConversationId, agentId.Value, request.ToAgentId);

            if (transferred)
            {
                return Ok(new { message = "Conversation transferred successfully" });
            }

            return BadRequest(new { message = "Failed to transfer conversation" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error transferring conversation");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("queue")]
    public async Task<IActionResult> GetQueue()
    {
        try
        {
            var tenantId = _tenantService.GetCurrentTenantId();
            var queuedConversations = await _queueManagementService.GetQueuedConversationsAsync(tenantId);
            
            return Ok(queuedConversations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting conversation queue");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("queue/statistics")]
    public async Task<IActionResult> GetQueueStatistics()
    {
        try
        {
            var tenantId = _tenantService.GetCurrentTenantId();
            var statistics = await _queueManagementService.GetQueueStatisticsAsync(tenantId);
            
            return Ok(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting queue statistics");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost("queue/next")]
    public async Task<IActionResult> GetNextInQueue([FromBody] NextInQueueRequest? request = null)
    {
        try
        {
            var tenantId = _tenantService.GetCurrentTenantId();
            var nextConversation = await _queueManagementService.GetNextInQueueAsync(
                tenantId, request?.Department);
            
            if (nextConversation != null)
            {
                return Ok(nextConversation);
            }

            return NotFound(new { message = "No conversations in queue" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting next conversation in queue");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost("escalate")]
    public async Task<IActionResult> EscalateConversation([FromBody] EscalateConversationRequest request)
    {
        try
        {
            var escalated = await _queueManagementService.EscalateConversationAsync(
                request.ConversationId, request.Reason);

            if (escalated)
            {
                return Ok(new { message = "Conversation escalated successfully" });
            }

            return BadRequest(new { message = "Failed to escalate conversation" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error escalating conversation");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("performance")]
    public async Task<IActionResult> GetPerformanceMetrics([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
    {
        try
        {
            var agentId = _currentUserService.UserId;
            if (!agentId.HasValue)
            {
                return Unauthorized();
            }

            var start = startDate ?? DateTime.UtcNow.AddDays(-30);
            var end = endDate ?? DateTime.UtcNow;

            var metrics = await _agentManagementService.GetPerformanceMetricsAsync(agentId.Value, start, end);

            return Ok(metrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting performance metrics");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetAllAgents()
    {
        try
        {
            var tenantId = _tenantService.GetCurrentTenantId();
            var agents = await _agentManagementService.GetAllAgentsAsync(tenantId);
            return Ok(agents);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all agents");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetAgentProfile(Guid id)
    {
        try
        {
            var tenantId = _tenantService.GetCurrentTenantId();
            var agent = await _agentManagementService.GetAgentProfileAsync(id, tenantId);

            if (agent==null)
            {
                return NotFound(new { message = "Agent not found" });
            }

            return Ok(agent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting agent profile for {AgentId}", id);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateAgentProfile(Guid id, [FromBody] UpdateAgentProfileRequest request)
    {
        try
        {
            var tenantId = _tenantService.GetCurrentTenantId();
            var updated = await _agentManagementService.UpdateAgentProfileAsync(id, request, tenantId);

            if (!updated)
            {
                return NotFound(new { message = "Agent not found" });
            }

            return Ok(new { message = "Agent profile updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating agent profile for {AgentId}", id);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("{agentId}/stats")]
    public async Task<IActionResult> GetAgentStats(Guid agentId, [FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
    {
        try
        {
            var tenantId = _tenantService.GetCurrentTenantId();
            var stats = await _agentManagementService.GetAgentStatsAsync(agentId, tenantId, startDate, endDate);
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting agent stats for {AgentId}", agentId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPut("messages/{messageId}/mark-read")]
    public async Task<IActionResult> MarkMessageAsRead(string messageId, [FromBody] MarkMessageAsReadRequest request)
    {
        try
        {
            var tenantId = _tenantService.GetCurrentTenantId();
            var agentId = _currentUserService.UserId;

            _logger.LogInformation("Agent {AgentId} marking message as read: {MessageId}", agentId, messageId);

            if (!Guid.TryParse(messageId, out var msgId))
            {
                return BadRequest(new { message = "Invalid message ID format" });
            }

            // Call ChatRuntimeService to mark message as read
            var result = await _chatRuntimeIntegrationService.MarkMessageAsReadAsync(msgId, agentId?.ToString() ?? "agent", "agent");

            if (result)
            {
                _logger.LogInformation("Message {MessageId} marked as read by agent {AgentId}", messageId, agentId);
                return Ok(new { success = true, message = "Message marked as read" });
            }
            else
            {
                return BadRequest(new { message = "Failed to mark message as read" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking message as read: {MessageId}", messageId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }
}

public class EscalationNotificationRequest
{
    public Guid ConversationId { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerEmail { get; set; }
    public string? Subject { get; set; }
    public string? Language { get; set; }
}

public class MarkMessageAsReadRequest
{
    public string ReaderId { get; set; } = string.Empty; // User/Agent ID who read the message
    public string ReaderType { get; set; } = string.Empty; // "customer" or "agent"
}


