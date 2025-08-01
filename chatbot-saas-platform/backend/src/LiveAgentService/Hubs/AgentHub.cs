using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Shared.Application.Common.Interfaces;
using LiveAgentService.Services;
using LiveAgentService.Models;

namespace LiveAgentService.Hubs;

[Authorize]
public class AgentHub : Hub
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ITenantService _tenantService;
    private readonly IAgentRoutingService _agentRoutingService;
    private readonly IQueueManagementService _queueManagementService;
    private readonly ILogger<AgentHub> _logger;

    public AgentHub(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        ITenantService tenantService,
        IAgentRoutingService agentRoutingService,
        IQueueManagementService queueManagementService,
        ILogger<AgentHub> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _tenantService = tenantService;
        _agentRoutingService = agentRoutingService;
        _queueManagementService = queueManagementService;
        _logger = logger;
    }

    public async Task JoinAgentGroup()
    {
        try
        {
            var tenantId = _tenantService.GetCurrentTenantId();
            var agentId = _currentUserService.UserId;

            if (agentId.HasValue)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"agents_{tenantId}");
                await _agentRoutingService.SetAgentStatusAsync(agentId.Value, AgentStatus.Online);
                
                await Clients.Group($"agents_{tenantId}").SendAsync("AgentStatusChanged", new
                {
                    AgentId = agentId.Value,
                    Status = AgentStatus.Online.ToString(),
                    Timestamp = DateTime.UtcNow
                });

                _logger.LogInformation($"Agent {agentId} joined tenant {tenantId} group");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error joining agent group");
        }
    }

    public async Task LeaveAgentGroup()
    {
        try
        {
            var tenantId = _tenantService.GetCurrentTenantId();
            var agentId = _currentUserService.UserId;

            if (agentId.HasValue)
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"agents_{tenantId}");
                await _agentRoutingService.SetAgentStatusAsync(agentId.Value, AgentStatus.Offline);
                
                await Clients.Group($"agents_{tenantId}").SendAsync("AgentStatusChanged", new
                {
                    AgentId = agentId.Value,
                    Status = AgentStatus.Offline.ToString(),
                    Timestamp = DateTime.UtcNow
                });

                _logger.LogInformation($"Agent {agentId} left tenant {tenantId} group");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error leaving agent group");
        }
    }

    public async Task UpdateAgentStatus(string status)
    {
        try
        {
            var agentId = _currentUserService.UserId;
            var tenantId = _tenantService.GetCurrentTenantId();

            if (agentId.HasValue && Enum.TryParse<AgentStatus>(status, out var agentStatus))
            {
                await _agentRoutingService.SetAgentStatusAsync(agentId.Value, agentStatus);
                
                await Clients.Group($"agents_{tenantId}").SendAsync("AgentStatusChanged", new
                {
                    AgentId = agentId.Value,
                    Status = status,
                    Timestamp = DateTime.UtcNow
                });

                _logger.LogInformation($"Agent {agentId} status updated to {status}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error updating agent status to {status}");
        }
    }

    public async Task AcceptConversation(string conversationId)
    {
        try
        {
            var agentId = _currentUserService.UserId;
            var tenantId = _tenantService.GetCurrentTenantId();

            if (agentId.HasValue && Guid.TryParse(conversationId, out var convId))
            {
                await _queueManagementService.RemoveFromQueueAsync(convId);
                var assigned = await _agentRoutingService.AssignConversationToAgentAsync(convId, agentId.Value);

                if (assigned)
                {
                    await Clients.Caller.SendAsync("ConversationAssigned", new
                    {
                        ConversationId = conversationId,
                        AgentId = agentId.Value,
                        Timestamp = DateTime.UtcNow
                    });

                    await Clients.OthersInGroup($"agents_{tenantId}").SendAsync("ConversationTaken", new
                    {
                        ConversationId = conversationId,
                        AgentId = agentId.Value,
                        Timestamp = DateTime.UtcNow
                    });

                    _logger.LogInformation($"Agent {agentId} accepted conversation {conversationId}");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error accepting conversation {conversationId}");
        }
    }

    public async Task TransferConversation(string conversationId, string targetAgentId, string reason)
    {
        try
        {
            var fromAgentId = _currentUserService.UserId;
            var tenantId = _tenantService.GetCurrentTenantId();

            if (fromAgentId.HasValue && 
                Guid.TryParse(conversationId, out var convId) && 
                Guid.TryParse(targetAgentId, out var toAgentId))
            {
                var transferred = await _agentRoutingService.TransferConversationAsync(convId, fromAgentId.Value, toAgentId);

                if (transferred)
                {
                    await Clients.Group($"agents_{tenantId}").SendAsync("ConversationTransferred", new
                    {
                        ConversationId = conversationId,
                        FromAgentId = fromAgentId.Value,
                        ToAgentId = toAgentId,
                        Reason = reason,
                        Timestamp = DateTime.UtcNow
                    });

                    _logger.LogInformation($"Conversation {conversationId} transferred from {fromAgentId} to {toAgentId}: {reason}");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error transferring conversation {conversationId}");
        }
    }

    public async Task EscalateConversation(string conversationId, string reason)
    {
        try
        {
            var agentId = _currentUserService.UserId;
            var tenantId = _tenantService.GetCurrentTenantId();

            if (Guid.TryParse(conversationId, out var convId))
            {
                var escalated = await _queueManagementService.EscalateConversationAsync(convId, reason);

                if (escalated)
                {
                    await Clients.Group($"agents_{tenantId}").SendAsync("ConversationEscalated", new
                    {
                        ConversationId = conversationId,
                        AgentId = agentId,
                        Reason = reason,
                        Priority = QueuePriority.Urgent.ToString(),
                        Timestamp = DateTime.UtcNow
                    });

                    _logger.LogInformation($"Conversation {conversationId} escalated by agent {agentId}: {reason}");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error escalating conversation {conversationId}");
        }
    }

    public async Task RequestAssistance(string conversationId, string message)
    {
        try
        {
            var agentId = _currentUserService.UserId;
            var tenantId = _tenantService.GetCurrentTenantId();

            if (agentId.HasValue)
            {
                await Clients.OthersInGroup($"agents_{tenantId}").SendAsync("AssistanceRequested", new
                {
                    ConversationId = conversationId,
                    RequestingAgentId = agentId.Value,
                    Message = message,
                    Timestamp = DateTime.UtcNow
                });

                _logger.LogInformation($"Agent {agentId} requested assistance for conversation {conversationId}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error requesting assistance for conversation {conversationId}");
        }
    }

    public async Task BroadcastToAgents(string message, string messageType = "info")
    {
        try
        {
            var tenantId = _tenantService.GetCurrentTenantId();
            var agentId = _currentUserService.UserId;

            var userTenant = await _context.UserTenants
                .FirstOrDefaultAsync(ut => ut.UserId == agentId && ut.TenantId == tenantId);

            if (userTenant?.Role == Shared.Domain.Enums.TenantRole.Admin)
            {
                await Clients.Group($"agents_{tenantId}").SendAsync("BroadcastMessage", new
                {
                    Message = message,
                    Type = messageType,
                    FromAgentId = agentId,
                    Timestamp = DateTime.UtcNow
                });

                _logger.LogInformation($"Admin {agentId} broadcasted message to all agents in tenant {tenantId}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting message to agents");
        }
    }

    public override async Task OnConnectedAsync()
    {
        var agentId = _currentUserService.UserId;
        var tenantId = _tenantService.GetCurrentTenantId();
        
        _logger.LogInformation($"Agent {agentId} connected to agent hub for tenant {tenantId}");
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var agentId = _currentUserService.UserId;
        var tenantId = _tenantService.GetCurrentTenantId();

        if (agentId.HasValue)
        {
            await _agentRoutingService.SetAgentStatusAsync(agentId.Value, AgentStatus.Offline);
            
            await Clients.Group($"agents_{tenantId}").SendAsync("AgentStatusChanged", new
            {
                AgentId = agentId.Value,
                Status = AgentStatus.Offline.ToString(),
                Timestamp = DateTime.UtcNow
            });
        }

        _logger.LogInformation($"Agent {agentId} disconnected from agent hub for tenant {tenantId}");
        await base.OnDisconnectedAsync(exception);
    }
}
