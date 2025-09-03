using Microsoft.EntityFrameworkCore;
using Shared.Application.Common.Interfaces;
using Shared.Domain.Entities;
using Shared.Domain.Enums;
using LiveAgentService.Models;
using LiveAgentService.Services;
using AnalyticsService.Events;

namespace LiveAgentService.Services;

public class ConversationRatingService : IConversationRatingService
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ITenantService _tenantService;
    private readonly ILiveAgentAnalyticsService _liveAgentAnalytics;
    private readonly ILogger<ConversationRatingService> _logger;

    public ConversationRatingService(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        ITenantService tenantService,
        ILiveAgentAnalyticsService liveAgentAnalytics,
        ILogger<ConversationRatingService> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _tenantService = tenantService;
        _liveAgentAnalytics = liveAgentAnalytics;
        _logger = logger;
    }

    public async Task<ConversationRating> RateConversationAsync(RateConversationRequest request)
    {
        try
        {
            var tenantId = _tenantService.GetCurrentTenantId();
            var conversation = await _context.Conversations
                .Include(c => c.Messages)
                .FirstOrDefaultAsync(c => c.Id == request.ConversationId && c.TenantId == tenantId);

            if (conversation == null)
                throw new InvalidOperationException("Conversation not found");

            // Check if rating already exists
            var existingRating = await _context.ConversationRatings
                .FirstOrDefaultAsync(r => r.ConversationId == request.ConversationId);

            ConversationRating rating;
            
            if (existingRating != null)
            {
                // Update existing rating
                existingRating.Rating = request.Rating;
                existingRating.Feedback = request.Feedback;
                existingRating.Categories = request.Categories;
                existingRating.UpdatedAt = DateTime.UtcNow;
                rating = existingRating;
            }
            else
            {
                // Create new rating
                rating = new ConversationRating
                {
                    Id = Guid.NewGuid(),
                    ConversationId = request.ConversationId,
                    Rating = request.Rating,
                    Feedback = request.Feedback,
                    Categories = request.Categories,
                    RatedBy = request.RatedBy,
                    TenantId = tenantId,
                    CreatedAt = DateTime.UtcNow
                };

                _context.ConversationRatings.Add(rating);
            }

            await _context.SaveChangesAsync();

            // Calculate conversation metrics for analytics
            var conversationDuration = conversation.EndedAt.HasValue ? 
                conversation.EndedAt.Value - conversation.StartedAt : 
                DateTime.UtcNow - conversation.StartedAt;
            
            var messageCount = conversation.Messages?.Count ?? 0;
            var wasResolved = conversation.Status == ConversationStatus.Resolved;

            // Generate session ID for analytics
            var sessionId = Guid.NewGuid().ToString();

            // Determine satisfaction level based on rating
            var satisfactionLevel = request.Rating switch
            {
                5 => SatisfactionLevel.VerySatisfied,
                4 => SatisfactionLevel.Satisfied,
                3 => SatisfactionLevel.Neutral,
                2 => SatisfactionLevel.Dissatisfied,
                _ => SatisfactionLevel.VeryDissatisfied
            };

            // Publish live agent feedback analytics event
            await _liveAgentAnalytics.PublishFeedbackAsync(
                request.ConversationId,
                conversation.AssignedAgentId?.ToString() ?? "unknown",
                conversation.CustomerEmail, // Using email as userId
                tenantId.ToString(),
                sessionId,
                request.Rating,
                satisfactionLevel,
                request.Feedback,
                 string.IsNullOrWhiteSpace(request.Categories) ? null :
        request.Categories.Split(',', StringSplitOptions.RemoveEmptyEntries)
                         .Select(c => c.Trim())
                         .ToList(),
                request.Rating >= 4, // Would recommend if rating is 4 or 5
                null // Improvement suggestions could be extracted from feedback
            );

            _logger.LogInformation("Conversation {ConversationId} rated with {Rating} stars by {RatedBy}", 
                request.ConversationId, request.Rating, request.RatedBy);

            return rating;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rating conversation {ConversationId}", request.ConversationId);
            throw;
        }
    }

    public async Task<ConversationRating?> GetConversationRatingAsync(Guid conversationId)
    {
        try
        {
            var tenantId = _tenantService.GetCurrentTenantId();
            return await _context.ConversationRatings
                .FirstOrDefaultAsync(r => r.ConversationId == conversationId && r.TenantId == tenantId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting rating for conversation {ConversationId}", conversationId);
            throw;
        }
    }

    public async Task<List<ConversationRating>> GetAgentRatingsAsync(Guid agentId, DateTime? from = null, DateTime? to = null)
    {
        try
        {
            var tenantId = _tenantService.GetCurrentTenantId();
            var query = _context.ConversationRatings
                .Include(r => r.Conversation)
                .Where(r => r.Conversation.AssignedAgentId == agentId && r.TenantId == tenantId);

            if (from.HasValue)
                query = query.Where(r => r.CreatedAt >= from.Value);

            if (to.HasValue)
                query = query.Where(r => r.CreatedAt <= to.Value);

            return await query.OrderByDescending(r => r.CreatedAt).ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting ratings for agent {AgentId}", agentId);
            throw;
        }
    }

    public async Task<double> GetAgentAverageRatingAsync(Guid agentId, DateTime? from = null, DateTime? to = null)
    {
        try
        {
            var ratings = await GetAgentRatingsAsync(agentId, from, to);
            return ratings.Any() ? ratings.Average(r => r.Rating) : 0.0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating average rating for agent {AgentId}", agentId);
            return 0.0;
        }
    }

    public async Task<AgentRatingsSummary> GetAgentRatingsSummaryAsync(Guid agentId, DateTime? from = null, DateTime? to = null)
    {
        try
        {
            var ratings = await GetAgentRatingsAsync(agentId, from, to);
            
            return new AgentRatingsSummary
            {
                AgentId = agentId,
                TotalRatings = ratings.Count,
                AverageRating = ratings.Any() ? ratings.Average(r => r.Rating) : 0.0,
                RatingDistribution = ratings.GroupBy(r => r.Rating)
                    .ToDictionary(g => g.Key, g => g.Count()),
                Period = new Models.DateRange 
                { 
                    From = from ?? DateTime.UtcNow.AddDays(-30), 
                    To = to ?? DateTime.UtcNow 
                },
                LastUpdated = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting ratings summary for agent {AgentId}", agentId);
            throw;
        }
    }

    public async Task<bool> DeleteRatingAsync(Guid ratingId)
    {
        try
        {
            var tenantId = _tenantService.GetCurrentTenantId();
            var rating = await _context.ConversationRatings
                .FirstOrDefaultAsync(r => r.Id == ratingId && r.TenantId == tenantId);

            if (rating == null)
                return false;

            _context.ConversationRatings.Remove(rating);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Rating {RatingId} deleted", ratingId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting rating {RatingId}", ratingId);
            return false;
        }
    }

    public async Task<bool> RateConversationAsync(Guid conversationId, RateConversationRequest request, Guid agentId)
    {
        try
        {
            var tenantId = _tenantService.GetCurrentTenantId();
            var conversation = await _context.Conversations
                .FirstOrDefaultAsync(c => c.Id == conversationId && c.TenantId == tenantId);

            if (conversation == null)
                return false;

            if (conversation.AssignedAgentId != agentId)
                return false;

            request.ConversationId = conversationId;
            await RateConversationAsync(request);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rating conversation {ConversationId} by agent {AgentId}", conversationId, agentId);
            return false;
        }
    }

    async Task<ConversationRatingResult?> IConversationRatingService.GetConversationRatingAsync(Guid conversationId)
    {
        try
        {
            var tenantId = _tenantService.GetCurrentTenantId();
            var rating = await _context.ConversationRatings
                .Include(r => r.Conversation)
                .FirstOrDefaultAsync(r => r.ConversationId == conversationId && r.TenantId == tenantId);

            if (rating == null)
                return null;

            return new ConversationRatingResult
            {
                ConversationId = rating.ConversationId,
                AgentId = rating.Conversation?.AssignedAgentId,
                RatedAt = rating.CreatedAt
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting rating result for conversation {ConversationId}", conversationId);
            throw;
        }
    }

    async Task<List<ConversationRatingResult>> IConversationRatingService.GetAgentRatingsAsync(Guid agentId, DateTime? startDate, DateTime? endDate)
    {
        try
        {
            var tenantId = _tenantService.GetCurrentTenantId();
            var query = _context.ConversationRatings
                .Include(r => r.Conversation)
                .Where(r => r.Conversation.AssignedAgentId == agentId && r.TenantId == tenantId);

            if (startDate.HasValue)
                query = query.Where(r => r.CreatedAt >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(r => r.CreatedAt <= endDate.Value);

            var ratings = await query.OrderByDescending(r => r.CreatedAt).ToListAsync();

            return ratings.Select(r => new ConversationRatingResult
            {
                ConversationId = r.ConversationId,
                AgentId = r.Conversation?.AssignedAgentId,
                RatedAt = r.CreatedAt
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting rating results for agent {AgentId}", agentId);
            throw;
        }
    }

    public async Task<ConversationRatingStatistics> GetRatingStatisticsAsync(Guid tenantId, DateTime? startDate = null, DateTime? endDate = null)
    {
        try
        {
            var query = _context.ConversationRatings
                .Where(r => r.TenantId == tenantId);

            if (startDate.HasValue)
                query = query.Where(r => r.CreatedAt >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(r => r.CreatedAt <= endDate.Value);

            var ratings = await query.ToListAsync();

            var ratingDistribution = ratings
                .GroupBy(r => r.Rating)
                .ToDictionary(g => g.Key, g => g.Count());

            return new ConversationRatingStatistics
            {
                AverageRating = ratings.Any() ? ratings.Average(r => r.Rating) : 0.0,
                TotalRatings = ratings.Count,
                RatingDistribution = ratingDistribution,
                PeriodStart = startDate,
                PeriodEnd = endDate
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting rating statistics for tenant {TenantId}", tenantId);
            throw;
        }
    }

    public async Task<bool> ValidateRatingAsync(Guid conversationId, Guid agentId)
    {
        try
        {
            var tenantId = _tenantService.GetCurrentTenantId();
            var conversation = await _context.Conversations
                .FirstOrDefaultAsync(c => c.Id == conversationId && c.TenantId == tenantId);

            return conversation != null && conversation.AssignedAgentId == agentId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating rating for conversation {ConversationId} and agent {AgentId}", conversationId, agentId);
            return false;
        }
    }
}

