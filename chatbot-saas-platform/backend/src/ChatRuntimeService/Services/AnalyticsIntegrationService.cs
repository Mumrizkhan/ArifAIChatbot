using System.Text.Json;
using AnalyticsService.Models;

namespace ChatRuntimeService.Services;

public class AnalyticsIntegrationService : IAnalyticsIntegrationService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AnalyticsIntegrationService> _logger;
    private readonly string _analyticsServiceUrl;

    public AnalyticsIntegrationService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<AnalyticsIntegrationService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
        _analyticsServiceUrl = _configuration["Services:Analytics"] ?? "http://localhost:5001";
    }

    public async Task<RealtimeAnalyticsDto> GetRealtimeAnalyticsAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_analyticsServiceUrl}/api/analytics/realtime");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<RealtimeAnalyticsDto>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                return result ?? new RealtimeAnalyticsDto();
            }
            return new RealtimeAnalyticsDto();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting realtime analytics");
            return new RealtimeAnalyticsDto();
        }
    }

    public async Task<DashboardStatsDto> GetDashboardStatsAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_analyticsServiceUrl}/api/analytics/dashboard");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<DashboardStatsDto>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                return result ?? new DashboardStatsDto();
            }
            return new DashboardStatsDto();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting dashboard stats");
            return new DashboardStatsDto();
        }
    }

    public async Task<ConversationMetricsDto> GetConversationMetricsAsync(string timeRange, string? tenantId = null)
    {
        try
        {
            var queryParams = $"timeRange={timeRange}";
            if (!string.IsNullOrEmpty(tenantId))
            {
                queryParams += $"&tenantId={tenantId}";
            }

            var response = await _httpClient.GetAsync($"{_analyticsServiceUrl}/api/analytics/conversations?{queryParams}");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<ConversationMetricsDto>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                return result ?? new ConversationMetricsDto();
            }
            return new ConversationMetricsDto();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting conversation metrics for timeRange: {TimeRange}, tenantId: {TenantId}", timeRange, tenantId);
            return new ConversationMetricsDto();
        }
    }

    public async Task<AgentMetricsDto> GetAgentMetricsAsync(string timeRange, string? tenantId = null)
    {
        try
        {
            var queryParams = $"timeRange={timeRange}";
            if (!string.IsNullOrEmpty(tenantId))
            {
                queryParams += $"&tenantId={tenantId}";
            }

            var response = await _httpClient.GetAsync($"{_analyticsServiceUrl}/api/analytics/agents?{queryParams}");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<AgentMetricsDto>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                return result ?? new AgentMetricsDto();
            }
            return new AgentMetricsDto();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting agent metrics for timeRange: {TimeRange}, tenantId: {TenantId}", timeRange, tenantId);
            return new AgentMetricsDto();
        }
    }

    public async Task<AnalyticsDto> GetTenantAnalyticsAsync(string tenantId, string startDate, string endDate)
    {
        try
        {
            var request = new AnalyticsRequest
            {
                TenantId = tenantId,
                DateFrom = startDate,
                DateTo = endDate
            };

            var response = await _httpClient.PostAsJsonAsync($"{_analyticsServiceUrl}/api/analytics", request);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<AnalyticsDto>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                return result ?? new AnalyticsDto();
            }
            return new AnalyticsDto();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting tenant analytics for tenantId: {TenantId}, period: {StartDate} to {EndDate}", tenantId, startDate, endDate);
            return new AnalyticsDto();
        }
    }
}
