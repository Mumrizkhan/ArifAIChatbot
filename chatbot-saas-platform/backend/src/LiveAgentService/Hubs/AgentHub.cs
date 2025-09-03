using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Shared.Application.Common.Interfaces;
using Shared.Domain.Entities;
using Shared.Domain.Enums;
using LiveAgentService.Services;
using AnalyticsService.Events;
using LiveAgentService.Models;

namespace LiveAgentService.Hubs;

//[Authorize]
public class AgentHub : Hub
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ITenantService _tenantService;
    private readonly ILiveAgentAnalyticsService _liveAgentAnalytics;
    private readonly IAgentRoutingService _agentRoutingService;
    private readonly IQueueManagementService _queueManagementService;
    private readonly ILogger<AgentHub> _logger;
    private readonly Dictionary<string, DateTime> _agentJoinTimes = new();
    private readonly Dictionary<string, int> _messageCounts = new();

    public AgentHub(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        ITenantService tenantService,
        ILiveAgentAnalyticsService liveAgentAnalytics,
         IAgentRoutingService agentRoutingService,
        IQueueManagementService queueManagementService,
        ILogger<AgentHub> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _tenantService = tenantService;
        _liveAgentAnalytics = liveAgentAnalytics;
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
                    Timestamp = DateTime.UtcNow.ToString("O")
                });
                await Groups.AddToGroupAsync(Context.ConnectionId, $"agent_{agentId}");
               // await _agentRoutingService.SetAgentStatusAsync(agentId.Value, AgentStatus.Online);

                await Clients.Group($"agent_{agentId}").SendAsync("AgentStatusChanged", new
                {
                    AgentId = agentId.Value,
                    Status = AgentStatus.Online.ToString(),
                    Timestamp = DateTime.UtcNow.ToString("O")
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
                    Timestamp = DateTime.UtcNow.ToString("O")
                });

                _logger.LogInformation($"Agent {agentId} left tenant {tenantId} group");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error leaving agent group");
        }
    }

    public async Task JoinConversation(string conversationId)
    {
        try
        {
            var userId = _currentUserService.UserId;
            var tenantId = _tenantService.GetCurrentTenantId();
            
            if (userId == null || !Guid.TryParse(conversationId, out var convId))
                return;

            var conversation = await _context.Conversations
                .Include(c => c.AssignedAgent)
                .FirstOrDefaultAsync(c => c.Id == convId && c.TenantId == tenantId);

            if (conversation == null)
                return;

            await Groups.AddToGroupAsync(Context.ConnectionId, $"conversation_{conversationId}");
            
            // Track join time for session duration analytics
            var sessionKey = $"{userId}_{conversationId}";
            _agentJoinTimes[sessionKey] = DateTime.UtcNow;
            _messageCounts[sessionKey] = 0;

            // Generate session ID for analytics
            var sessionId = Guid.NewGuid().ToString();

            // Publish live agent joined analytics event
            await _liveAgentAnalytics.PublishAgentJoinedAsync(
                convId,
                userId.ToString(),
                conversation.AssignedAgent?.FirstName ?? "Unknown Agent",
                conversation.CustomerEmail, // Using email as userId
                tenantId.ToString(),
                sessionId,
                null, // Response time - could be calculated from assignment time
                false, // Not a transfer for now
                null // No previous agent for simple join
            );

            await Clients.Group($"conversation_{conversationId}")
                .SendAsync("AgentJoined", new { AgentId = userId, ConversationId = conversationId });

            _logger.LogInformation("Agent {AgentId} joined conversation {ConversationId}", userId, conversationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error joining conversation {ConversationId}", conversationId);
        }
    }

    public async Task LeaveConversation(string conversationId)
    {
        try
        {
            var userId = _currentUserService.UserId;
            var tenantId = _tenantService.GetCurrentTenantId();
            
            if (userId == null || !Guid.TryParse(conversationId, out var convId))
                return;

            var conversation = await _context.Conversations
                .Include(c => c.AssignedAgent)
                .FirstOrDefaultAsync(c => c.Id == convId && c.TenantId == tenantId);

            if (conversation == null)
                return;

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"conversation_{conversationId}");
            
            // Calculate session duration and get message count
            var sessionKey = $"{userId}_{conversationId}";
            var sessionDuration = TimeSpan.Zero;
            var messagesSent = 0;

            if (_agentJoinTimes.TryGetValue(sessionKey, out var joinTime))
            {
                sessionDuration = DateTime.UtcNow - joinTime;
                _agentJoinTimes.Remove(sessionKey);
            }

            if (_messageCounts.TryGetValue(sessionKey, out var msgCount))
            {
                messagesSent = msgCount;
                _messageCounts.Remove(sessionKey);
            }

            // Generate session ID for analytics
            var sessionId = Guid.NewGuid().ToString();

            // Publish live agent left analytics event
            await _liveAgentAnalytics.PublishAgentLeftAsync(
                convId,
                userId.ToString(),
                conversation.AssignedAgent?.FirstName ?? "Unknown Agent",
                conversation.CustomerEmail, // Using email as userId
                tenantId.ToString(),
                sessionId,
                sessionDuration,
                LeaveReason.ConversationEnded, // Assume normal conversation end
                messagesSent,
                false, // Not a transfer
                null // No transfer reason
            );

            await Clients.Group($"conversation_{conversationId}")
                .SendAsync("AgentLeft", new { AgentId = userId, ConversationId = conversationId });

            _logger.LogInformation("Agent {AgentId} left conversation {ConversationId}", userId, conversationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error leaving conversation {ConversationId}", conversationId);
        }
    }

    public async Task StartTyping(string conversationId)
    {
        try
        {
            var agentId = _currentUserService.UserId;
            var typingInfo = new
            {
                UserId = agentId,
                UserName = "Agent", // Could be enhanced with actual agent name
                ConversationId = conversationId
            };

            await Clients.OthersInGroup($"conversation_{conversationId}")
                .SendAsync("UserStartedTyping", typingInfo);

            _logger.LogInformation($"Agent {agentId} started typing in conversation {conversationId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error broadcasting agent typing status for conversation {conversationId}");
        }
    }

    public async Task StopTyping(string conversationId)
    {
        try
        {
            var agentId = _currentUserService.UserId;
            var typingInfo = new
            {
                UserId = agentId,
                ConversationId = conversationId
            };

            await Clients.OthersInGroup($"conversation_{conversationId}")
                .SendAsync("UserStoppedTyping", typingInfo);

            _logger.LogInformation($"Agent {agentId} stopped typing in conversation {conversationId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error broadcasting agent stop typing status for conversation {conversationId}");
        }
    }

    public async Task MarkMessageAsRead(string messageId, string conversationId)
    {
        try
        {
            var agentId = _currentUserService.UserId;
            var readNotification = new
            {
                MessageId = messageId,
                ConversationId = conversationId,
                ReaderId = agentId?.ToString(),
                ReaderType = "agent",
                ReadAt = DateTime.UtcNow.ToString("O")
            };

            await Clients.OthersInGroup($"conversation_{conversationId}")
                .SendAsync("MessageMarkedAsRead", readNotification);

            _logger.LogInformation($"Agent {agentId} marked message {messageId} as read in conversation {conversationId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error broadcasting agent message read status for message {messageId}");
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
                    Timestamp = DateTime.UtcNow.ToString("O")
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
                    // Get conversation details for complete assignment info
                    var conversation = await _context.Conversations
                        .FirstOrDefaultAsync(c => c.Id == convId && c.TenantId == tenantId);

                    var assignmentPayload = new
                    {
                        ConversationId = conversationId,
                        AgentId = agentId.Value,
                        AgentName = "Agent", // Could be enhanced to get actual agent name
                        CustomerName = conversation?.CustomerName,
                        CustomerEmail = conversation?.CustomerEmail,
                        Subject = conversation?.Subject,
                        Language = conversation?.Language,
                        Status = "active",
                        Timestamp = DateTime.UtcNow.ToString("O") // ISO 8601 format
                    };

                    await Clients.Caller.SendAsync("ConversationAssigned", assignmentPayload);

                    await Clients.OthersInGroup($"agents_{tenantId}").SendAsync("ConversationTaken", new
                    {
                        ConversationId = conversationId,
                        AgentId = agentId.Value,
                        Timestamp = DateTime.UtcNow.ToString("O")
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

    public async Task TransferConversation(string conversationId, string targetAgentId, string reason, string transferNotes = "")
    {
        try
        {
            var userId = _currentUserService.UserId;
            var tenantId = _tenantService.GetCurrentTenantId();
            
            if (userId == null || !Guid.TryParse(conversationId, out var convId) || !Guid.TryParse(targetAgentId, out var toAgent))
                return;

            var conversation = await _context.Conversations
                .Include(c => c.Messages)
                .FirstOrDefaultAsync(c => c.Id == convId && c.TenantId == tenantId);

            if (conversation == null)
                return;

            var conversationDuration = DateTime.UtcNow - conversation.StartedAt;
            conversation.AssignedAgentId = toAgent;
            conversation.UpdatedAt = DateTime.UtcNow;
            
            await _context.SaveChangesAsync();

            // Generate session ID for analytics
            var sessionId = Guid.NewGuid().ToString();

            // Publish live agent transfer analytics event
            await _liveAgentAnalytics.PublishTransferAsync(
                convId,
                userId.ToString(),
                targetAgentId,
                conversation.CustomerEmail, // Using email as userId
                tenantId.ToString(),
                sessionId,
                reason,
                conversationDuration,
                !string.IsNullOrEmpty(transferNotes), // Warm transfer if notes provided
                transferNotes
            );

            await Clients.Group($"conversation_{conversationId}")
                .SendAsync("ConversationTransferred", new
                {
                    ConversationId = conversationId,
                    FromAgentId = userId,
                    ToAgentId = targetAgentId,
                    Reason = reason,
                    TransferNotes = transferNotes
                });

            _logger.LogInformation("Conversation {ConversationId} transferred from agent {FromAgentId} to agent {ToAgentId}", 
                conversationId, userId, targetAgentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error transferring conversation {ConversationId}", conversationId);
        }
    }

    public async Task EscalateConversation(string conversationId, string toAgentId, string reason)
    {
        try
        {
            var userId = _currentUserService.UserId;
            var tenantId = _tenantService.GetCurrentTenantId();
            
            if (userId == null || !Guid.TryParse(conversationId, out var convId))
                return;

            var conversation = await _context.Conversations
                .Include(c => c.Messages)
                .FirstOrDefaultAsync(c => c.Id == convId && c.TenantId == tenantId);

            if (conversation == null)
                return;

            var conversationDuration = DateTime.UtcNow - conversation.StartedAt;
            var messageCount = conversation.Messages?.Count ?? 0;

            // Generate session ID for analytics
            var sessionId = Guid.NewGuid().ToString();

            // Publish live agent escalation analytics event
            await _liveAgentAnalytics.PublishEscalationAsync(
                convId,
                userId.ToString(),
                toAgentId,
                conversation.CustomerEmail, // Using email as userId
                tenantId.ToString(),
                sessionId,
                reason,
                conversationDuration,
                messageCount,
                null, // From department - could be determined from agent
                null  // To department - could be determined from target agent
            );

            await Clients.Group($"conversation_{conversationId}")
                .SendAsync("ConversationEscalated", new
                {
                    ConversationId = conversationId,
                    FromAgentId = userId,
                    ToAgentId = toAgentId,
                    Reason = reason
                });

            _logger.LogInformation("Conversation {ConversationId} escalated from agent {FromAgentId} to agent {ToAgentId}", 
                conversationId, userId, toAgentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error escalating conversation {ConversationId}", conversationId);
        }
    }

    private static string DetermineMessageIntent(string content)
    {
        // Simple intent detection based on content keywords
        var lowerContent = content.ToLower();
        
        if (lowerContent.Contains("hello") || lowerContent.Contains("hi"))
            return "greeting";
        if (lowerContent.Contains("thank"))
            return "gratitude";
        if (lowerContent.Contains("help") || lowerContent.Contains("assist"))
            return "assistance";
        if (lowerContent.Contains("problem") || lowerContent.Contains("issue"))
            return "problem_solving";
        if (lowerContent.Contains("goodbye") || lowerContent.Contains("bye"))
            return "farewell";
        
        return "information";
    }

    private static string DetermineResponseQuality(string content)
    {
        // Simple quality assessment based on content length and characteristics
        if (content.Length < 10)
            return "brief";
        if (content.Length > 200)
            return "detailed";
        if (content.Contains("?"))
            return "clarifying";
        
        return "standard";
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
                    Timestamp = DateTime.UtcNow.ToString("O")
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
                    Timestamp = DateTime.UtcNow.ToString("O")
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
                Timestamp = DateTime.UtcNow.ToString("O")
            });
        }

        _logger.LogInformation($"Agent {agentId} disconnected from agent hub for tenant {tenantId}");
        await base.OnDisconnectedAsync(exception);
    }
}
