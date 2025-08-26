using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Shared.Application.Common.Interfaces;
using Shared.Domain.Entities;
using WorkflowService.Models;

namespace WorkflowService.Hubs;

[Authorize]
public class WorkflowHub : Hub
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ITenantService _tenantService;
    private readonly ILogger<WorkflowHub> _logger;

    public WorkflowHub(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        ITenantService tenantService,
        ILogger<WorkflowHub> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _tenantService = tenantService;
        _logger = logger;
    }

    public async Task JoinTenantGroup()
    {
        try
        {
            var tenantId = _tenantService.GetCurrentTenantId();
            await Groups.AddToGroupAsync(Context.ConnectionId, $"tenant_{tenantId}");
            
            _logger.LogInformation($"User joined workflow group for tenant {tenantId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error joining tenant workflow group");
        }
    }

    public async Task LeaveTenantGroup()
    {
        try
        {
            var tenantId = _tenantService.GetCurrentTenantId();
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"tenant_{tenantId}");
            
            _logger.LogInformation($"User left workflow group for tenant {tenantId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error leaving tenant workflow group");
        }
    }

    public async Task JoinWorkflowExecution(string executionId)
    {
        try
        {
            var tenantId = _tenantService.GetCurrentTenantId();
            
            if (Guid.TryParse(executionId, out var execId))
            {
                var execution = await _context.Set<WorkflowExecution>()
                    .FirstOrDefaultAsync(e => e.Id == execId && e.TenantId == tenantId);

                if (execution != null)
                {
                    await Groups.AddToGroupAsync(Context.ConnectionId, $"execution_{executionId}");
                    
                    await Clients.Caller.SendAsync("WorkflowExecutionStatus", new
                    {
                        ExecutionId = executionId,
                        Status = execution.Status.ToString(),
                        StartedAt = execution.StartedAt,
                        CompletedAt = execution.CompletedAt,
                        Duration = execution.Duration,
                        StepCount = execution.StepExecutions.Count,
                        CompletedSteps = execution.StepExecutions.Count(s => s.Status == WorkflowStepStatus.Completed)
                    });

                    _logger.LogInformation($"User joined workflow execution {executionId}");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error joining workflow execution {ExecutionId}", executionId);
        }
    }

    public async Task LeaveWorkflowExecution(string executionId)
    {
        try
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"execution_{executionId}");
            _logger.LogInformation($"User left workflow execution {executionId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error leaving workflow execution {ExecutionId}", executionId);
        }
    }

    public async Task GetWorkflowExecutionProgress(string executionId)
    {
        try
        {
            var tenantId = _tenantService.GetCurrentTenantId();
            
            if (Guid.TryParse(executionId, out var execId))
            {
                var execution = await _context.Set<WorkflowExecution>()
                    .Include(e => e.StepExecutions)
                    .FirstOrDefaultAsync(e => e.Id == execId && e.TenantId == tenantId);

                if (execution != null)
                {
                    var progress = new
                    {
                        ExecutionId = executionId,
                        Status = execution.Status.ToString(),
                        StartedAt = execution.StartedAt,
                        CompletedAt = execution.CompletedAt,
                        Duration = execution.Duration,
                        TotalSteps = execution.StepExecutions.Count,
                        CompletedSteps = execution.StepExecutions.Count(s => s.Status == WorkflowStepStatus.Completed),
                        FailedSteps = execution.StepExecutions.Count(s => s.Status == WorkflowStepStatus.Failed),
                        RunningSteps = execution.StepExecutions.Count(s => s.Status == WorkflowStepStatus.Running),
                        Steps = execution.StepExecutions.Select(s => new
                        {
                            s.StepId,
                            s.StepName,
                            Status = s.Status.ToString(),
                            s.StartedAt,
                            s.CompletedAt,
                            s.Duration,
                            s.ErrorMessage
                        }).ToList()
                    };

                    await Clients.Caller.SendAsync("WorkflowExecutionProgress", progress);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting workflow execution progress {ExecutionId}", executionId);
        }
    }

    public async Task GetActiveWorkflows()
    {
        try
        {
            var tenantId = _tenantService.GetCurrentTenantId();
            
            var activeWorkflows = await _context.Set<Workflow>()
                .Where(w => w.TenantId == tenantId && w.IsActive)
                .Select(w => new
                {
                    w.Id,
                    w.Name,
                    w.Description,
                    Status = w.Status.ToString(),
                    w.ExecutionCount,
                    w.LastExecutedAt,
                    w.CreatedAt
                })
                .ToListAsync();

            await Clients.Caller.SendAsync("ActiveWorkflows", activeWorkflows);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active workflows");
        }
    }

    public async Task GetRecentExecutions(int count = 10)
    {
        try
        {
            var tenantId = _tenantService.GetCurrentTenantId();
            
            var recentExecutions = await _context.Set<WorkflowExecution>()
                .Where(e => e.TenantId == tenantId)
                .OrderByDescending(e => e.StartedAt)
                .Take(count)
                .Select(e => new
                {
                    e.Id,
                    e.WorkflowId,
                    Status = e.Status.ToString(),
                    e.StartedAt,
                    e.CompletedAt,
                    e.Duration,
                    e.TriggerSource
                })
                .ToListAsync();

            await Clients.Caller.SendAsync("RecentExecutions", recentExecutions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recent workflow executions");
        }
    }

    public async Task ValidateWorkflowDefinition(object workflowDefinition)
    {
        try
        {
            var validationResult = new
            {
                IsValid = true,
                Errors = new List<string>(),
                Warnings = new List<string>()
            };

            await Clients.Caller.SendAsync("WorkflowValidationResult", validationResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating workflow definition");
            
            var validationResult = new
            {
                IsValid = false,
                Errors = new List<string> { ex.Message },
                Warnings = new List<string>()
            };

            await Clients.Caller.SendAsync("WorkflowValidationResult", validationResult);
        }
    }

    public async Task BroadcastWorkflowUpdate(string workflowId, string updateType, object data)
    {
        try
        {
            var tenantId = _tenantService.GetCurrentTenantId();
            var userId = _currentUserService.UserId;

            if (userId.HasValue && Guid.TryParse(workflowId, out var wfId))
            {
                var workflow = await _context.Set<Workflow>()
                    .FirstOrDefaultAsync(w => w.Id == wfId && w.TenantId == tenantId);

                if (workflow != null)
                {
                    await Clients.Group($"tenant_{tenantId}").SendAsync("WorkflowUpdated", new
                    {
                        WorkflowId = workflowId,
                        UpdateType = updateType,
                        Data = data,
                        UpdatedBy = userId,
                        Timestamp = DateTime.UtcNow.ToString("O")
                    });

                    _logger.LogInformation($"Workflow {workflowId} update broadcasted by user {userId}");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting workflow update for workflow {WorkflowId}", workflowId);
        }
    }

    public override async Task OnConnectedAsync()
    {
        var userId = _currentUserService.UserId;
        var tenantId = _tenantService.GetCurrentTenantId();
        
        _logger.LogInformation($"User {userId} connected to workflow hub for tenant {tenantId}");
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = _currentUserService.UserId;
        var tenantId = _tenantService.GetCurrentTenantId();

        _logger.LogInformation($"User {userId} disconnected from workflow hub for tenant {tenantId}");
        await base.OnDisconnectedAsync(exception);
    }
}
