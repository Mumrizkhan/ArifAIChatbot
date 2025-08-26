using Newtonsoft.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Shared.Application.Common.Interfaces;

namespace ChatRuntimeService.Services;

public interface ILiveAgentIntegrationService
{
    Task<bool> AddToQueueAsync(Guid conversationId);
    Task<Guid?> FindAvailableAgentAsync(Guid tenantId, string? language = null);
    Task<bool> AssignConversationToAgentAsync(Guid conversationId, Guid agentId);
    Task<bool> NotifyEscalationAsync(Guid conversationId, string? customerName, string? customerEmail, string? subject, string? language);
    Task<bool> NotifyMessageAsync(Guid conversationId, Guid messageId, string content, string type, string sender, Guid? senderId, string? senderName, DateTime createdAt);
}

public class LiveAgentIntegrationService : ILiveAgentIntegrationService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<LiveAgentIntegrationService> _logger;
    private readonly ITenantService _tenantService;
    private readonly string _liveAgentServiceUrl;

    public LiveAgentIntegrationService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<LiveAgentIntegrationService> logger,
        ITenantService tenantService)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
        _tenantService = tenantService;
        _liveAgentServiceUrl = _configuration["Services:LiveAgent"] ?? "http://localhost:5003";
    }

    private void SetTenantHeader()
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        _httpClient.DefaultRequestHeaders.Remove("X-Tenant-ID");
        _httpClient.DefaultRequestHeaders.Add("X-Tenant-ID", tenantId.ToString());
    }

    public async Task<bool> AddToQueueAsync(Guid conversationId)
    {
        try
        {
            SetTenantHeader();
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
            SetTenantHeader();
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
            SetTenantHeader();
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

    public async Task<bool> NotifyEscalationAsync(Guid conversationId, string? customerName, string? customerEmail, string? subject, string? language)
    {
        try
        {
            SetTenantHeader();
            var request = new 
            { 
                ConversationId = conversationId,
                CustomerName = customerName,
                CustomerEmail = customerEmail,
                Subject = subject,
                Language = language
            };
            var response = await _httpClient.PostAsJsonAsync($"{_liveAgentServiceUrl}/api/agents/notify-escalation", request);
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Escalation notification sent successfully for conversation {ConversationId}", conversationId);
                return true;
            }
            else
            {
                _logger.LogWarning("Failed to send escalation notification for conversation {ConversationId}. Status: {StatusCode}", 
                    conversationId, response.StatusCode);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error notifying escalation for conversation {ConversationId}", conversationId);
            return false;
        }
    }

    public async Task<bool> NotifyMessageAsync(Guid conversationId, Guid messageId, string content, string type, string sender, Guid? senderId, string? senderName, DateTime createdAt)
    {
        try
        {
            SetTenantHeader();

            var request = new
            {
                ConversationId = conversationId,
                MessageId = messageId,
                Content = content,
                Type = type,
                Sender = sender,
                SenderId = senderId,
                SenderName = senderName,
                CreatedAt = createdAt
            };

            var response = await _httpClient.PostAsJsonAsync($"{_liveAgentServiceUrl}/api/agents/notify-message", request);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Message notification sent successfully for conversation {ConversationId}", conversationId);
                return true;
            }

            _logger.LogWarning("Failed to send message notification for conversation {ConversationId}. Status: {StatusCode}", 
                conversationId, response.StatusCode);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending message notification for conversation {ConversationId}", conversationId);
            return false;
        }
    }
}

public class FindAgentResponse
{
    public string AgentId { get; set; } = string.Empty;
}
