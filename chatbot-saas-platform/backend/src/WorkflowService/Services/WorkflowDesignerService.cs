using WorkflowService.Services;
using WorkflowService.Models;
using Shared.Domain.Entities;

namespace WorkflowService.Services;

public class WorkflowDesignerService : IWorkflowDesignerService
{
    private readonly ILogger<WorkflowDesignerService> _logger;

    public WorkflowDesignerService(ILogger<WorkflowDesignerService> logger)
    {
        _logger = logger;
    }

    public async Task<WorkflowDefinition> ValidateWorkflowDefinitionAsync(WorkflowDefinition definition)
    {
        try
        {
            var errors = new List<string>();

            var startSteps = definition.Steps.Where(s => s.IsStartStep).ToList();
            if (startSteps.Count == 0)
            {
                errors.Add("Workflow must have at least one start step");
            }
            else if (startSteps.Count > 1)
            {
                errors.Add("Workflow can only have one start step");
            }

            var endSteps = definition.Steps.Where(s => s.IsEndStep).ToList();
            if (endSteps.Count == 0)
            {
                errors.Add("Workflow must have at least one end step");
            }

            var stepIds = definition.Steps.Select(s => s.Id).ToList();
            var duplicateIds = stepIds.GroupBy(id => id).Where(g => g.Count() > 1).Select(g => g.Key);
            if (duplicateIds.Any())
            {
                errors.Add($"Duplicate step IDs found: {string.Join(", ", duplicateIds)}");
            }

            foreach (var connection in definition.Connections)
            {
                var sourceStep = definition.Steps.FirstOrDefault(s => s.Id == connection.SourceStepId);
                var targetStep = definition.Steps.FirstOrDefault(s => s.Id == connection.TargetStepId);

                if (sourceStep == null)
                {
                    errors.Add($"Connection references non-existent source step: {connection.SourceStepId}");
                }

                if (targetStep == null)
                {
                    errors.Add($"Connection references non-existent target step: {connection.TargetStepId}");
                }

                if (sourceStep != null && !sourceStep.OutputPorts.Contains(connection.SourcePort))
                {
                    errors.Add($"Connection references non-existent source port: {connection.SourcePort} on step {connection.SourceStepId}");
                }

                if (targetStep != null && !targetStep.InputPorts.Contains(connection.TargetPort))
                {
                    errors.Add($"Connection references non-existent target port: {connection.TargetPort} on step {connection.TargetStepId}");
                }
            }

            var reachableSteps = new HashSet<string>();
            var startStep = startSteps.FirstOrDefault();
            if (startStep != null)
            {
                TraverseSteps(startStep.Id, definition.Connections, reachableSteps);
            }

            var unreachableSteps = definition.Steps.Where(s => !s.IsStartStep && !reachableSteps.Contains(s.Id)).ToList();
            if (unreachableSteps.Any())
            {
                errors.Add($"Unreachable steps found: {string.Join(", ", unreachableSteps.Select(s => s.Name))}");
            }

            if (errors.Any())
            {
                throw new InvalidOperationException($"Workflow validation failed: {string.Join("; ", errors)}");
            }

            return definition;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating workflow definition");
            throw;
        }
    }

