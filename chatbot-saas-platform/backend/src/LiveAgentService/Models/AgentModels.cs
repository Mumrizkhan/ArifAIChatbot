using Shared.Domain.Enums;

namespace LiveAgentService.Models;

public enum AgentStatus
{
    Online,
    Away,
    Busy,
    Offline
}

public enum QueuePriority
{
    Low = 1,
    Normal = 2,
    High = 3,
    Urgent = 4
}

public class AgentWorkload
{
    public Guid AgentId { get; set; }
    public string AgentName { get; set; } = string.Empty;
    public string AgentEmail { get; set; } = string.Empty;
    public AgentStatus Status { get; set; }
    public int ActiveConversations { get; set; }
    public int MaxConversations { get; set; } = 5;
    public double AverageResponseTime { get; set; }
    public double CustomerSatisfactionRating { get; set; }
    public string? Department { get; set; }
    public List<string> Languages { get; set; } = new();
    public DateTime LastActivity { get; set; }
}

public class ConversationAssignment
{
    public Guid ConversationId { get; set; }
    public Guid AgentId { get; set; }
    public string? CustomerName { get; set; }
    public string? Subject { get; set; }
    public ConversationStatus Status { get; set; }
    public DateTime AssignedAt { get; set; }
    public DateTime LastMessageAt { get; set; }
    public int UnreadMessages { get; set; }
    public QueuePriority Priority { get; set; }
}

public class QueuedConversation
{
    public Guid ConversationId { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerEmail { get; set; }
    public string? Subject { get; set; }
    public QueuePriority Priority { get; set; }
    public DateTime QueuedAt { get; set; }
    public TimeSpan WaitTime { get; set; }
    public int Position { get; set; }
    public string? Department { get; set; }
    public string Language { get; set; } = "en";
    public ConversationChannel Channel { get; set; }
}

public class QueueStatistics
{
    public int TotalQueued { get; set; }
    public int HighPriorityQueued { get; set; }
    public TimeSpan AverageWaitTime { get; set; }
    public TimeSpan LongestWaitTime { get; set; }
    public int AvailableAgents { get; set; }
    public int BusyAgents { get; set; }
    public double ServiceLevel { get; set; } // Percentage of conversations answered within target time
}

public class AgentPerformanceMetrics
{
    public Guid AgentId { get; set; }
    public string AgentName { get; set; } = string.Empty;
    public int ConversationsHandled { get; set; }
    public TimeSpan AverageResponseTime { get; set; }
    public TimeSpan AverageResolutionTime { get; set; }
    public double CustomerSatisfactionRating { get; set; }
    public int EscalationsReceived { get; set; }
    public int EscalationsMade { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
}

public class ShiftSchedule
{
    public Guid AgentId { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public string TimeZone { get; set; } = "UTC";
    public bool IsActive { get; set; } = true;
}
