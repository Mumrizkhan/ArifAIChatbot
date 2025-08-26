using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using Shared.Application.Common.Interfaces;
using Shared.Domain.Enums;
using LiveAgentService.Services;
using LiveAgentService.Models;
using LiveAgentService.Hubs;

namespace LiveAgentService.Services;

public class AgentRoutingService : IAgentRoutingService
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<AgentRoutingService> _logger;
    private readonly IHubContext<AgentHub> _hubContext;
    private readonly Dictionary<Guid, AgentStatus> _agentStatuses = new();
    private readonly Dictionary<Guid, DateTime> _lastActivity = new();

    public AgentRoutingService(
        IApplicationDbContext context, 
        ILogger<AgentRoutingService> logger,
        IHubContext<AgentHub> hubContext)
    {
        _context = context;
        _logger = logger;
        _hubContext = hubContext;
    }

    public async Task<Guid?> FindAvailableAgentAsync(Guid tenantId, string? department = null, string? language = null)
    {
        try
        {
            var agents = await _context.UserTenants
                .Include(ut => ut.User)
                .Where(ut => ut.TenantId == tenantId && 
                           (ut.Role == TenantRole.Agent || ut.Role == TenantRole.Admin) &&
                           ut.IsActive)
                .Select(ut => new { ut.UserId, ut.User.Email, ut.User.PreferredLanguage,ut.User.Status })
                .ToListAsync();

            var availableAgents = new List<Guid>();

            foreach (var agent in agents)
            {
                //var status = GetAgentStatusAsync(agent.UserId).Result;
                if (agent.Status == UserStatus.Online)
                {
                    var activeConversations = await GetActiveConversationCountAsync(agent.UserId);
                    if (activeConversations < 5) // Max conversations per agent
                    {
                        if (language != null && agent.PreferredLanguage != language)
                            continue;

                        availableAgents.Add(agent.UserId);
                    }
                }
            }

            if (!availableAgents.Any())
                return null;

            var selectedAgent = availableAgents.OrderBy(a => GetActiveConversationCountAsync(a).Result).First();
            return selectedAgent;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding available agent for tenant {TenantId}", tenantId);
            return null;
        }
    }

    public async Task<bool> AssignConversationToAgentAsync(Guid conversationId, Guid agentId)
    {
        try
        {
            var conversation = await _context.Conversations
                .FirstOrDefaultAsync(c => c.Id == conversationId);

            if (conversation == null)
                return false;

            conversation.AssignedAgentId = agentId;
            conversation.Status = ConversationStatus.Active;
            conversation.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning conversation {ConversationId} to agent {AgentId}", conversationId, agentId);
            return false;
        }
    }

    public async Task<bool> TransferConversationAsync(Guid conversationId, Guid fromAgentId, Guid toAgentId)
    {
        try
        {
            var conversation = await _context.Conversations
                .FirstOrDefaultAsync(c => c.Id == conversationId && c.AssignedAgentId == fromAgentId);

            if (conversation == null)
                return false;

            conversation.AssignedAgentId = toAgentId;
            conversation.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error transferring conversation {ConversationId} from {FromAgentId} to {ToAgentId}", 
                conversationId, fromAgentId, toAgentId);
            return false;
        }
    }

    public async Task<List<AgentWorkload>> GetAgentWorkloadsAsync(Guid tenantId)
    {
        try
        {
            var agents = await _context.UserTenants
                .Include(ut => ut.User)
                .Where(ut => ut.TenantId == tenantId && 
                           (ut.Role == TenantRole.Agent || ut.Role == TenantRole.Admin) &&
                           ut.IsActive)
                .ToListAsync();

            var workloads = new List<AgentWorkload>();

            foreach (var agent in agents)
            {
                var activeConversations = await GetActiveConversationCountAsync(agent.UserId);
                var status = await GetAgentStatusAsync(agent.UserId);

                workloads.Add(new AgentWorkload
                {
                    AgentId = agent.UserId,
                    AgentName = $"{agent.User.FirstName} {agent.User.LastName}",
                    AgentEmail = agent.User.Email,
                    Status = status,
                    ActiveConversations = activeConversations,
                    MaxConversations = 5,
                    Languages = new List<string> { agent.User.PreferredLanguage },
                    LastActivity = _lastActivity.GetValueOrDefault(agent.UserId, DateTime.UtcNow)
                });
            }

            return workloads;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting agent workloads for tenant {TenantId}", tenantId);
            return new List<AgentWorkload>();
        }
    }

    public async Task<bool> SetAgentStatusAsync(Guid agentId, AgentStatus status)
    {
        try
        {
            _agentStatuses[agentId] = status;
            _lastActivity[agentId] = DateTime.UtcNow;
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting agent status for {AgentId}", agentId);
            return false;
        }
    }

    public async Task<AgentStatus> GetAgentStatusAsync(Guid agentId)
    {
        return _agentStatuses.GetValueOrDefault(agentId, AgentStatus.Offline);
    }

    public async Task<List<ConversationAssignment>> GetAgentConversationsAsync(Guid agentId)
    {
        try
        {
            var conversations = await _context.Conversations
                .Where(c => c.AssignedAgentId == agentId && 
                           c.Status != ConversationStatus.Closed &&
                           c.Status != ConversationStatus.Resolved)
                .OrderByDescending(c => c.UpdatedAt ?? c.CreatedAt)
                .Select(c => new ConversationAssignment
                {
                    ConversationId = c.Id,
                    AgentId = agentId,
                    CustomerName = c.CustomerName,
                    Subject = c.Subject,
                    Status = c.Status,
                    AssignedAt = c.CreatedAt, // Simplified - should track actual assignment time
                    LastMessageAt = c.UpdatedAt ?? c.CreatedAt,
                    UnreadMessages = 0, // Simplified - would need to track read status
                    Priority = QueuePriority.Normal // Simplified - would need priority field
                })
                .ToListAsync();

            return conversations;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting conversations for agent {AgentId}", agentId);
            return new List<ConversationAssignment>();
        }
    }

    private async Task<int> GetActiveConversationCountAsync(Guid agentId)
    {
        return await _context.Conversations
            .CountAsync(c => c.AssignedAgentId == agentId && 
                           c.Status != ConversationStatus.Closed &&
                           c.Status != ConversationStatus.Resolved);
    }

    public async Task NotifyAgentsOfEscalationAsync(Guid tenantId, object escalationData)
    {
        try
        {
            _logger.LogInformation("Sending escalation notification to agents in tenant {TenantId}: {Data}", 
                tenantId, escalationData);
            
            // Send ConversationAssigned event to all agents in the tenant group
            await _hubContext.Clients.Group($"agents_{tenantId}")
                .SendAsync("ConversationAssigned", escalationData);
            
            _logger.LogInformation("Escalation notification sent successfully to agents in tenant {TenantId}", tenantId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error notifying agents of escalation for tenant {TenantId}", tenantId);
        }
    }
}