    public async Task<List<WorkflowStep>> GetAvailableStepTypesAsync()
    {
        try
        {
            var stepTypes = new List<WorkflowStep>
            {
                new WorkflowStep
                {
                    Id = "start",
                    Name = "Start",
                    Type = WorkflowStepType.Start,
                    InputPorts = new List<string>(),
                    OutputPorts = new List<string> { "output" },
                    IsStartStep = true
                },
                new WorkflowStep
                {
                    Id = "end",
                    Name = "End",
                    Type = WorkflowStepType.End,
                    InputPorts = new List<string> { "input" },
                    OutputPorts = new List<string>(),
                    IsEndStep = true
                },
                new WorkflowStep
                {
                    Id = "action",
                    Name = "Action",
                    Type = WorkflowStepType.Action,
                    InputPorts = new List<string> { "input" },
                    OutputPorts = new List<string> { "output" }
                },
                new WorkflowStep
                {
                    Id = "condition",
                    Name = "Condition",
                    Type = WorkflowStepType.Condition,
                    InputPorts = new List<string> { "input" },
                    OutputPorts = new List<string> { "true", "false" }
                },
                new WorkflowStep
                {
                    Id = "http-request",
                    Name = "HTTP Request",
                    Type = WorkflowStepType.HttpRequest,
                    InputPorts = new List<string> { "input" },
                    OutputPorts = new List<string> { "success", "error" }
                },
                new WorkflowStep
                {
                    Id = "email-send",
                    Name = "Send Email",
                    Type = WorkflowStepType.EmailSend,
                    InputPorts = new List<string> { "input" },
                    OutputPorts = new List<string> { "sent", "failed" }
                },
                new WorkflowStep
                {
                    Id = "database-query",
                    Name = "Database Query",
                    Type = WorkflowStepType.DatabaseQuery,
                    InputPorts = new List<string> { "input" },
                    OutputPorts = new List<string> { "result", "error" }
                },
                new WorkflowStep
                {
                    Id = "script-execution",
                    Name = "Script Execution",
                    Type = WorkflowStepType.ScriptExecution,
                    InputPorts = new List<string> { "input" },
                    OutputPorts = new List<string> { "output", "error" }
                },
                new WorkflowStep
                {
                    Id = "wait",
                    Name = "Wait",
                    Type = WorkflowStepType.Wait,
                    InputPorts = new List<string> { "input" },
                    OutputPorts = new List<string> { "output" }
                },
                new WorkflowStep
                {
                    Id = "user-task",
                    Name = "User Task",
                    Type = WorkflowStepType.UserTask,
                    InputPorts = new List<string> { "input" },
                    OutputPorts = new List<string> { "completed", "cancelled" }
                }
            };

            return stepTypes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available step types");
            throw;
        }
    }

    public async Task<WorkflowStep> CreateStepAsync(WorkflowStepType stepType, Dictionary<string, object> configuration)
    {
        try
        {
            var availableSteps = await GetAvailableStepTypesAsync();
            var template = availableSteps.FirstOrDefault(s => s.Type == stepType);

            if (template == null)
            {
                throw new ArgumentException($"Unknown step type: {stepType}");
            }

            var step = new WorkflowStep
            {
                Id = Guid.NewGuid().ToString(),
                Name = template.Name,
                Type = stepType,
                Configuration = configuration,
                InputPorts = new List<string>(template.InputPorts),
                OutputPorts = new List<string>(template.OutputPorts),
                IsStartStep = template.IsStartStep,
                IsEndStep = template.IsEndStep,
                Position = new WorkflowStepPosition { X = 0, Y = 0 }
            };

            return step;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating workflow step of type {StepType}", stepType);
            throw;
        }
    }

    public async Task<WorkflowConnection> CreateConnectionAsync(string sourceStepId, string sourcePort, string targetStepId, string targetPort)
    {
        try
        {
            var connection = new WorkflowConnection
            {
                Id = Guid.NewGuid().ToString(),
                SourceStepId = sourceStepId,
                SourcePort = sourcePort,
                TargetStepId = targetStepId,
                TargetPort = targetPort
            };

            return connection;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating workflow connection");
            throw;
        }
    }

    public async Task<bool> ValidateConnectionAsync(WorkflowConnection connection, WorkflowDefinition definition)
    {
        try
        {
            var sourceStep = definition.Steps.FirstOrDefault(s => s.Id == connection.SourceStepId);
            var targetStep = definition.Steps.FirstOrDefault(s => s.Id == connection.TargetStepId);

            if (sourceStep == null || targetStep == null)
                return false;

            if (!sourceStep.OutputPorts.Contains(connection.SourcePort))
                return false;

            if (!targetStep.InputPorts.Contains(connection.TargetPort))
                return false;

            var visited = new HashSet<string>();
            return !HasCircularDependency(connection.TargetStepId, connection.SourceStepId, definition.Connections, visited);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating workflow connection");
            return false;
        }
    }

