using Shared.Domain.Entities;
using WorkflowService.Models;

namespace WorkflowService.Services;

public interface IWorkflowTemplateService
{
    Task<List<WorkflowTemplate>> GetTemplatesAsync(string? category = null, bool publicOnly = true);
    Task<WorkflowTemplate?> GetTemplateAsync(Guid templateId);
    Task<WorkflowTemplate> CreateTemplateAsync(WorkflowTemplate template, Guid tenantId, Guid userId);
    Task<bool> UpdateTemplateAsync(WorkflowTemplate template);
    Task<bool> DeleteTemplateAsync(Guid templateId, Guid tenantId);
    Task<Workflow> CreateWorkflowFromTemplateAsync(Guid templateId, string workflowName, Guid tenantId, Guid userId);
    Task<List<string>> GetCategoriesAsync();
}
