using Azure.Core;
using LiveAgentService.Models;
using Microsoft.Extensions.Logging;
using Shared.Domain.Entities;

namespace LiveAgentService.Services;

public interface IAgentRoutingService
{
    Task<Guid?> FindAvailableAgentAsync(Guid tenantId, string? department = null, string? language = null);
    Task<bool> AssignConversationToAgentAsync(Guid conversationId, Guid agentId);
    Task<bool> TransferConversationAsync(Guid conversationId, Guid fromAgentId, Guid toAgentId);
    Task<List<AgentWorkload>> GetAgentWorkloadsAsync(Guid tenantId);
    Task<bool> SetAgentStatusAsync(Guid agentId, AgentStatus status);
    Task<AgentStatus> GetAgentStatusAsync(Guid agentId);
    Task<List<ConversationAssignment>> GetAgentConversationsAsync(Guid agentId);
    Task NotifyAgentsOfEscalationAsync(Guid tenantId, object escalationData);
    Task<bool> UpdateConversationStatusAsync(Guid conversationId, string status, Guid value);
}
