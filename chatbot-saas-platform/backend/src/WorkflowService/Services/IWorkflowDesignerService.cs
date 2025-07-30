using Shared.Domain.Entities;
using WorkflowService.Models;

namespace WorkflowService.Services;

public interface IWorkflowDesignerService
{
    Task<WorkflowDefinition> ValidateWorkflowDefinitionAsync(WorkflowDefinition definition);
    Task<List<WorkflowStep>> GetAvailableStepTypesAsync();
    Task<WorkflowStep> CreateStepAsync(WorkflowStepType stepType, Dictionary<string, object> configuration);
    Task<WorkflowConnection> CreateConnectionAsync(string sourceStepId, string sourcePort, string targetStepId, string targetPort);
    Task<bool> ValidateConnectionAsync(WorkflowConnection connection, WorkflowDefinition definition);
    Task<WorkflowDefinition> OptimizeWorkflowAsync(WorkflowDefinition definition);
    Task<List<string>> GetWorkflowVariablesAsync(WorkflowDefinition definition);
}
