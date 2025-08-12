namespace ChatRuntimeService.Models;

public class ConversationMetricsDto
{
    public int Total { get; set; }
    public int Completed { get; set; }
    public int Active { get; set; }
    public double AverageRating { get; set; }
}