using AnalyticsService.Models;
using AnalyticsService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AnalyticsService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AnalyticsController : ControllerBase
{
    private readonly IAnalyticsService _analyticsService;
    private readonly ILogger<AnalyticsController> _logger;

    public AnalyticsController(IAnalyticsService analyticsService, ILogger<AnalyticsController> logger)
    {
        _analyticsService = analyticsService;
        _logger = logger;
    }

    [HttpGet("conversations/summary")]
    public async Task<IActionResult> GetConversationsSummary()
    {
        try
        {
            var summary = await _analyticsService.GetConversationsSummaryAsync();
            return Ok(new
            {
                TotalConversations = summary.TotalConversations,
                ActiveConversations = summary.ActiveConversations,
                ResolvedConversations = summary.CompletedConversations
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting conversations summary");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("messages/summary")]
    public async Task<IActionResult> GetMessagesSummary()
    {
        try
        {
            var summary = await _analyticsService.GetMessagesSummaryAsync();
            return Ok(new
            {
                TotalMessages = summary.TotalMessages,
                TodayMessages = summary.TotalMessages
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting messages summary");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("tenants/summary")]
    public async Task<IActionResult> GetTenantsSummary()
    {
        try
        {
            var summary = await _analyticsService.GetTenantsSummaryAsync();
            return Ok(new
            {
                TotalTenants = summary.TotalTenants,
                ActiveTenants = summary.ActiveTenants
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting tenants summary");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("realtime")]
    public async Task<IActionResult> GetRealtimeAnalytics()
    {
        try
        {
            var analytics = await _analyticsService.GetRealtimeAnalyticsAsync();
            return Ok(new
            {
                ActiveConversations = analytics.OngoingConversations,
                RecentMessages = analytics.ActiveUsers,
                OnlineAgents = analytics.AvailableAgents,
                Timestamp = DateTime.UtcNow
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
            var analytics = await _analyticsService.GetAnalyticsAsync(request);
            return Ok(analytics);
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
            var bytes = await _analyticsService.ExportAnalyticsAsync(request);
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
            var stats = await _analyticsService.GetDashboardStatsAsync();
            return Ok(new
            {
                TotalTenants = stats.TotalUsers,
                ActiveTenants = stats.TotalUsers,
                TotalUsers = stats.TotalUsers,
                ActiveUsers = stats.TotalUsers,
                TotalConversations = stats.TotalConversations,
                ActiveConversations = stats.TotalConversations,
                TotalMessages = stats.TotalMessages,
                TodayMessages = stats.TotalMessages
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
            var metrics = await _analyticsService.GetConversationMetricsAsync(timeRange, tenantId);
            return Ok(new
            {
                TotalConversations = metrics.Total,
                ResolvedConversations = metrics.Completed,
                ResolutionRate = metrics.Total > 0 ? (double)metrics.Completed / metrics.Total : 0,
                AverageRating = metrics.AverageRating,
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
            var metrics = await _analyticsService.GetAgentMetricsAsync(timeRange, tenantId);
            return Ok(new
            {
                TotalAgents = metrics.TotalAgents,
                ActiveAgents = metrics.ActiveAgents,
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
            var metrics = await _analyticsService.GetBotMetricsAsync(timeRange, tenantId);
            return Ok(new
            {
                BotMessages = metrics.TotalInteractions,
                TotalMessages = metrics.TotalInteractions,
                BotResponseRate = metrics.SuccessRate,
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
            var metrics = await _analyticsService.GetOverviewMetricsAsync(dateFrom, dateTo, granularity);
            return Ok(new
            {
                ConversationsByDate = metrics.ConversationsByDate,
                MessagesByDate = metrics.MessagesByDate,
                Granularity = granularity
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
            var metrics = await _analyticsService.GetUserMetricsAsync(dateFrom, dateTo, granularity);
            return Ok(new
            {
                TotalUsers = metrics.TotalUsers,
                ActiveUsers = metrics.ActiveUsers,
                NewUsers = metrics.NewUsers,
                Granularity = granularity
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
            var metrics = await _analyticsService.GetChatbotMetricsAsync(dateFrom, dateTo, granularity);
            return Ok(new
            {
                TotalChatbots = metrics.TotalChatbots,
                ActiveChatbots = metrics.ActiveChatbots,
                AverageUptime = metrics.AverageUptime,
                Granularity = granularity
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
            var metrics = await _analyticsService.GetSubscriptionMetricsAsync(dateFrom, dateTo, granularity);
            return Ok(new
            {
                TotalSubscriptions = metrics.TotalSubscriptions,
                ActiveSubscriptions = metrics.ActiveSubscriptions,
                TotalRevenue = metrics.TotalRevenue,
                Granularity = granularity
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
            var metrics = await _analyticsService.GetPerformanceMetricsAsync(dateFrom, dateTo);
            return Ok(new
            {
                AverageResponseTime = metrics.AverageResponseTime,
                SystemUptime = metrics.SystemUptime,
                ErrorRate = metrics.ErrorRate
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
            var reports = await _analyticsService.GetReportsAsync();
            return Ok(reports);
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
            var report = await _analyticsService.GetReportAsync(id);
            return Ok(report);
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
            var report = await _analyticsService.CreateReportAsync(request);
            return Ok(new { id = report.Id, message = "Report created successfully" });
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
            var result = await _analyticsService.UpdateReportAsync(id, request);
            if (!result)
            {
                return NotFound(new { message = "Report not found" });
            }
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
            var result = await _analyticsService.DeleteReportAsync(id);
            if (!result)
            {
                return NotFound(new { message = "Report not found" });
            }
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
            var execution = await _analyticsService.RunReportAsync(id);
            return Ok(new { data = execution, executedAt = DateTime.UtcNow });
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
            var comparison = await _analyticsService.CompareAnalyticsAsync(request);
            return Ok(new
            {
                current = comparison.Current,
                previous = comparison.Previous,
                change = comparison.Change,
                changePercent = comparison.ChangePercent
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
            var goals = await _analyticsService.GetGoalsAsync();
            return Ok(goals);
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
            var goal = await _analyticsService.CreateGoalAsync(request);
            return Ok(new { id = goal.Id, message = "Goal created successfully" });
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
            var result = await _analyticsService.UpdateGoalAsync(id, request);
            if (!result)
            {
                return NotFound(new { message = "Goal not found" });
            }
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
            var result = await _analyticsService.DeleteGoalAsync(id);
            if (!result)
            {
                return NotFound(new { message = "Goal not found" });
            }
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
            var report = await _analyticsService.GetCustomReportAsync(request);
            return Ok(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating custom report");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost("events/batch")]
    public async Task<IActionResult> TrackEventsBatch([FromBody] AnalyticsEventBatchRequest request)
    {
        try
        {
            var tenantId = GetTenantIdFromHeader();
            var success = await _analyticsService.TrackEventsBatchAsync(request, tenantId);

            if (success)
            {
                _logger.LogInformation("Tracked {Count} analytics events for tenant {TenantId}",
                    request.Events.Count, tenantId);

                return Ok(new { Message = $"Successfully tracked {request.Events.Count} events" });
            }
            else
            {
                return StatusCode(500, new { Message = "Failed to track analytics events" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to track analytics events");
            return StatusCode(500, new { Message = "Failed to track analytics events" });
        }
    }

    /// <summary>
    /// Get analytics dashboard data for a tenant
    /// </summary>
    [HttpGet("dashboard/{tenantId}")]
    public async Task<IActionResult> GetDashboardData(
        string tenantId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to)
    {
        try
        {
            var dashboard = await _analyticsService.GetDashboardDataAsync(tenantId, from, to);
            return Ok(dashboard);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get dashboard data for tenant {TenantId}", tenantId);
            return StatusCode(500, new { Message = "Failed to retrieve dashboard data" });
        }
    }

    /// <summary>
    /// Get performance metrics for a specific agent
    /// </summary>
    [HttpGet("agents/{agentId}/metrics")]
    public async Task<IActionResult> GetAgentMetricsDetailed(
        string agentId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to)
    {
        try
        {
            var tenantId = GetTenantIdFromHeader();
            var metrics = await _analyticsService.GetAgentMetricsDetailedAsync(agentId, tenantId, from, to);
            return Ok(metrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get metrics for agent {AgentId}", agentId);
            return StatusCode(500, new { Message = "Failed to retrieve agent metrics" });
        }
    }

    /// <summary>
    /// Get conversation analytics
    /// </summary>
    [HttpGet("conversations/{conversationId}")]
    public async Task<IActionResult> GetConversationAnalytics(string conversationId)
    {
        try
        {
            var tenantId = GetTenantIdFromHeader();
            var analytics = await _analyticsService.GetConversationAnalyticsAsync(conversationId, tenantId);
            return Ok(analytics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get analytics for conversation {ConversationId}", conversationId);
            return StatusCode(500, new { Message = "Failed to retrieve conversation analytics" });
        }
    }

    /// <summary>
    /// Get real-time analytics summary
    /// </summary>
    [HttpGet("realtime/{tenantId}")]
    public async Task<IActionResult> GetRealtimeAnalyticsDetailed(string tenantId)
    {
        try
        {
            var realtime = await _analyticsService.GetRealtimeAnalyticsDetailedAsync(tenantId);
            return Ok(realtime);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get realtime analytics for tenant {TenantId}", tenantId);
            return StatusCode(500, new { Message = "Failed to retrieve realtime analytics" });
        }
    }

    #region Private Helper Methods

    private string GetTenantIdFromHeader()
    {
        return Request.Headers["X-Tenant-ID"].FirstOrDefault() ?? throw new UnauthorizedAccessException("Tenant ID is required");
    }

    #endregion
}

//public class CustomReportRequest
//{
//    public DateTime StartDate { get; set; }
//    public DateTime EndDate { get; set; }
//    public string? TenantId { get; set; }
//    public List<string> Metrics { get; set; } = new();
//}

//public class CompareAnalyticsRequest
//{
//    public string DateFrom { get; set; } = string.Empty;
//    public string DateTo { get; set; } = string.Empty;
//    public string PreviousDateFrom { get; set; } = string.Empty;
//    public string PreviousDateTo { get; set; } = string.Empty;
//    public string Metric { get; set; } = string.Empty;
//}

//public class CreateGoalRequest
//{
//    public string Name { get; set; } = string.Empty;
//    public double Target { get; set; }
//    public string Metric { get; set; } = string.Empty;
//    public string Deadline { get; set; } = string.Empty;
//}

//public class UpdateGoalRequest
//{
//    public string? Name { get; set; }
//    public double? Target { get; set; }
//    public string? Deadline { get; set; }
//}

//public class CreateReportRequest
//{
//    public string Name { get; set; } = string.Empty;
//    public string Type { get; set; } = string.Empty;
//    public List<string> Metrics { get; set; } = new();
//}

//public class UpdateReportRequest
//{
//    public string? Name { get; set; }
//    public string? Type { get; set; }
//    public List<string>? Metrics { get; set; }
//}

//public class AnalyticsRequest
//{
//    public DateTime StartDate { get; set; }
//    public DateTime EndDate { get; set; }
//}

//public class ExportAnalyticsRequest
//{
//    public DateTime StartDate { get; set; }
//    public DateTime EndDate { get; set; }
//    public string Format { get; set; } = "csv";
//}

//public class CustomReportRequest
//{
//    public string ReportType { get; set; } = string.Empty;
//    public DateTime StartDate { get; set; }
//    public DateTime EndDate { get; set; }
//    public List<string> Metrics { get; set; } = new();
//}
