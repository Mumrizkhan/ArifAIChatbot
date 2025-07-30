using Shared.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Domain.Entities
{
    public class Workflow : AuditableEntity
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public WorkflowStatus Status { get; set; } = WorkflowStatus.Draft;
        public Guid TenantId { get; set; }
        public Guid CreatedByUserId { get; set; }
        public string Version { get; set; } = "1.0";
        public bool IsActive { get; set; } = true;
        public WorkflowDefinition Definition { get; set; } = new();
        public Dictionary<string, object> Variables { get; set; } = new();
        public List<string> Tags { get; set; } = new();
        public WorkflowTrigger Trigger { get; set; } = new();
        public WorkflowSettings Settings { get; set; } = new();
        public DateTime? LastExecutedAt { get; set; }
        public int ExecutionCount { get; set; }
        public string? TemplateId { get; set; }
    }

    public class WorkflowExecution : AuditableEntity
    {
        public Guid Id { get; set; }
        public Guid WorkflowId { get; set; }
        public Guid TenantId { get; set; }
        public Guid? TriggeredByUserId { get; set; }
        public WorkflowExecutionStatus Status { get; set; } = WorkflowExecutionStatus.Running;
        public Dictionary<string, object> InputData { get; set; } = new();
        public Dictionary<string, object> OutputData { get; set; } = new();
        public List<WorkflowStepExecution> StepExecutions { get; set; } = new();
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string? ErrorMessage { get; set; }
        public TimeSpan? Duration { get; set; }
        public string? TriggerSource { get; set; }
    }

    public class WorkflowStepExecution : AuditableEntity
    {
        public Guid Id { get; set; }
        public Guid WorkflowExecutionId { get; set; }
        public string StepId { get; set; } = string.Empty;
        public string StepName { get; set; } = string.Empty;
        public WorkflowStepType StepType { get; set; }
        public WorkflowStepStatus Status { get; set; } = WorkflowStepStatus.Pending;
        public Dictionary<string, object> InputData { get; set; } = new();
        public Dictionary<string, object> OutputData { get; set; } = new();
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string? ErrorMessage { get; set; }
        public TimeSpan? Duration { get; set; }
        public int RetryCount { get; set; }
    }

    public class WorkflowTemplate : AuditableEntity
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public WorkflowDefinition Definition { get; set; } = new();
        public Dictionary<string, object> DefaultVariables { get; set; } = new();
        public List<string> Tags { get; set; } = new();
        public bool IsPublic { get; set; }
        public Guid? TenantId { get; set; }
        public int UsageCount { get; set; }
        public double Rating { get; set; }
    }

    public class WorkflowDefinition
    {
        public Guid Id { get; set; }
        public List<WorkflowStep> Steps { get; set; } = new();
        public List<WorkflowConnection> Connections { get; set; } = new();
        public WorkflowLayout Layout { get; set; } = new();
    }

    public class WorkflowStep
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public WorkflowStepType Type { get; set; }
        public Dictionary<string, object> Configuration { get; set; } = new();
        public WorkflowStepPosition Position { get; set; } = new();
        public List<string> InputPorts { get; set; } = new();
        public List<string> OutputPorts { get; set; } = new();
        public bool IsStartStep { get; set; }
        public bool IsEndStep { get; set; }
        public WorkflowStepCondition? Condition { get; set; }
    }

    public class WorkflowConnection
    {
        public string Id { get; set; } = string.Empty;
        public string SourceStepId { get; set; } = string.Empty;
        public string SourcePort { get; set; } = string.Empty;
        public string TargetStepId { get; set; } = string.Empty;
        public string TargetPort { get; set; } = string.Empty;
        public WorkflowConnectionCondition? Condition { get; set; }
    }

    public class WorkflowStepPosition
    {
        public double X { get; set; }
        public double Y { get; set; }
    }

    public class WorkflowLayout
    {
        public double Width { get; set; } = 1200;
        public double Height { get; set; } = 800;
        public double Zoom { get; set; } = 1.0;
        public WorkflowStepPosition ViewportPosition { get; set; } = new();
    }

    public class WorkflowTrigger
    {
        public WorkflowTriggerType Type { get; set; }
        public Dictionary<string, object> Configuration { get; set; } = new();
        public bool IsEnabled { get; set; } = true;
    }

    public class WorkflowSettings
    {
        public int MaxRetries { get; set; } = 3;
        public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(30);
        public bool EnableLogging { get; set; } = true;
        public bool EnableNotifications { get; set; } = true;
        public WorkflowErrorHandling ErrorHandling { get; set; } = WorkflowErrorHandling.Stop;
        public Dictionary<string, object> CustomSettings { get; set; } = new();
    }

    public class WorkflowStepCondition
    {
        public string Expression { get; set; } = string.Empty;
        public Dictionary<string, object> Variables { get; set; } = new();
    }

    public class WorkflowConnectionCondition
    {
        public string Expression { get; set; } = string.Empty;
        public Dictionary<string, object> Variables { get; set; } = new();
    }

    public enum WorkflowStatus
    {
        Draft,
        Active,
        Inactive,
        Archived,
        Error
    }

    public enum WorkflowExecutionStatus
    {
        Pending,
        Running,
        Completed,
        Failed,
        Cancelled,
        Timeout
    }

    public enum WorkflowStepStatus
    {
        Pending,
        Running,
        Completed,
        Failed,
        Skipped,
        Cancelled
    }

    public enum WorkflowStepType
    {
        Start,
        End,
        Action,
        Condition,
        Loop,
        Parallel,
        Wait,
        HttpRequest,
        EmailSend,
        DatabaseQuery,
        ScriptExecution,
        UserTask,
        ServiceTask,
        Timer,
        Gateway,
        SubWorkflow
    }

    public enum WorkflowTriggerType
    {
        Manual,
        Scheduled,
        Event,
        Webhook,
        FileUpload,
        DatabaseChange,
        MessageReceived
    }

    public enum WorkflowErrorHandling
    {
        Stop,
        Continue,
        Retry,
        Escalate
    }
}
