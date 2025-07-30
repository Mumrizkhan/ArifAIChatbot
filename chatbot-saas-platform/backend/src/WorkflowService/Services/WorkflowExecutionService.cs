using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using Shared.Application.Common.Interfaces;
using WorkflowService.Services;
using WorkflowService.Models;
using WorkflowService.Hubs;
using Hangfire;
using Shared.Domain.Entities;

namespace WorkflowService.Services;

public class WorkflowExecutionService : IWorkflowExecutionService
{
    private readonly IApplicationDbContext _context;
    private readonly IExternalIntegrationService _integrationService;
    private readonly IHubContext<WorkflowHub> _hubContext;
    private readonly ILogger<WorkflowExecutionService> _logger;

    public WorkflowExecutionService(
        IApplicationDbContext context,
        IExternalIntegrationService integrationService,
        IHubContext<WorkflowHub> hubContext,
        ILogger<WorkflowExecutionService> logger)
    {
        _context = context;
        _integrationService = integrationService;
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task<Guid> ExecuteWorkflowAsync(Guid workflowId, ExecuteWorkflowRequest request, Guid tenantId, Guid? userId = null)
    {
        try
        {
            var workflow = await _context.Set<Workflow>()
                .FirstOrDefaultAsync(w => w.Id == workflowId && w.TenantId == tenantId && w.IsActive);

            if (workflow == null)
                throw new ArgumentException("Workflow not found or inactive");

            var execution = new WorkflowExecution
            {
                Id = Guid.NewGuid(),
                WorkflowId = workflowId,
                TenantId = tenantId,
                TriggeredByUserId = userId,
                Status = WorkflowExecutionStatus.Running,
                InputData = request.InputData,
                StartedAt = DateTime.UtcNow,
                TriggerSource = request.TriggerSource,
                CreatedAt = DateTime.UtcNow
            };

            _context.Set<WorkflowExecution>().Add(execution);
            await _context.SaveChangesAsync();

            workflow.ExecutionCount++;
            workflow.LastExecutedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            if (request.WaitForCompletion)
            {
                await ExecuteWorkflowStepsAsync(execution, workflow);
            }
            else
            {
                BackgroundJob.Enqueue(() => ExecuteWorkflowStepsAsync(execution, workflow));
            }

            return execution.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing workflow {WorkflowId}", workflowId);
            throw;
        }
    }

    public async Task<WorkflowExecution?> GetExecutionAsync(Guid executionId, Guid tenantId)
    {
        try
        {
            return await _context.Set<WorkflowExecution>()
                .Include(e => e.StepExecutions)
                .FirstOrDefaultAsync(e => e.Id == executionId && e.TenantId == tenantId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting workflow execution {ExecutionId}", executionId);
            throw;
        }
    }

    public async Task<List<WorkflowExecution>> GetExecutionsAsync(Guid workflowId, Guid tenantId, int page = 1, int pageSize = 20)
    {
        try
        {
            return await _context.Set<WorkflowExecution>()
                .Where(e => e.WorkflowId == workflowId && e.TenantId == tenantId)
                .OrderByDescending(e => e.StartedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting workflow executions for workflow {WorkflowId}", workflowId);
            throw;
        }
    }

    public async Task<bool> CancelExecutionAsync(Guid executionId, Guid tenantId)
    {
        try
        {
            var execution = await GetExecutionAsync(executionId, tenantId);
            if (execution == null || execution.Status != WorkflowExecutionStatus.Running)
                return false;

            execution.Status = WorkflowExecutionStatus.Cancelled;
            execution.CompletedAt = DateTime.UtcNow;
            execution.Duration = execution.CompletedAt - execution.StartedAt;
            execution.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await _hubContext.Clients.Group($"tenant_{tenantId}")
                .SendAsync("WorkflowExecutionCancelled", new { ExecutionId = executionId });

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling workflow execution {ExecutionId}", executionId);
            return false;
        }
    }

    public async Task<bool> RetryExecutionAsync(Guid executionId, Guid tenantId)
    {
        try
        {
            var execution = await GetExecutionAsync(executionId, tenantId);
            if (execution == null || execution.Status != WorkflowExecutionStatus.Failed)
                return false;

            var workflow = await _context.Set<Workflow>()
                .FirstOrDefaultAsync(w => w.Id == execution.WorkflowId);

            if (workflow == null)
                return false;

            execution.Status = WorkflowExecutionStatus.Running;
            execution.ErrorMessage = null;
            execution.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            BackgroundJob.Enqueue(() => ExecuteWorkflowStepsAsync(execution, workflow));

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrying workflow execution {ExecutionId}", executionId);
            return false;
        }
    }

    public async Task ProcessPendingExecutionsAsync()
    {
        try
        {
            var pendingExecutions = await _context.Set<WorkflowExecution>()
                .Where(e => e.Status == WorkflowExecutionStatus.Pending)
                .Take(10)
                .ToListAsync();

            foreach (var execution in pendingExecutions)
            {
                var workflow = await _context.Set<Workflow>()
                    .FirstOrDefaultAsync(w => w.Id == execution.WorkflowId);

                if (workflow != null)
                {
                    BackgroundJob.Enqueue(() => ExecuteWorkflowStepsAsync(execution, workflow));
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing pending workflow executions");
        }
    }

    public async Task<WorkflowExecution> ExecuteStepAsync(WorkflowExecution execution, WorkflowStep step, Dictionary<string, object> inputData)
    {
        var stepExecution = new WorkflowStepExecution
        {
            Id = Guid.NewGuid(),
            WorkflowExecutionId = execution.Id,
            StepId = step.Id,
            StepName = step.Name,
            StepType = step.Type,
            Status = WorkflowStepStatus.Running,
            InputData = inputData,
            StartedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        execution.StepExecutions.Add(stepExecution);
        await _context.SaveChangesAsync();

        try
        {
            var outputData = await ExecuteStepLogicAsync(step, inputData);
            
            stepExecution.Status = WorkflowStepStatus.Completed;
            stepExecution.OutputData = outputData;
            stepExecution.CompletedAt = DateTime.UtcNow;
            stepExecution.Duration = stepExecution.CompletedAt - stepExecution.StartedAt;
        }
        catch (Exception ex)
        {
            stepExecution.Status = WorkflowStepStatus.Failed;
            stepExecution.ErrorMessage = ex.Message;
            stepExecution.CompletedAt = DateTime.UtcNow;
            stepExecution.Duration = stepExecution.CompletedAt - stepExecution.StartedAt;
            
            _logger.LogError(ex, "Error executing workflow step {StepId}", step.Id);
        }

        stepExecution.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        await _hubContext.Clients.Group($"tenant_{execution.TenantId}")
            .SendAsync("WorkflowStepCompleted", new
            {
                ExecutionId = execution.Id,
                StepId = step.Id,
                Status = stepExecution.Status.ToString(),
                OutputData = stepExecution.OutputData
            });

        return execution;
    }

    private async Task ExecuteWorkflowStepsAsync(WorkflowExecution execution, Workflow workflow)
    {
        try
        {
            var startStep = workflow.Definition.Steps.FirstOrDefault(s => s.IsStartStep);
            if (startStep == null)
            {
                throw new InvalidOperationException("No start step found in workflow");
            }

            var currentData = execution.InputData;
            var visitedSteps = new HashSet<string>();

            await ExecuteStepRecursiveAsync(execution, workflow, startStep, currentData, visitedSteps);

            execution.Status = WorkflowExecutionStatus.Completed;
            execution.CompletedAt = DateTime.UtcNow;
            execution.Duration = execution.CompletedAt - execution.StartedAt;
            execution.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await _hubContext.Clients.Group($"tenant_{execution.TenantId}")
                .SendAsync("WorkflowExecutionCompleted", new
                {
                    ExecutionId = execution.Id,
                    Status = execution.Status.ToString(),
                    Duration = execution.Duration,
                    OutputData = execution.OutputData
                });
        }
        catch (Exception ex)
        {
            execution.Status = WorkflowExecutionStatus.Failed;
            execution.ErrorMessage = ex.Message;
            execution.CompletedAt = DateTime.UtcNow;
            execution.Duration = execution.CompletedAt - execution.StartedAt;
            execution.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogError(ex, "Error executing workflow {WorkflowId}", workflow.Id);

            await _hubContext.Clients.Group($"tenant_{execution.TenantId}")
                .SendAsync("WorkflowExecutionFailed", new
                {
                    ExecutionId = execution.Id,
                    ErrorMessage = ex.Message
                });
        }
    }

    private async Task ExecuteStepRecursiveAsync(WorkflowExecution execution, Workflow workflow, WorkflowStep step, Dictionary<string, object> inputData, HashSet<string> visitedSteps)
    {
        if (visitedSteps.Contains(step.Id))
        {
            _logger.LogWarning("Circular dependency detected in workflow {WorkflowId} at step {StepId}", workflow.Id, step.Id);
            return;
        }

        visitedSteps.Add(step.Id);

        await ExecuteStepAsync(execution, step, inputData);

        var stepExecution = execution.StepExecutions.LastOrDefault(se => se.StepId == step.Id);
        if (stepExecution?.Status != WorkflowStepStatus.Completed)
        {
            return; // Stop execution if step failed
        }

        var connections = workflow.Definition.Connections
            .Where(c => c.SourceStepId == step.Id)
            .ToList();

        foreach (var connection in connections)
        {
            var nextStep = workflow.Definition.Steps.FirstOrDefault(s => s.Id == connection.TargetStepId);
            if (nextStep != null)
            {
                var nextInputData = stepExecution?.OutputData ?? inputData;
                await ExecuteStepRecursiveAsync(execution, workflow, nextStep, nextInputData, new HashSet<string>(visitedSteps));
            }
        }
    }

    private async Task<Dictionary<string, object>> ExecuteStepLogicAsync(WorkflowStep step, Dictionary<string, object> inputData)
    {
        return step.Type switch
        {
            WorkflowStepType.HttpRequest => await _integrationService.ExecuteHttpRequestAsync(step.Configuration, inputData),
            WorkflowStepType.EmailSend => await _integrationService.SendEmailAsync(step.Configuration, inputData),
            WorkflowStepType.DatabaseQuery => await _integrationService.ExecuteDatabaseQueryAsync(step.Configuration, inputData),
            WorkflowStepType.ScriptExecution => await _integrationService.ExecuteScriptAsync(step.Configuration, inputData),
            WorkflowStepType.Action => ExecuteActionStep(step, inputData),
            WorkflowStepType.Condition => ExecuteConditionStep(step, inputData),
            WorkflowStepType.Wait => await ExecuteWaitStep(step, inputData),
            _ => inputData
        };
    }

    private Dictionary<string, object> ExecuteActionStep(WorkflowStep step, Dictionary<string, object> inputData)
    {
        var outputData = new Dictionary<string, object>(inputData);
        
        if (step.Configuration.ContainsKey("transformations"))
        {
        }

        return outputData;
    }

    private Dictionary<string, object> ExecuteConditionStep(WorkflowStep step, Dictionary<string, object> inputData)
    {
        var outputData = new Dictionary<string, object>(inputData);
        
        if (step.Condition != null && !string.IsNullOrEmpty(step.Condition.Expression))
        {
            outputData["conditionResult"] = true;
        }

        return outputData;
    }

    private async Task<Dictionary<string, object>> ExecuteWaitStep(WorkflowStep step, Dictionary<string, object> inputData)
    {
        if (step.Configuration.ContainsKey("waitTimeSeconds"))
        {
            var waitTime = Convert.ToInt32(step.Configuration["waitTimeSeconds"]);
            await Task.Delay(TimeSpan.FromSeconds(waitTime));
        }
        return inputData;
    }
}
