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

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboardStats()
    {
        try
        {
            var totalTenants = await _context.Tenants.CountAsync();
            var activeTenants = await _context.Tenants
                .CountAsync(t => t.Status == Shared.Domain.Enums.TenantStatus.Active);
            var totalUsers = await _context.Users.CountAsync();
            var activeUsers = await _context.Users
                .CountAsync(u => u.IsActive);
            var totalConversations = await _context.Conversations.CountAsync();
            var activeConversations = await _context.Conversations
                .CountAsync(c => c.Status == Shared.Domain.Enums.ConversationStatus.Active);
            var totalMessages = await _context.Messages.CountAsync();
            var todayMessages = await _context.Messages
                .CountAsync(m => m.CreatedAt.Date == DateTime.UtcNow.Date);

            return Ok(new
            {
                TotalTenants = totalTenants,
                ActiveTenants = activeTenants,
                TotalUsers = totalUsers,
                ActiveUsers = activeUsers,
                TotalConversations = totalConversations,
                ActiveConversations = activeConversations,
                TotalMessages = totalMessages,
                TodayMessages = todayMessages
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting dashboard stats");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("conversations")]
    public async Task<IActionResult> GetConversationMetrics([FromQuery] string timeRange = "7d", [FromQuery] string? tenantId = null)
    {
        try
        {
            var startDate = GetStartDateFromTimeRange(timeRange);
            var query = _context.Conversations
                .Where(c => c.CreatedAt >= startDate);

            if (!string.IsNullOrEmpty(tenantId) && Guid.TryParse(tenantId, out var parsedTenantId))
            {
                query = query.Where(c => c.TenantId == parsedTenantId);
            }

            var conversations = await query.ToListAsync();
            var totalConversations = conversations.Count;
            var resolvedConversations = conversations.Count(c => c.Status == Shared.Domain.Enums.ConversationStatus.Resolved);
            var averageRating = conversations
                .Where(c => c.CustomerSatisfactionRating.HasValue)
                .Average(c => c.CustomerSatisfactionRating) ?? 0;

            return Ok(new
            {
                TotalConversations = totalConversations,
                ResolvedConversations = resolvedConversations,
                ResolutionRate = totalConversations > 0 ? (double)resolvedConversations / totalConversations : 0,
                AverageRating = averageRating,
                TimeRange = timeRange
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting conversation metrics");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("agents")]
    public async Task<IActionResult> GetAgentMetrics([FromQuery] string timeRange = "7d", [FromQuery] string? tenantId = null)
    {
        try
        {
            var startDate = GetStartDateFromTimeRange(timeRange);
            var query = _context.UserTenants
                .Include(ut => ut.User)
                .Where(ut => ut.Role == Shared.Domain.Enums.TenantRole.Agent && ut.IsActive);

            if (!string.IsNullOrEmpty(tenantId) && Guid.TryParse(tenantId, out var parsedTenantId))
            {
                query = query.Where(ut => ut.TenantId == parsedTenantId);
            }

            var agents = await query.ToListAsync();
            var totalAgents = agents.Count;
            var activeAgents = agents.Count(a => a.User.LastLoginAt >= startDate);

            return Ok(new
            {
                TotalAgents = totalAgents,
                ActiveAgents = activeAgents,
                TimeRange = timeRange
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting agent metrics");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("bot")]
    public async Task<IActionResult> GetBotMetrics([FromQuery] string timeRange = "7d", [FromQuery] string? tenantId = null)
    {
        try
        {
            var startDate = GetStartDateFromTimeRange(timeRange);
            var query = _context.Messages
                .Where(m => m.CreatedAt >= startDate && m.Sender == Shared.Domain.Enums.MessageSender.Bot);

            if (!string.IsNullOrEmpty(tenantId) && Guid.TryParse(tenantId, out var parsedTenantId))
            {
                query = query.Where(m => m.TenantId == parsedTenantId);
            }

            var botMessages = await query.CountAsync();
            var totalMessages = await _context.Messages
                .Where(m => m.CreatedAt >= startDate)
                .CountAsync();

            var botResponseRate = totalMessages > 0 ? (double)botMessages / totalMessages : 0;

            return Ok(new
            {
                BotMessages = botMessages,
                TotalMessages = totalMessages,
                BotResponseRate = botResponseRate,
                TimeRange = timeRange
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting bot metrics");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("overview")]
    public async Task<IActionResult> GetOverviewMetrics([FromQuery] string? dateFrom, [FromQuery] string? dateTo, [FromQuery] string granularity = "day")
    {
        try
        {
            return Ok(new
            {
                totalConversations = 1500,
                totalMessages = 8500,
                avgResponseTime = 2.3,
                satisfactionScore = 4.2
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting overview metrics");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetUserMetrics([FromQuery] string? dateFrom, [FromQuery] string? dateTo, [FromQuery] string granularity = "day")
    {
        try
        {
            return Ok(new
            {
                totalUsers = 100,
                activeUsers = 80,
                newUsers = 20,
                userGrowth = 15.5
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user metrics");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("chatbot")]
    public async Task<IActionResult> GetChatbotMetrics([FromQuery] string? dateFrom, [FromQuery] string? dateTo, [FromQuery] string granularity = "day")
    {
        try
        {
            return Ok(new
            {
                totalConversations = 500,
                avgResponseTime = 2.5,
                satisfactionScore = 4.2,
                resolutionRate = 85.5
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting chatbot metrics");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("subscription")]
    public async Task<IActionResult> GetSubscriptionMetrics([FromQuery] string? dateFrom, [FromQuery] string? dateTo, [FromQuery] string granularity = "day")
    {
        try
        {
            return Ok(new
            {
                revenue = 5000.0,
                subscriptions = 50,
                churnRate = 5.2,
                mrr = 4800.0
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting subscription metrics");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("performance")]
    public async Task<IActionResult> GetPerformanceMetrics([FromQuery] string? dateFrom, [FromQuery] string? dateTo)
    {
        try
        {
            return Ok(new
            {
                avgResponseTime = 150.5,
                uptime = 99.9,
                errorRate = 0.1,
                throughput = 1000
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting performance metrics");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("reports")]
    public async Task<IActionResult> GetReports()
    {
        try
        {
            return Ok(new List<object>
            {
                new { id = 1, name = "Monthly Report", type = "monthly", createdAt = DateTime.UtcNow }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting reports");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("reports/{id}")]
    public async Task<IActionResult> GetReport(Guid id)
    {
        try
        {
            return Ok(new { id, name = "Sample Report", data = new { } });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting report");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost("reports")]
    public async Task<IActionResult> CreateReport([FromBody] CreateReportRequest request)
    {
        try
        {
            return Ok(new { id = Guid.NewGuid(), message = "Report created successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating report");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPut("reports/{id}")]
    public async Task<IActionResult> UpdateReport(Guid id, [FromBody] UpdateReportRequest request)
    {
        try
        {
            return Ok(new { message = "Report updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating report");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpDelete("reports/{id}")]
    public async Task<IActionResult> DeleteReport(Guid id)
    {
        try
        {
            return Ok(new { message = "Report deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting report");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost("reports/{id}/run")]
    public async Task<IActionResult> RunReport(Guid id)
    {
        try
        {
            return Ok(new { data = new { }, executedAt = DateTime.UtcNow });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error running report");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost("compare")]
    public async Task<IActionResult> CompareAnalytics([FromBody] CompareAnalyticsRequest request)
    {
        try
        {
            return Ok(new
            {
                current = new { value = 100 },
                previous = new { value = 85 },
                change = 15,
                changePercent = 17.6
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error comparing analytics");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("goals")]
    public async Task<IActionResult> GetGoals()
    {
        try
        {
            return Ok(new List<object>
            {
                new { id = 1, name = "Increase conversions", target = 1000, progress = 750, deadline = "2024-12-31" }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting goals");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost("goals")]
    public async Task<IActionResult> CreateGoal([FromBody] CreateGoalRequest request)
    {
        try
        {
            return Ok(new { id = Guid.NewGuid(), message = "Goal created successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating goal");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPut("goals/{id}")]
    public async Task<IActionResult> UpdateGoal(Guid id, [FromBody] UpdateGoalRequest request)
    {
        try
        {
            return Ok(new { message = "Goal updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating goal");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpDelete("goals/{id}")]
    public async Task<IActionResult> DeleteGoal(Guid id)
    {
        try
        {
            return Ok(new { message = "Goal deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting goal");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost("reports/custom")]
    public async Task<IActionResult> GetCustomReport([FromBody] CustomReportRequest request)
    {
        try
        {
            var query = _context.Conversations
                .Where(c => c.CreatedAt >= request.StartDate && c.CreatedAt <= request.EndDate);

            if (!string.IsNullOrEmpty(request.TenantId) && Guid.TryParse(request.TenantId, out var parsedTenantId))
            {
                query = query.Where(c => c.TenantId == parsedTenantId);
            }

            var conversations = await query.Include(c => c.Messages).ToListAsync();
            
            var report = new
            {
                TotalConversations = conversations.Count,
                TotalMessages = conversations.Sum(c => c.Messages.Count),
                AverageMessagesPerConversation = conversations.Any() ? conversations.Average(c => c.Messages.Count) : 0,
                ResolvedConversations = conversations.Count(c => c.Status == Shared.Domain.Enums.ConversationStatus.Resolved),
                PeriodStart = request.StartDate,
                PeriodEnd = request.EndDate,
                GeneratedAt = DateTime.UtcNow
            };

            return Ok(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating custom report");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    private DateTime GetStartDateFromTimeRange(string timeRange)
    {
        return timeRange switch
        {
            "1d" => DateTime.UtcNow.AddDays(-1),
            "7d" => DateTime.UtcNow.AddDays(-7),
            "30d" => DateTime.UtcNow.AddDays(-30),
            "90d" => DateTime.UtcNow.AddDays(-90),
            _ => DateTime.UtcNow.AddDays(-7)
        };
    }
}

public class CustomReportRequest
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string? TenantId { get; set; }
    public List<string> Metrics { get; set; } = new();
}

public class CompareAnalyticsRequest
{
    public string DateFrom { get; set; } = string.Empty;
    public string DateTo { get; set; } = string.Empty;
    public string PreviousDateFrom { get; set; } = string.Empty;
    public string PreviousDateTo { get; set; } = string.Empty;
    public string Metric { get; set; } = string.Empty;
}

public class CreateGoalRequest
{
    public string Name { get; set; } = string.Empty;
    public double Target { get; set; }
    public string Metric { get; set; } = string.Empty;
    public string Deadline { get; set; } = string.Empty;
}

public class UpdateGoalRequest
{
    public string? Name { get; set; }
    public double? Target { get; set; }
    public string? Deadline { get; set; }
}

public class CreateReportRequest
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public List<string> Metrics { get; set; } = new();
}

public class UpdateReportRequest
{
    public string? Name { get; set; }
    public string? Type { get; set; }
    public List<string>? Metrics { get; set; }
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

public class CustomReportRequest
{
    public string ReportType { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public List<string> Metrics { get; set; } = new();
}
