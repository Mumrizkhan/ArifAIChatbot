using LiveAgentService.Models;

namespace LiveAgentService.Services;

public interface IConversationRatingService
{
    Task<bool> RateConversationAsync(Guid conversationId, RateConversationRequest request, Guid agentId);
    Task<ConversationRatingResult?> GetConversationRatingAsync(Guid conversationId);
    Task<List<ConversationRatingResult>> GetAgentRatingsAsync(Guid agentId, DateTime? startDate = null, DateTime? endDate = null);
    Task<ConversationRatingStatistics> GetRatingStatisticsAsync(Guid tenantId, DateTime? startDate = null, DateTime? endDate = null);
    Task<bool> ValidateRatingAsync(Guid conversationId, Guid agentId);
}

public class ConversationRatingResult
{
    public Guid ConversationId { get; set; }
    public Guid? AgentId { get; set; }
    public int Rating { get; set; }
    public string? Feedback { get; set; }
    public DateTime RatedAt { get; set; }
    public string? CustomerName { get; set; }
    public string? Subject { get; set; }
}

public class ConversationRatingStatistics
{
    public double AverageRating { get; set; }
    public int TotalRatings { get; set; }
    public Dictionary<int, int> RatingDistribution { get; set; } = new();
    public int FiveStarCount { get; set; }
    public int FourStarCount { get; set; }
    public int ThreeStarCount { get; set; }
    public int TwoStarCount { get; set; }
    public int OneStarCount { get; set; }
    public DateTime? PeriodStart { get; set; }
    public DateTime? PeriodEnd { get; set; }
}