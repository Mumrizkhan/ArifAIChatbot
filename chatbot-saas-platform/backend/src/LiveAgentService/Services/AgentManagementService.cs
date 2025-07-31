using Microsoft.EntityFrameworkCore;
using Shared.Application.Common.Interfaces;
using Shared.Domain.Entities;
using LiveAgentService.Models;
using Shared.Domain.Enums;

namespace LiveAgentService.Services;

public class AgentManagementService : IAgentManagementService
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<AgentManagementService> _logger;

    public AgentManagementService(IApplicationDbContext context, ILogger<AgentManagementService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<AgentDto>> GetAllAgentsAsync(Guid tenantId)
    {
        var agents = await _context.Users
            .Where(u => u.UserTenants.Any(ut => ut.TenantId == tenantId && ut.Role == TenantRole.Agent))
            .ToListAsync();

        return agents.Select(MapToAgentDto).ToList();
    }

    public async Task<AgentProfileDto?> GetAgentProfileAsync(Guid id, Guid tenantId)
    {
        var agent = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == id && 
                u.UserTenants.Any(ut => ut.TenantId == tenantId));

        return agent != null ? MapToAgentProfileDto(agent) : null;
    }

    public async Task<bool> UpdateAgentProfileAsync(Guid id, UpdateAgentProfileRequest request, Guid tenantId)
    {
        var agent = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == id && 
                u.UserTenants.Any(ut => ut.TenantId == tenantId));

        if (agent == null) return false;

        if (!string.IsNullOrEmpty(request.Name))
        {
            var nameParts = request.Name.Split(' ', 2);
            agent.FirstName = nameParts[0];
            agent.LastName = nameParts.Length > 1 ? nameParts[1] : "";
        }

        if (!string.IsNullOrEmpty(request.Email))
            agent.Email = request.Email;

        if (!string.IsNullOrEmpty(request.Language))
            agent.PreferredLanguage = request.Language;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<AgentStatsDto> GetAgentStatsAsync(Guid agentId, Guid tenantId, DateTime? startDate, DateTime? endDate)
    {
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

        return new AgentStatsDto
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
        };
    }

    public async Task<AgentPerformanceMetrics> GetPerformanceMetricsAsync(Guid agentId, DateTime? startDate, DateTime? endDate)
    {
        var start = startDate ?? DateTime.UtcNow.AddDays(-30);
        var end = endDate ?? DateTime.UtcNow;

        var conversations = await _context.Conversations
            .Where(c => c.AssignedAgentId == agentId && 
                       c.CreatedAt >= start && c.CreatedAt <= end)
            .ToListAsync();

        var totalMessages = await _context.Messages
            .Where(m => m.SenderId == agentId && 
                       m.CreatedAt >= start && m.CreatedAt <= end)
            .CountAsync();

        return new AgentPerformanceMetrics
        {
            AgentId = agentId,
            PeriodStart = start,
            PeriodEnd = end,
            TotalConversations = conversations.Count,
            ResolvedConversations = conversations.Count(c => c.Status ==  ConversationStatus.Resolved),
            AverageResponseTime =new TimeSpan(Convert.ToInt64( conversations.Any() ? conversations.Average(c => c.AverageResponseTime ?? 0) : 0)),
            AverageResolutionTime = new TimeSpan(),
            CustomerSatisfactionScore = conversations.Where(c => c.Rating.HasValue).Any() 
                ? conversations.Where(c => c.Rating.HasValue).Average(c => c.Rating.Value) 
                : 0,
            TotalMessages = totalMessages
        };
    }

    public async Task<bool> UpdateAgentStatusAsync(Guid agentId, string status, Guid tenantId)
    {
        var agent = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == agentId && 
                u.UserTenants.Any(ut => ut.TenantId == tenantId));

        if (agent == null) return false;

        agent.Status = status;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<object> GetAgentWorkloadsAsync(Guid tenantId)
    {
        var agents = await _context.Users
            .Where(u => u.UserTenants.Any(ut => ut.TenantId == tenantId && ut.Role == TenantRole.Agent))
            .Select(u => new
            {
                u.Id,
                u.FirstName,
                u.LastName,
                u.Status,
                ActiveConversations = u.AssignedConversations.Count(c => c.Status == ConversationStatus.Active),
                TotalConversations = u.AssignedConversations.Count()
            })
            .ToListAsync();

        return agents;
    }

    public async Task<object> GetMyConversationsAsync(Guid agentId, Guid tenantId)
    {
        var conversations = await _context.Conversations
            .Where(c => c.AssignedAgentId == agentId && c.TenantId == tenantId)
            .Select(c => new
            {
                c.Id,
                c.Status,
                c.CreatedAt,
                c.UpdatedAt,
                MessageCount = c.MessageCount,
                LastMessage = c.Messages.OrderByDescending(m => m.CreatedAt).FirstOrDefault()
            })
            .ToListAsync();

        return conversations;
    }

    public async Task<bool> AssignConversationAsync(Guid conversationId, Guid agentId, Guid tenantId)
    {
        var conversation = await _context.Conversations
            .FirstOrDefaultAsync(c => c.Id == conversationId && c.TenantId == tenantId);

        if (conversation == null) return false;

        conversation.AssignedAgentId = agentId;
        conversation.Status = ConversationStatus.Active;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<object> GetQueueAsync(Guid tenantId)
    {
        var queuedConversations = await _context.Conversations
            .Where(c => c.TenantId == tenantId && c.AssignedAgentId == null && c.Status == ConversationStatus.Queued)
            .Select(c => new
            {
                c.Id,
                c.CreatedAt,
                c.Priority,
                WaitTime = DateTime.UtcNow - c.CreatedAt,
                MessageCount = c.MessageCount
            })
            .ToListAsync();

        return new
        {
            QueuedConversations = queuedConversations,
            TotalInQueue = queuedConversations.Count,
            AverageWaitTime = queuedConversations.Any() ? 
                queuedConversations.Average(c => (DateTime.UtcNow - c.CreatedAt).TotalMinutes) : 0
        };
    }

    private AgentDto MapToAgentDto(User user)
    {
        return new AgentDto
        {
            Id = user.Id,
            Name = $"{user.FirstName} {user.LastName}",
            Email = user.Email,
            Avatar = user.AvatarUrl,
            Status = "Available",
            Skills = new string[] { },
            Specializations = new string[] { }
        };
    }

    private AgentProfileDto MapToAgentProfileDto(User user)
    {
        return new AgentProfileDto
        {
            Id = user.Id,
            Name = $"{user.FirstName} {user.LastName}",
            Email = user.Email,
            Phone = "",
            Avatar = user.AvatarUrl,
            Bio = "",
            Location = "",
            Timezone = "UTC",
            Language = user.PreferredLanguage,
            Skills = new string[] { },
            Specializations = new string[] { },
            Status = "Available"
        };
    }
}
