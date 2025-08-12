namespace ChatRuntimeService.Models;

public class RealtimeAnalyticsDto
{
    public int ActiveUsers { get; set; }
    public int OngoingConversations { get; set; }
    public int AvailableAgents { get; set; }
    public double SystemLoad { get; set; }
}