using LiveAgentService.Models;

namespace LiveAgentService.Models
{
    public class AgentRatingsSummary
    {
        public Guid AgentId { get; set; }
        public int TotalRatings { get; set; }
        public double AverageRating { get; set; }
        public Dictionary<int, int> RatingDistribution { get; set; }
        public DateRange Period { get; set; }
        public DateTime LastUpdated { get; set; }
    }
}