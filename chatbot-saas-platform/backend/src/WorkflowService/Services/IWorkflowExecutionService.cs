using Shared.Domain.Entities;
using WorkflowService.Models;

namespace WorkflowService.Services;

public interface IWorkflowExecutionService
{
    Task<Guid> ExecuteWorkflowAsync(Guid workflowId, ExecuteWorkflowRequest request, Guid tenantId, Guid? userId = null);
    Task<WorkflowExecution?> GetExecutionAsync(Guid executionId, Guid tenantId);
    Task<List<WorkflowExecution>> GetExecutionsAsync(Guid workflowId, Guid tenantId, int page = 1, int pageSize = 20);
    Task<bool> CancelExecutionAsync(Guid executionId, Guid tenantId);
    Task<bool> RetryExecutionAsync(Guid executionId, Guid tenantId);
    Task ProcessPendingExecutionsAsync();
    Task<WorkflowExecution> ExecuteStepAsync(WorkflowExecution execution, WorkflowStep step, Dictionary<string, object> inputData);
}
