namespace ChatRuntimeService.Models;

public class AnalyticsDto
{
    public Dictionary<string, object> Data { get; set; } = new();
    public string TimeRange { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; }
}