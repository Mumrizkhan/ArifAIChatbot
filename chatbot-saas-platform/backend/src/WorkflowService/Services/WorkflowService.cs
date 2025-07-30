using Microsoft.EntityFrameworkCore;
using Shared.Application.Common.Interfaces;
using WorkflowService.Services;
using WorkflowService.Models;
using Shared.Domain.Entities;

namespace WorkflowService.Services;

public class WorkflowService : IWorkflowService
{
    private readonly IApplicationDbContext _context;
    private readonly IWorkflowDesignerService _designerService;
    private readonly ILogger<WorkflowService> _logger;

    public WorkflowService(
        IApplicationDbContext context,
        IWorkflowDesignerService designerService,
        ILogger<WorkflowService> logger)
    {
        _context = context;
        _designerService = designerService;
        _logger = logger;
    }

    public async Task<Workflow> CreateWorkflowAsync(CreateWorkflowRequest request, Guid tenantId, Guid userId)
    {
        try
        {
            var workflow = new Workflow
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                Description = request.Description,
                TenantId = tenantId,
                CreatedByUserId = userId,
                Status = WorkflowStatus.Draft,
                Definition = request.Definition ?? new WorkflowDefinition(),
                Variables = request.Variables,
                Tags = request.Tags,
                Trigger = request.Trigger ?? new WorkflowTrigger(),
                Settings = request.Settings ?? new WorkflowSettings(),
                TemplateId = request.TemplateId,
                CreatedAt = DateTime.UtcNow
            };

            if (request.Definition != null)
            {
                workflow.Definition = await _designerService.ValidateWorkflowDefinitionAsync(request.Definition);
            }

            _context.Set<Workflow>().Add(workflow);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Workflow {WorkflowId} created for tenant {TenantId}", workflow.Id, tenantId);
            return workflow;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating workflow for tenant {TenantId}", tenantId);
            throw;
        }
    }

    public async Task<List<Workflow>> GetWorkflowsAsync(Guid tenantId, int page = 1, int pageSize = 20)
    {
        try
        {
            return await _context.Set<Workflow>()
                .Where(w => w.TenantId == tenantId)
                .OrderByDescending(w => w.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting workflows for tenant {TenantId}", tenantId);
            throw;
        }
    }

    public async Task<Workflow?> GetWorkflowAsync(Guid workflowId, Guid tenantId)
    {
        try
        {
            return await _context.Set<Workflow>()
                .FirstOrDefaultAsync(w => w.Id == workflowId && w.TenantId == tenantId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting workflow {WorkflowId} for tenant {TenantId}", workflowId, tenantId);
            throw;
        }
    }

    public async Task<bool> UpdateWorkflowAsync(Workflow workflow)
    {
        try
        {
            var existingWorkflow = await _context.Set<Workflow>()
                .FirstOrDefaultAsync(w => w.Id == workflow.Id);

            if (existingWorkflow == null)
                return false;

            workflow.Definition = await _designerService.ValidateWorkflowDefinitionAsync(workflow.Definition);

            existingWorkflow.Name = workflow.Name;
            existingWorkflow.Description = workflow.Description;
            existingWorkflow.Definition = workflow.Definition;
            existingWorkflow.Variables = workflow.Variables;
            existingWorkflow.Tags = workflow.Tags;
            existingWorkflow.Trigger = workflow.Trigger;
            existingWorkflow.Settings = workflow.Settings;
            existingWorkflow.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating workflow {WorkflowId}", workflow.Id);
            return false;
        }
    }

    public async Task<bool> DeleteWorkflowAsync(Guid workflowId, Guid tenantId)
    {
        try
        {
            var workflow = await GetWorkflowAsync(workflowId, tenantId);
            if (workflow == null)
                return false;

            _context.Set<Workflow>().Remove(workflow);
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting workflow {WorkflowId}", workflowId);
            return false;
        }
    }

    public async Task<bool> ActivateWorkflowAsync(Guid workflowId, Guid tenantId)
    {
        try
        {
            var workflow = await GetWorkflowAsync(workflowId, tenantId);
            if (workflow == null)
                return false;

            await _designerService.ValidateWorkflowDefinitionAsync(workflow.Definition);

            workflow.Status = WorkflowStatus.Active;
            workflow.IsActive = true;
            workflow.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error activating workflow {WorkflowId}", workflowId);
            return false;
        }
    }

    public async Task<bool> DeactivateWorkflowAsync(Guid workflowId, Guid tenantId)
    {
        try
        {
            var workflow = await GetWorkflowAsync(workflowId, tenantId);
            if (workflow == null)
                return false;

            workflow.Status = WorkflowStatus.Inactive;
            workflow.IsActive = false;
            workflow.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating workflow {WorkflowId}", workflowId);
            return false;
        }
    }

    public async Task<Workflow> CloneWorkflowAsync(Guid workflowId, Guid tenantId, string newName)
    {
        try
        {
            var originalWorkflow = await GetWorkflowAsync(workflowId, tenantId);
            if (originalWorkflow == null)
                throw new ArgumentException("Workflow not found");

            var clonedWorkflow = new Workflow
            {
                Id = Guid.NewGuid(),
                Name = newName,
                Description = $"Clone of {originalWorkflow.Name}",
                TenantId = tenantId,
                CreatedByUserId = originalWorkflow.CreatedByUserId,
                Status = WorkflowStatus.Draft,
                Definition = originalWorkflow.Definition,
                Variables = new Dictionary<string, object>(originalWorkflow.Variables),
                Tags = new List<string>(originalWorkflow.Tags),
                Trigger = originalWorkflow.Trigger,
                Settings = originalWorkflow.Settings,
                CreatedAt = DateTime.UtcNow
            };

            _context.Set<Workflow>().Add(clonedWorkflow);
            await _context.SaveChangesAsync();

            return clonedWorkflow;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cloning workflow {WorkflowId}", workflowId);
            throw;
        }
    }

    public async Task<List<Workflow>> GetWorkflowsByTagAsync(string tag, Guid tenantId)
    {
        try
        {
            return await _context.Set<Workflow>()
                .Where(w => w.TenantId == tenantId && w.Tags.Contains(tag))
                .OrderByDescending(w => w.CreatedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting workflows by tag {Tag} for tenant {TenantId}", tag, tenantId);
            throw;
        }
    }

    public async Task<WorkflowStatistics> GetStatisticsAsync(Guid tenantId, DateTime? startDate = null, DateTime? endDate = null)
    {
        try
        {
            var start = startDate ?? DateTime.UtcNow.AddDays(-30);
            var end = endDate ?? DateTime.UtcNow;

            var workflows = await _context.Set<Workflow>()
                .Where(w => w.TenantId == tenantId)
                .ToListAsync();

            var executions = await _context.Set<WorkflowExecution>()
                .Where(e => e.TenantId == tenantId && e.StartedAt >= start && e.StartedAt <= end)
                .ToListAsync();

            var stepExecutions = await _context.Set<WorkflowStepExecution>()
                .Where(se => executions.Select(e => e.Id).Contains(se.WorkflowExecutionId))
                .ToListAsync();

            return new WorkflowStatistics
            {
                TotalWorkflows = workflows.Count,
                ActiveWorkflows = workflows.Count(w => w.IsActive),
                TotalExecutions = executions.Count,
                SuccessfulExecutions = executions.Count(e => e.Status == WorkflowExecutionStatus.Completed),
                FailedExecutions = executions.Count(e => e.Status == WorkflowExecutionStatus.Failed),
                SuccessRate = executions.Count > 0 ? (double)executions.Count(e => e.Status == WorkflowExecutionStatus.Completed) / executions.Count * 100 : 0,
                AverageExecutionTime = executions.Where(e => e.Duration.HasValue).Any() ? 
                    TimeSpan.FromMilliseconds(executions.Where(e => e.Duration.HasValue).Average(e => e.Duration!.Value.TotalMilliseconds)) : 
                    TimeSpan.Zero,
                StepTypeUsage = stepExecutions.GroupBy(se => se.StepType).ToDictionary(g => g.Key, g => g.Count()),
                ExecutionsByWorkflow = executions.GroupBy(e => e.WorkflowId).ToDictionary(g => g.Key.ToString(), g => g.Count()),
                PeriodStart = start,
                PeriodEnd = end
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting workflow statistics for tenant {TenantId}", tenantId);
            throw;
        }
    }
}
