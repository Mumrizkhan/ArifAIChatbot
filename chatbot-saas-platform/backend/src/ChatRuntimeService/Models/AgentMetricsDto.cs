namespace ChatRuntimeService.Models;

public class AgentMetricsDto
{
    public int TotalAgents { get; set; }
    public int ActiveAgents { get; set; }
    public double AverageResponseTime { get; set; }
    public double AverageRating { get; set; }
}