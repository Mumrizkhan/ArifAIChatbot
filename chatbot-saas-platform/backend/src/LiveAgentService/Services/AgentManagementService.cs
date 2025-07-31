using Microsoft.EntityFrameworkCore;
using Shared.Application.Common.Interfaces;
using Shared.Domain.Entities;
using LiveAgentService.Models;

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
            .Where(u => u.TenantId == tenantId && u.Role == "Agent")
            .ToListAsync();

        return agents.Select(MapToAgentDto).ToList();
    }

    public async Task<AgentProfileDto?> GetAgentProfileAsync(Guid id, Guid tenantId)
    {
        var agent = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == id && u.TenantId == tenantId && u.Role == "Agent");

        return agent != null ? MapToAgentProfileDto(agent) : null;
    }

    public async Task<bool> UpdateAgentProfileAsync(Guid id, UpdateAgentProfileRequest request, Guid tenantId)
    {
        var agent = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == id && u.TenantId == tenantId && u.Role == "Agent");

        if (agent == null) return false;

        if (!string.IsNullOrEmpty(request.FirstName))
            agent.FirstName = request.FirstName;
        
        if (!string.IsNullOrEmpty(request.LastName))
            agent.LastName = request.LastName;

        agent.UpdatedAt = DateTime.UtcNow;
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
            .ToListAsync();

        return new AgentStatsDto
        {
            TotalConversations = conversations.Count,
            ActiveConversations = conversations.Count(c => c.Status == "Active"),
            ResolvedConversations = conversations.Count(c => c.Status == "Resolved"),
            AverageResponseTime = conversations.Any() ? conversations.Average(c => c.AverageResponseTime ?? 0) : 0,
            CustomerSatisfactionRating = conversations.Where(c => c.Rating.HasValue).Any() 
                ? conversations.Where(c => c.Rating.HasValue).Average(c => c.Rating.Value) 
                : 0
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
            ResolvedConversations = conversations.Count(c => c.Status == "Resolved"),
            AverageResponseTime = conversations.Any() ? conversations.Average(c => c.AverageResponseTime ?? 0) : 0,
            AverageResolutionTime = 0,
            CustomerSatisfactionScore = conversations.Where(c => c.Rating.HasValue).Any() 
                ? conversations.Where(c => c.Rating.HasValue).Average(c => c.Rating.Value) 
                : 0,
            TotalMessages = totalMessages
        };
    }

    public async Task<bool> UpdateAgentStatusAsync(Guid agentId, string status, Guid tenantId)
    {
        var agent = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == agentId && u.TenantId == tenantId && u.Role == "Agent");

        if (agent == null) return false;

        agent.IsOnline = status == "online";
        agent.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }

    private AgentDto MapToAgentDto(User user)
    {
        return new AgentDto
        {
            Id = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Status = user.IsOnline ? "online" : "offline",
            IsOnline = user.IsOnline,
            CreatedAt = user.CreatedAt
        };
    }

    private AgentProfileDto MapToAgentProfileDto(User user)
    {
        return new AgentProfileDto
        {
            Id = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            AvatarUrl = user.AvatarUrl,
            Status = user.IsOnline ? "online" : "offline",
            IsOnline = user.IsOnline,
            Skills = new List<string>()
        };
    }
}
