using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Persistence;

namespace AnalyticsService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AnalyticsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AnalyticsController> _logger;

    public AnalyticsController(ApplicationDbContext context, ILogger<AnalyticsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet("conversations/summary")]
    public async Task<IActionResult> GetConversationsSummary()
    {
        var totalConversations = await _context.Conversations.CountAsync();
        var activeConversations = await _context.Conversations
            .CountAsync(c => c.Status == Shared.Domain.Enums.ConversationStatus.Active);
        var resolvedConversations = await _context.Conversations
            .CountAsync(c => c.Status == Shared.Domain.Enums.ConversationStatus.Resolved);

        return Ok(new
        {
            TotalConversations = totalConversations,
            ActiveConversations = activeConversations,
            ResolvedConversations = resolvedConversations
        });
    }

    [HttpGet("messages/summary")]
    public async Task<IActionResult> GetMessagesSummary()
    {
        var totalMessages = await _context.Messages.CountAsync();
        var todayMessages = await _context.Messages
            .CountAsync(m => m.CreatedAt.Date == DateTime.UtcNow.Date);

        return Ok(new
        {
            TotalMessages = totalMessages,
            TodayMessages = todayMessages
        });
    }

    [HttpGet("tenants/summary")]
    public async Task<IActionResult> GetTenantsSummary()
    {
        var totalTenants = await _context.Tenants.CountAsync();
        var activeTenants = await _context.Tenants
            .CountAsync(t => t.Status == Shared.Domain.Enums.TenantStatus.Active);

        return Ok(new
        {
            TotalTenants = totalTenants,
            ActiveTenants = activeTenants
        });
    }

    [HttpGet("realtime")]
    public async Task<IActionResult> GetRealtimeAnalytics()
    {
        try
        {
            var now = DateTime.UtcNow;
            var hourAgo = now.AddHours(-1);

            var activeConversations = await _context.Conversations
                .CountAsync(c => c.Status == Shared.Domain.Enums.ConversationStatus.Active);

            var recentMessages = await _context.Messages
                .CountAsync(m => m.CreatedAt >= hourAgo);

            var onlineAgents = await _context.UserTenants
                .Include(ut => ut.User)
                .CountAsync(ut => ut.User.LastLoginAt >= hourAgo &&
                                 (ut.Role == Shared.Domain.Enums.TenantRole.Agent || ut.Role == Shared.Domain.Enums.TenantRole.Admin));

            return Ok(new
            {
                ActiveConversations = activeConversations,
                RecentMessages = recentMessages,
                OnlineAgents = onlineAgents,
                Timestamp = now
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting realtime analytics");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost]
    public async Task<IActionResult> GetAnalytics([FromBody] AnalyticsRequest request)
    {
        try
        {
            var conversations = await _context.Conversations
                .Where(c => c.CreatedAt >= request.StartDate &&
                           c.CreatedAt <= request.EndDate)
                .Include(c => c.Messages)
                .ToListAsync();

            var totalConversations = conversations.Count;
            var resolvedConversations = conversations.Count(c => c.Status == Shared.Domain.Enums.ConversationStatus.Resolved);
            var averageMessages = conversations.Any() ? conversations.Average(c => c.Messages.Count) : 0;
            var averageRating = conversations
                .Where(c => c.CustomerSatisfactionRating.HasValue)
                .Average(c => c.CustomerSatisfactionRating) ?? 0;

            return Ok(new
            {
                TotalConversations = totalConversations,
                ResolvedConversations = resolvedConversations,
                ResolutionRate = totalConversations > 0 ? (double)resolvedConversations / totalConversations : 0,
                AverageMessagesPerConversation = averageMessages,
                AverageCustomerSatisfaction = averageRating,
                PeriodStart = request.StartDate,
                PeriodEnd = request.EndDate
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting analytics");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost("export")]
    public async Task<IActionResult> ExportAnalytics([FromBody] ExportAnalyticsRequest request)
    {
        try
        {
            var conversations = await _context.Conversations
                .Where(c => c.CreatedAt >= request.StartDate &&
                           c.CreatedAt <= request.EndDate)
                .Include(c => c.Messages)
                .ToListAsync();

            var csvContent = "Date,Conversations,Messages,Resolution Rate\n";
            var groupedByDate = conversations
                .GroupBy(c => c.CreatedAt.Date)
                .OrderBy(g => g.Key);

            foreach (var group in groupedByDate)
            {
                var date = group.Key.ToString("yyyy-MM-dd");
                var convCount = group.Count();
                var msgCount = group.Sum(c => c.Messages.Count);
                var resolved = group.Count(c => c.Status == Shared.Domain.Enums.ConversationStatus.Resolved);
                var resolutionRate = convCount > 0 ? (double)resolved / convCount : 0;

                csvContent += $"{date},{convCount},{msgCount},{resolutionRate:P}\n";
            }

            var bytes = System.Text.Encoding.UTF8.GetBytes(csvContent);
            return File(bytes, "text/csv", $"analytics-{DateTime.UtcNow:yyyyMMdd}.csv");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting analytics");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }
}

public class AnalyticsRequest
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}

public class ExportAnalyticsRequest
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Format { get; set; } = "csv";
}
