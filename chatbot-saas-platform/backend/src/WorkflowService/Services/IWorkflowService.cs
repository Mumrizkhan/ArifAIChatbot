using Shared.Domain.Entities;
using WorkflowService.Models;

namespace WorkflowService.Services;

public interface IWorkflowService
{
    Task<Workflow> CreateWorkflowAsync(CreateWorkflowRequest request, Guid tenantId, Guid userId);
    Task<List<Workflow>> GetWorkflowsAsync(Guid tenantId, int page = 1, int pageSize = 20);
    Task<Workflow?> GetWorkflowAsync(Guid workflowId, Guid tenantId);
    Task<bool> UpdateWorkflowAsync(Workflow workflow);
    Task<bool> DeleteWorkflowAsync(Guid workflowId, Guid tenantId);
    Task<bool> ActivateWorkflowAsync(Guid workflowId, Guid tenantId);
    Task<bool> DeactivateWorkflowAsync(Guid workflowId, Guid tenantId);
    Task<Workflow> CloneWorkflowAsync(Guid workflowId, Guid tenantId, string newName);
    Task<List<Workflow>> GetWorkflowsByTagAsync(string tag, Guid tenantId);
    Task<WorkflowStatistics> GetStatisticsAsync(Guid tenantId, DateTime? startDate = null, DateTime? endDate = null);
}
