using Microsoft.EntityFrameworkCore;
using Shared.Application.Common.Interfaces;
using Shared.Domain.Entities;
using LiveAgentService.Models;
using LiveAgentService.Services;

namespace LiveAgentService.Services;

public class ConversationRatingService : IConversationRatingService
{
    private readonly IApplicationDbContext _context;
    private readonly IChatRuntimeIntegrationService _chatRuntimeIntegrationService;
    private readonly IAgentRoutingService _agentRoutingService;
    private readonly ITenantService _tenantService;
    private readonly ILogger<ConversationRatingService> _logger;
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public ConversationRatingService(
        IApplicationDbContext context,
        IChatRuntimeIntegrationService chatRuntimeIntegrationService,
        IAgentRoutingService agentRoutingService,
        ITenantService tenantService,
        ILogger<ConversationRatingService> logger,
        HttpClient httpClient,
        IConfiguration configuration)
    {
        _context = context;
        _chatRuntimeIntegrationService = chatRuntimeIntegrationService;
        _agentRoutingService = agentRoutingService;
        _tenantService = tenantService;
        _logger = logger;
        _httpClient = httpClient;
        _configuration = configuration;
    }

    public async Task<bool> RateConversationAsync(Guid conversationId, RateConversationRequest request, Guid agentId)
    {
        try
        {
            // Validate the rating request
            if (!await ValidateRatingAsync(conversationId, agentId))
            {
                _logger.LogWarning("Invalid rating attempt for conversation {ConversationId} by agent {AgentId}", conversationId, agentId);
                return false;
            }

            // Validate rating range
            if (request.Rating < 1 || request.Rating > 5)
            {
                _logger.LogWarning("Invalid rating value {Rating} for conversation {ConversationId}", request.Rating, conversationId);
                return false;
            }

            // Submit rating to ChatRuntimeService
            var success = await SubmitRatingToChatServiceAsync(conversationId, request);
            
            if (success)
            {
                // Create a local rating record for tracking
                await CreateLocalRatingRecordAsync(conversationId, request, agentId);
                
                _logger.LogInformation("Successfully rated conversation {ConversationId} with rating {Rating} by agent {AgentId}", 
                    conversationId, request.Rating, agentId);
                
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rating conversation {ConversationId}", conversationId);
            return false;
        }
    }

    public async Task<ConversationRatingResult?> GetConversationRatingAsync(Guid conversationId)
    {
        try
        {
            var tenantId = _tenantService.GetCurrentTenantId();
            
            // Get conversation details from ChatRuntimeService
            var conversationDetails = await _chatRuntimeIntegrationService.GetConversationDetailsAsync(conversationId);
            
            if (conversationDetails == null)
            {
                return null;
            }

            // Check if conversation has a rating
            var conversation = await _context.Conversations
                .FirstOrDefaultAsync(c => c.Id == conversationId && c.TenantId == tenantId);

            if (conversation?.CustomerSatisfactionRating == null)
            {
                return null;
            }

            return new ConversationRatingResult
            {
                ConversationId = conversationId,
                AgentId = conversation.AssignedAgentId,
                Rating = conversation.CustomerSatisfactionRating.Value,
                Feedback = conversation.CustomerFeedback,
                RatedAt = conversation.UpdatedAt ?? conversation.CreatedAt,
                CustomerName = conversationDetails.CustomerName,
                Subject = conversationDetails.Subject
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting rating for conversation {ConversationId}", conversationId);
            return null;
        }
    }

    public async Task<List<ConversationRatingResult>> GetAgentRatingsAsync(Guid agentId, DateTime? startDate = null, DateTime? endDate = null)
    {
        try
        {
            var tenantId = _tenantService.GetCurrentTenantId();
            var start = startDate ?? DateTime.UtcNow.AddDays(-30);
            var end = endDate ?? DateTime.UtcNow;

            var ratings = await _context.Conversations
                .Where(c => c.TenantId == tenantId && 
                           c.AssignedAgentId == agentId &&
                           c.CustomerSatisfactionRating != null &&
                           c.UpdatedAt >= start && c.UpdatedAt <= end)
                .Select(c => new ConversationRatingResult
                {
                    ConversationId = c.Id,
                    AgentId = c.AssignedAgentId,
                    Rating = c.CustomerSatisfactionRating!.Value,
                    Feedback = c.CustomerFeedback,
                    RatedAt = c.UpdatedAt ?? c.CreatedAt,
                    CustomerName = c.CustomerName,
                    Subject = c.Subject
                })
                .OrderByDescending(r => r.RatedAt)
                .ToListAsync();

            return ratings;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting ratings for agent {AgentId}", agentId);
            return new List<ConversationRatingResult>();
        }
    }

    public async Task<ConversationRatingStatistics> GetRatingStatisticsAsync(Guid tenantId, DateTime? startDate = null, DateTime? endDate = null)
    {
        try
        {
            var start = startDate ?? DateTime.UtcNow.AddDays(-30);
            var end = endDate ?? DateTime.UtcNow;

            var ratings = await _context.Conversations
                .Where(c => c.TenantId == tenantId && 
                           c.CustomerSatisfactionRating != null &&
                           c.UpdatedAt >= start && c.UpdatedAt <= end)
                .Select(c => c.CustomerSatisfactionRating!.Value)
                .ToListAsync();

            if (!ratings.Any())
            {
                return new ConversationRatingStatistics
                {
                    PeriodStart = start,
                    PeriodEnd = end
                };
            }

            var averageRating = ratings.Average();
            var ratingDistribution = ratings.GroupBy(r => r).ToDictionary(g => g.Key, g => g.Count());

            return new ConversationRatingStatistics
            {
                AverageRating = Math.Round(averageRating, 2),
                TotalRatings = ratings.Count,
                RatingDistribution = ratingDistribution,
                FiveStarCount = ratingDistribution.GetValueOrDefault(5, 0),
                FourStarCount = ratingDistribution.GetValueOrDefault(4, 0),
                ThreeStarCount = ratingDistribution.GetValueOrDefault(3, 0),
                TwoStarCount = ratingDistribution.GetValueOrDefault(2, 0),
                OneStarCount = ratingDistribution.GetValueOrDefault(1, 0),
                PeriodStart = start,
                PeriodEnd = end
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting rating statistics for tenant {TenantId}", tenantId);
            return new ConversationRatingStatistics
            {
                PeriodStart = startDate,
                PeriodEnd = endDate
            };
        }
    }

    public async Task<bool> ValidateRatingAsync(Guid conversationId, Guid agentId)
    {
        try
        {
            // Check if agent has access to the conversation through the routing service
            var agentConversations = await _agentRoutingService.GetAgentConversationsAsync(agentId);
            var hasAccess = agentConversations.Any(ac => ac.ConversationId == conversationId);

            if (!hasAccess)
            {
                _logger.LogWarning("Agent {AgentId} is not assigned to conversation {ConversationId}", agentId, conversationId);
                return false;
            }

            // Get conversation details to check status
            var conversationDetails = await _chatRuntimeIntegrationService.GetConversationDetailsAsync(conversationId);
            
            if (conversationDetails == null)
            {
                _logger.LogWarning("Conversation {ConversationId} not found", conversationId);
                return false;
            }

            // Check if conversation is in a state that can be rated (closed or resolved)
            var validStatuses = new[] { "Closed", "Resolved" };
            if (!validStatuses.Contains(conversationDetails.Status))
            {
                _logger.LogWarning("Conversation {ConversationId} status {Status} is not valid for rating", 
                    conversationId, conversationDetails.Status);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating rating for conversation {ConversationId}", conversationId);
            return false;
        }
    }

    private async Task<bool> SubmitRatingToChatServiceAsync(Guid conversationId, RateConversationRequest request)
    {
        try
        {
            var chatRuntimeBaseUrl = _configuration["Services:ChatRuntime"] ?? "http://localhost:5002";
            var endpoint = $"{chatRuntimeBaseUrl}/api/conversations/{conversationId}/rating";

            var submitRatingRequest = new
            {
                Rating = request.Rating,
                Feedback = request.Feedback,
                ConversationId = conversationId.ToString(),
                SubmittedAt = DateTime.UtcNow.ToString("O")
            };

            var response = await _httpClient.PostAsJsonAsync(endpoint, submitRatingRequest);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Successfully submitted rating to ChatRuntimeService for conversation {ConversationId}", conversationId);
                return true;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to submit rating to ChatRuntimeService. Status: {StatusCode}, Content: {Content}", 
                    response.StatusCode, errorContent);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting rating to ChatRuntimeService for conversation {ConversationId}", conversationId);
            return false;
        }
    }

    private async Task CreateLocalRatingRecordAsync(Guid conversationId, RateConversationRequest request, Guid agentId)
    {
        try
        {
            // Update local conversation record if it exists
            var conversation = await _context.Conversations
                .FirstOrDefaultAsync(c => c.Id == conversationId);

            if (conversation != null)
            {
                conversation.CustomerSatisfactionRating = request.Rating;
                conversation.CustomerFeedback = request.Feedback;
                conversation.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating local rating record for conversation {ConversationId}", conversationId);
            // Don't throw - rating was already submitted to ChatRuntimeService
        }
    }
}