    public async Task<WorkflowDefinition> OptimizeWorkflowAsync(WorkflowDefinition definition)
    {
        try
        {
            var connectedStepIds = new HashSet<string>();
            foreach (var connection in definition.Connections)
            {
                connectedStepIds.Add(connection.SourceStepId);
                connectedStepIds.Add(connection.TargetStepId);
            }

            var startEndStepIds = definition.Steps.Where(s => s.IsStartStep || s.IsEndStep).Select(s => s.Id);
            foreach (var stepId in startEndStepIds)
            {
                connectedStepIds.Add(stepId);
            }

            definition.Steps = definition.Steps.Where(s => connectedStepIds.Contains(s.Id)).ToList();

            await OptimizeLayoutAsync(definition);

            return definition;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error optimizing workflow");
            throw;
        }
    }

    public async Task<List<string>> GetWorkflowVariablesAsync(WorkflowDefinition definition)
    {
        try
        {
            var variables = new HashSet<string>();

            foreach (var step in definition.Steps)
            {
                ExtractVariablesFromConfiguration(step.Configuration, variables);

                if (step.Condition != null)
                {
                    ExtractVariablesFromConfiguration(step.Condition.Variables, variables);
                }
            }

            foreach (var connection in definition.Connections)
            {
                if (connection.Condition != null)
                {
                    ExtractVariablesFromConfiguration(connection.Condition.Variables, variables);
                }
            }

            return variables.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting workflow variables");
            throw;
        }
    }

    private void TraverseSteps(string stepId, List<WorkflowConnection> connections, HashSet<string> visited)
    {
        if (visited.Contains(stepId))
            return;

        visited.Add(stepId);

        var outgoingConnections = connections.Where(c => c.SourceStepId == stepId);
        foreach (var connection in outgoingConnections)
        {
            TraverseSteps(connection.TargetStepId, connections, visited);
        }
    }

    private bool HasCircularDependency(string startStepId, string targetStepId, List<WorkflowConnection> connections, HashSet<string> visited)
    {
        if (startStepId == targetStepId)
            return true;

        if (visited.Contains(startStepId))
            return false;

        visited.Add(startStepId);

        var outgoingConnections = connections.Where(c => c.SourceStepId == startStepId);
        foreach (var connection in outgoingConnections)
        {
            if (HasCircularDependency(connection.TargetStepId, targetStepId, connections, new HashSet<string>(visited)))
                return true;
        }

        return false;
    }

    private async Task OptimizeLayoutAsync(WorkflowDefinition definition)
    {
        var stepsPerRow = Math.Max(1, (int)Math.Ceiling(Math.Sqrt(definition.Steps.Count)));
        var stepWidth = 200;
        var stepHeight = 100;
        var padding = 50;

        for (int i = 0; i < definition.Steps.Count; i++)
        {
            var row = i / stepsPerRow;
            var col = i % stepsPerRow;

            definition.Steps[i].Position.X = col * (stepWidth + padding) + padding;
            definition.Steps[i].Position.Y = row * (stepHeight + padding) + padding;
        }

        definition.Layout.Width = stepsPerRow * (stepWidth + padding) + padding;
        definition.Layout.Height = ((definition.Steps.Count - 1) / stepsPerRow + 1) * (stepHeight + padding) + padding;
    }

    private void ExtractVariablesFromConfiguration(Dictionary<string, object> configuration, HashSet<string> variables)
    {
        foreach (var kvp in configuration)
        {
            if (kvp.Value is string stringValue)
            {
                var matches = System.Text.RegularExpressions.Regex.Matches(stringValue, @"\$\{([^}]+)\}");
                foreach (System.Text.RegularExpressions.Match match in matches)
                {
                    variables.Add(match.Groups[1].Value);
                }
            }
        }
    }
}
