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
    public int TotalConversations { get; internal set; }
    public int ResolvedConversations { get; internal set; }
    public double CustomerSatisfactionScore { get; internal set; }
    public int TotalMessages { get; internal set; }
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
public class UpdateStatusRequest
{
    public string Status { get; set; } = string.Empty;
}

public class AssignConversationRequest
{
    public Guid ConversationId { get; set; }
    public Guid AgentId { get; set; }
}

public class TransferConversationRequest
{
    public Guid ConversationId { get; set; }
    public Guid ToAgentId { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public class NextInQueueRequest
{
    public string? Department { get; set; }
}

public class EscalateConversationRequest
{
    public Guid ConversationId { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public class UpdateAgentProfileRequest
{
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Bio { get; set; }
    public string? Location { get; set; }
    public string? Timezone { get; set; }
    public string? Language { get; set; }
    public string[]? Skills { get; set; }
    public string[]? Specializations { get; set; }
}



public class AgentDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Avatar { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string[] Skills { get; set; } = Array.Empty<string>();
    public string[] Specializations { get; set; } = Array.Empty<string>();
}

public class AgentProfileDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Avatar { get; set; } = string.Empty;
    public string Bio { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Timezone { get; set; } = "UTC";
    public string Language { get; set; } = "en";
    public string[] Skills { get; set; } = Array.Empty<string>();
    public string[] Specializations { get; set; } = Array.Empty<string>();
    public string Status { get; set; } = string.Empty;
}

public class AgentStatsDto
{
    public Guid AgentId { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public int TotalConversations { get; set; }
    public int ResolvedConversations { get; set; }
    public double ResolutionRate { get; set; }
    public double AverageResponseTimeMinutes { get; set; }
    public double CustomerSatisfactionRating { get; set; }
    public int ActiveConversations { get; set; }
}

//public class AgentPerformanceMetrics
//{
//    public Guid AgentId { get; set; }
//    public DateTime PeriodStart { get; set; }
//    public DateTime PeriodEnd { get; set; }
//    public int TotalConversations { get; set; }
//    public int ResolvedConversations { get; set; }
//    public TimeSpan AverageResponseTime { get; set; }
//    public TimeSpan AverageResolutionTime { get; set; }
//    public double CustomerSatisfactionScore { get; set; }
//    public int TotalMessages { get; set; }
//}

//public class UpdateAgentProfileRequest
//{
//    public string? Name { get; set; }
//    public string? Email { get; set; }
//    public string? Phone { get; set; }
//    public string? Bio { get; set; }
//    public string? Location { get; set; }
//    public string? Timezone { get; set; }
//    public string? Language { get; set; }
//    public string[]? Skills { get; set; }
//    public string[]? Specializations { get; set; }
//}