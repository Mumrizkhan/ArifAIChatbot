using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared.Application.Common.Interfaces;
using Shared.Domain.Enums;
using LiveAgentService.Services;
using LiveAgentService.Models;

namespace LiveAgentService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AgentsController : ControllerBase
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ITenantService _tenantService;
    private readonly IAgentRoutingService _agentRoutingService;
    private readonly IQueueManagementService _queueManagementService;
    private readonly ILogger<AgentsController> _logger;

    public AgentsController(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        ITenantService tenantService,
        IAgentRoutingService agentRoutingService,
        IQueueManagementService queueManagementService,
        ILogger<AgentsController> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _tenantService = tenantService;
        _agentRoutingService = agentRoutingService;
        _queueManagementService = queueManagementService;
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

            var conversations = await _context.Conversations
                .Where(c => c.AssignedAgentId == agentId.Value &&
                           c.CreatedAt >= start && c.CreatedAt <= end)
                .ToListAsync();

            var metrics = new AgentPerformanceMetrics
            {
                AgentId = agentId.Value,
                AgentName = _currentUserService.Email ?? "Agent",
                ConversationsHandled = conversations.Count,
                AverageResponseTime = TimeSpan.FromMinutes(2), // Simplified
                AverageResolutionTime = TimeSpan.FromMinutes(15), // Simplified
                CustomerSatisfactionRating = conversations.Where(c => c.CustomerSatisfactionRating.HasValue)
                    .Average(c => c.CustomerSatisfactionRating) ?? 0,
                PeriodStart = start,
                PeriodEnd = end
            };

            return Ok(metrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting performance metrics");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("{agentId}/stats")]
    public async Task<IActionResult> GetAgentStats(Guid agentId, [FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
    {
        try
        {
            var tenantId = _tenantService.GetCurrentTenantId();
            var start = startDate ?? DateTime.UtcNow.AddDays(-30);
            var end = endDate ?? DateTime.UtcNow;

            var conversations = await _context.Conversations
                .Where(c => c.AssignedAgentId == agentId && 
                           c.TenantId == tenantId &&
                           c.CreatedAt >= start && c.CreatedAt <= end)
                .Include(c => c.Messages)
                .ToListAsync();

            var totalConversations = conversations.Count;
            var resolvedConversations = conversations.Count(c => c.Status == ConversationStatus.Resolved);
            var averageResponseTime = conversations
                .Where(c => c.Messages.Any())
                .Select(c => c.Messages.Where(m => m.Sender == MessageSender.Agent).FirstOrDefault()?.CreatedAt - c.CreatedAt)
                .Where(t => t.HasValue)
                .DefaultIfEmpty(TimeSpan.Zero)
                .Average(t => t?.TotalMinutes ?? 0);

            var customerSatisfactionRating = conversations
                .Where(c => c.CustomerSatisfactionRating.HasValue)
                .Average(c => c.CustomerSatisfactionRating) ?? 0;

            return Ok(new
            {
                AgentId = agentId,
                PeriodStart = start,
                PeriodEnd = end,
                TotalConversations = totalConversations,
                ResolvedConversations = resolvedConversations,
                ResolutionRate = totalConversations > 0 ? (double)resolvedConversations / totalConversations : 0,
                AverageResponseTimeMinutes = averageResponseTime,
                CustomerSatisfactionRating = customerSatisfactionRating,
                ActiveConversations = conversations.Count(c => c.Status == ConversationStatus.Active)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting agent stats for {AgentId}", agentId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }
}

public class UpdateStatusRequest
{
    public string Status { get; set; } = string.Empty;
}

public class AssignConversationRequest
{
    public Guid ConversationId { get; set; }
    public Guid AgentId { get; set; }
}

public class TransferConversationRequest
{
    public Guid ConversationId { get; set; }
    public Guid ToAgentId { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public class NextInQueueRequest
{
    public string? Department { get; set; }
}

public class EscalateConversationRequest
{
    public Guid ConversationId { get; set; }
    public string Reason { get; set; } = string.Empty;
}
