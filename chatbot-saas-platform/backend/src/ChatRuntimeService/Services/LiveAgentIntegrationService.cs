using Newtonsoft.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ChatRuntimeService.Services;

public interface ILiveAgentIntegrationService
{
    Task<bool> AddToQueueAsync(Guid conversationId);
    Task<Guid?> FindAvailableAgentAsync(Guid tenantId, string? language = null);
    Task<bool> AssignConversationToAgentAsync(Guid conversationId, Guid agentId);
}

public class LiveAgentIntegrationService : ILiveAgentIntegrationService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<LiveAgentIntegrationService> _logger;
    private readonly string _liveAgentServiceUrl;

    public LiveAgentIntegrationService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<LiveAgentIntegrationService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
        _liveAgentServiceUrl = _configuration["Services:LiveAgent"] ?? "http://localhost:5003";
    }

    public async Task<bool> AddToQueueAsync(Guid conversationId)
    {
        try
        {
            var request = new { ConversationId = conversationId, Priority = "Normal" };
            var response = await _httpClient.PostAsJsonAsync($"{_liveAgentServiceUrl}/api/agents/queue", request);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding conversation {ConversationId} to queue", conversationId);
            return false;
        }
    }

    public async Task<Guid?> FindAvailableAgentAsync(Guid tenantId, string? language = null)
    {
        try
        {
            
            var response = await _httpClient.GetAsync($"{_liveAgentServiceUrl}/api/agents/available?tenantId={tenantId}&language={language}");

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var result = System.Text.Json.JsonSerializer.Deserialize<FindAgentResponse>(content, options);
                return Guid.Parse( result?.AgentId??"");
            }
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding available agent for tenant {TenantId}", tenantId);
            return null;
        }
    }

    public async Task<bool> AssignConversationToAgentAsync(Guid conversationId, Guid agentId)
    {
        try
        {
            var request = new { ConversationId = conversationId, AgentId = agentId };
            var response = await _httpClient.PostAsJsonAsync($"{_liveAgentServiceUrl}/api/agents/assign-conversation", request);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning conversation {ConversationId} to agent {AgentId}", conversationId, agentId);
            return false;
        }
    }
}

public class FindAgentResponse
{

    public string AgentId { get; set; } 
}
