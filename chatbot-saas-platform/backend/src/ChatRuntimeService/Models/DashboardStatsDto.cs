namespace ChatRuntimeService.Models;

public class DashboardStatsDto
{
    public int TotalUsers { get; set; }
    public int TotalConversations { get; set; }
    public int TotalMessages { get; set; }
    public double AverageResponseTime { get; set; }
}