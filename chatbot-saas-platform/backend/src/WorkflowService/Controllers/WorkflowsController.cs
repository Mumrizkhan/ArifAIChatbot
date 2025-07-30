using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Application.Common.Interfaces;
using WorkflowService.Services;
using WorkflowService.Models;
using Shared.Domain.Entities;

namespace WorkflowService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class WorkflowsController : ControllerBase
{
    private readonly IWorkflowService _workflowService;
    private readonly IWorkflowExecutionService _executionService;
    private readonly IWorkflowDesignerService _designerService;
    private readonly IWorkflowTemplateService _templateService;
    private readonly ITenantService _tenantService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<WorkflowsController> _logger;

    public WorkflowsController(
        IWorkflowService workflowService,
        IWorkflowExecutionService executionService,
        IWorkflowDesignerService designerService,
        IWorkflowTemplateService templateService,
        ITenantService tenantService,
        ICurrentUserService currentUserService,
        ILogger<WorkflowsController> logger)
    {
        _workflowService = workflowService;
        _executionService = executionService;
        _designerService = designerService;
        _templateService = templateService;
        _tenantService = tenantService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> CreateWorkflow([FromBody] CreateWorkflowRequest request)
    {
        try
        {
            var tenantId = _tenantService.GetCurrentTenantId();
            var userId = _currentUserService.UserId;

            if (!userId.HasValue)
            {
                return Unauthorized();
            }

            var workflow = await _workflowService.CreateWorkflowAsync(request, tenantId, userId.Value);

            return Ok(new
            {
                workflow.Id,
                workflow.Name,
                workflow.Description,
                Status = workflow.Status.ToString(),
                Message = "Workflow created successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating workflow");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetWorkflows([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        try
        {
            var tenantId = _tenantService.GetCurrentTenantId();
            var workflows = await _workflowService.GetWorkflowsAsync(tenantId, page, pageSize);

            return Ok(workflows.Select(w => new
            {
                w.Id,
                w.Name,
                w.Description,
                Status = w.Status.ToString(),
                w.Version,
                w.IsActive,
                w.Tags,
                w.ExecutionCount,
                w.LastExecutedAt,
                w.CreatedAt,
                w.UpdatedAt
            }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting workflows");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("{workflowId}")]
    public async Task<IActionResult> GetWorkflow(Guid workflowId)
    {
        try
        {
            var tenantId = _tenantService.GetCurrentTenantId();
            var workflow = await _workflowService.GetWorkflowAsync(workflowId, tenantId);

            if (workflow == null)
            {
                return NotFound(new { message = "Workflow not found" });
            }

            return Ok(new
            {
                workflow.Id,
                workflow.Name,
                workflow.Description,
                Status = workflow.Status.ToString(),
                workflow.Version,
                workflow.IsActive,
                workflow.Definition,
                workflow.Variables,
                workflow.Tags,
                workflow.Trigger,
                workflow.Settings,
                workflow.ExecutionCount,
                workflow.LastExecutedAt,
                workflow.TemplateId,
                workflow.CreatedAt,
                workflow.UpdatedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting workflow {WorkflowId}", workflowId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPut("{workflowId}")]
    public async Task<IActionResult> UpdateWorkflow(Guid workflowId, [FromBody] Workflow workflow)
    {
        try
        {
            var tenantId = _tenantService.GetCurrentTenantId();
            workflow.Id = workflowId;
            workflow.TenantId = tenantId;

            var updated = await _workflowService.UpdateWorkflowAsync(workflow);

            if (!updated)
            {
                return NotFound(new { message = "Workflow not found" });
            }

            return Ok(new { message = "Workflow updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating workflow {WorkflowId}", workflowId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpDelete("{workflowId}")]
    public async Task<IActionResult> DeleteWorkflow(Guid workflowId)
    {
        try
        {
            var tenantId = _tenantService.GetCurrentTenantId();
            var deleted = await _workflowService.DeleteWorkflowAsync(workflowId, tenantId);

            if (!deleted)
            {
                return NotFound(new { message = "Workflow not found" });
            }

            return Ok(new { message = "Workflow deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting workflow {WorkflowId}", workflowId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost("{workflowId}/activate")]
    public async Task<IActionResult> ActivateWorkflow(Guid workflowId)
    {
        try
        {
            var tenantId = _tenantService.GetCurrentTenantId();
            var activated = await _workflowService.ActivateWorkflowAsync(workflowId, tenantId);

            if (!activated)
            {
                return NotFound(new { message = "Workflow not found or cannot be activated" });
            }

            return Ok(new { message = "Workflow activated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error activating workflow {WorkflowId}", workflowId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost("{workflowId}/deactivate")]
    public async Task<IActionResult> DeactivateWorkflow(Guid workflowId)
    {
        try
        {
            var tenantId = _tenantService.GetCurrentTenantId();
            var deactivated = await _workflowService.DeactivateWorkflowAsync(workflowId, tenantId);

            if (!deactivated)
            {
                return NotFound(new { message = "Workflow not found" });
            }

            return Ok(new { message = "Workflow deactivated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating workflow {WorkflowId}", workflowId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost("{workflowId}/clone")]
    public async Task<IActionResult> CloneWorkflow(Guid workflowId, [FromBody] CloneWorkflowRequest request)
    {
        try
        {
            var tenantId = _tenantService.GetCurrentTenantId();
            var clonedWorkflow = await _workflowService.CloneWorkflowAsync(workflowId, tenantId, request.NewName);

            return Ok(new
            {
                clonedWorkflow.Id,
                clonedWorkflow.Name,
                Message = "Workflow cloned successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cloning workflow {WorkflowId}", workflowId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost("{workflowId}/execute")]
    public async Task<IActionResult> ExecuteWorkflow(Guid workflowId, [FromBody] ExecuteWorkflowRequest request)
    {
        try
        {
            var tenantId = _tenantService.GetCurrentTenantId();
            var userId = _currentUserService.UserId;

            var executionId = await _executionService.ExecuteWorkflowAsync(workflowId, request, tenantId, userId);

            return Ok(new
            {
                ExecutionId = executionId,
                Message = "Workflow execution started"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing workflow {WorkflowId}", workflowId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("{workflowId}/executions")]
    public async Task<IActionResult> GetWorkflowExecutions(Guid workflowId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        try
        {
            var tenantId = _tenantService.GetCurrentTenantId();
            var executions = await _executionService.GetExecutionsAsync(workflowId, tenantId, page, pageSize);

            return Ok(executions.Select(e => new
            {
                e.Id,
                e.WorkflowId,
                Status = e.Status.ToString(),
                e.StartedAt,
                e.CompletedAt,
                e.Duration,
                e.TriggerSource,
                StepCount = e.StepExecutions.Count,
                CompletedSteps = e.StepExecutions.Count(s => s.Status == WorkflowStepStatus.Completed),
                FailedSteps = e.StepExecutions.Count(s => s.Status == WorkflowStepStatus.Failed)
            }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting workflow executions for workflow {WorkflowId}", workflowId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("executions/{executionId}")]
    public async Task<IActionResult> GetWorkflowExecution(Guid executionId)
    {
        try
        {
            var tenantId = _tenantService.GetCurrentTenantId();
            var execution = await _executionService.GetExecutionAsync(executionId, tenantId);

            if (execution == null)
            {
                return NotFound(new { message = "Workflow execution not found" });
            }

            return Ok(new
            {
                execution.Id,
                execution.WorkflowId,
                Status = execution.Status.ToString(),
                execution.InputData,
                execution.OutputData,
                execution.StartedAt,
                execution.CompletedAt,
                execution.Duration,
                execution.ErrorMessage,
                execution.TriggerSource,
                StepExecutions = execution.StepExecutions.Select(s => new
                {
                    s.Id,
                    s.StepId,
                    s.StepName,
                    StepType = s.StepType.ToString(),
                    Status = s.Status.ToString(),
                    s.InputData,
                    s.OutputData,
                    s.StartedAt,
                    s.CompletedAt,
                    s.Duration,
                    s.ErrorMessage,
                    s.RetryCount
                }).ToList()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting workflow execution {ExecutionId}", executionId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost("executions/{executionId}/cancel")]
    public async Task<IActionResult> CancelExecution(Guid executionId)
    {
        try
        {
            var tenantId = _tenantService.GetCurrentTenantId();
            var cancelled = await _executionService.CancelExecutionAsync(executionId, tenantId);

            if (!cancelled)
            {
                return NotFound(new { message = "Workflow execution not found or cannot be cancelled" });
            }

            return Ok(new { message = "Workflow execution cancelled successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling workflow execution {ExecutionId}", executionId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost("executions/{executionId}/retry")]
    public async Task<IActionResult> RetryExecution(Guid executionId)
    {
        try
        {
            var tenantId = _tenantService.GetCurrentTenantId();
            var retried = await _executionService.RetryExecutionAsync(executionId, tenantId);

            if (!retried)
            {
                return NotFound(new { message = "Workflow execution not found or cannot be retried" });
            }

            return Ok(new { message = "Workflow execution retry initiated" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrying workflow execution {ExecutionId}", executionId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("statistics")]
    public async Task<IActionResult> GetStatistics([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
    {
        try
        {
            var tenantId = _tenantService.GetCurrentTenantId();
            var statistics = await _workflowService.GetStatisticsAsync(tenantId, startDate, endDate);

            return Ok(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting workflow statistics");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("designer/step-types")]
    public async Task<IActionResult> GetAvailableStepTypes()
    {
        try
        {
            var stepTypes = await _designerService.GetAvailableStepTypesAsync();
            return Ok(stepTypes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available step types");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost("designer/validate")]
    public async Task<IActionResult> ValidateWorkflowDefinition([FromBody] WorkflowDefinition definition)
    {
        try
        {
            var validatedDefinition = await _designerService.ValidateWorkflowDefinitionAsync(definition);
            return Ok(new
            {
                IsValid = true,
                Definition = validatedDefinition,
                Message = "Workflow definition is valid"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating workflow definition");
            return BadRequest(new
            {
                IsValid = false,
                Message = ex.Message
            });
        }
    }

    [HttpGet("templates")]
    public async Task<IActionResult> GetTemplates([FromQuery] string? category, [FromQuery] bool publicOnly = true)
    {
        try
        {
            var templates = await _templateService.GetTemplatesAsync(category, publicOnly);
            return Ok(templates);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting workflow templates");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost("templates/{templateId}/create-workflow")]
    public async Task<IActionResult> CreateWorkflowFromTemplate(Guid templateId, [FromBody] CreateFromTemplateRequest request)
    {
        try
        {
            var tenantId = _tenantService.GetCurrentTenantId();
            var userId = _currentUserService.UserId;

            if (!userId.HasValue)
            {
                return Unauthorized();
            }

            var workflow = await _templateService.CreateWorkflowFromTemplateAsync(templateId, request.WorkflowName, tenantId, userId.Value);

            return Ok(new
            {
                workflow.Id,
                workflow.Name,
                Message = "Workflow created from template successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating workflow from template {TemplateId}", templateId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }
}

public class CloneWorkflowRequest
{
    public string NewName { get; set; } = string.Empty;
}

public class CreateFromTemplateRequest
{
    public string WorkflowName { get; set; } = string.Empty;
}
