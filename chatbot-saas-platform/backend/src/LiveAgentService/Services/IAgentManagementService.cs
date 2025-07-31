using LiveAgentService.Models;
using Shared.Domain.Entities;

namespace LiveAgentService.Services;

public interface IAgentManagementService
{
    Task<List<AgentDto>> GetAllAgentsAsync(Guid tenantId);
    Task<AgentProfileDto?> GetAgentProfileAsync(Guid id, Guid tenantId);
    Task<bool> UpdateAgentProfileAsync(Guid id, UpdateAgentProfileRequest request, Guid tenantId);
    Task<AgentStatsDto> GetAgentStatsAsync(Guid agentId, Guid tenantId, DateTime? startDate, DateTime? endDate);
    Task<AgentPerformanceMetrics> GetPerformanceMetricsAsync(Guid agentId, DateTime? startDate, DateTime? endDate);
    Task<bool> UpdateAgentStatusAsync(Guid agentId, string status, Guid tenantId);
}
