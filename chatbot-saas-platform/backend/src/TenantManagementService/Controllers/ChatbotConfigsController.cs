using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TenantManagementService.Services;
using TenantManagementService.Models;
using Shared.Infrastructure.Services;
using Shared.Application.Common.Interfaces;

namespace TenantManagementService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ChatbotConfigsController : ControllerBase
{
    private readonly IChatbotConfigService _chatbotConfigService;
    private readonly ITenantService _tenantService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<ChatbotConfigsController> _logger;

    public ChatbotConfigsController(
        IChatbotConfigService chatbotConfigService,
        ITenantService tenantService,
        ICurrentUserService currentUserService,
        ILogger<ChatbotConfigsController> logger)
    {
        _chatbotConfigService = chatbotConfigService;
        _tenantService = tenantService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetChatbotConfigs()
    {
        try
        {
            var tenantId = _tenantService.GetCurrentTenantId();
            var configs = await _chatbotConfigService.GetChatbotConfigsAsync(tenantId);
            return Ok(configs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting chatbot configs");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost]
    public async Task<IActionResult> CreateChatbotConfig([FromBody] CreateChatbotConfigRequest request)
    {
        try
        {
            var tenantId = _tenantService.GetCurrentTenantId();
            var config = await _chatbotConfigService.CreateChatbotConfigAsync(request, tenantId);
            return Ok(new { message = "Chatbot configuration created successfully", config });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating chatbot config");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateChatbotConfig(Guid id, [FromBody] UpdateChatbotConfigRequest request)
    {
        try
        {
            var tenantId = _tenantService.GetCurrentTenantId();
            var config = await _chatbotConfigService.UpdateChatbotConfigAsync(id, request, tenantId);

            if (config == null)
            {
                return NotFound(new { message = "Chatbot configuration not found" });
            }

            return Ok(new { message = "Chatbot configuration updated successfully", config });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating chatbot config");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteChatbotConfig(Guid id)
    {
        try
        {
            var tenantId = _tenantService.GetCurrentTenantId();
            var deleted = await _chatbotConfigService.DeleteChatbotConfigAsync(id, tenantId);

            if (!deleted)
            {
                return NotFound(new { message = "Chatbot configuration not found" });
            }

            return Ok(new { message = "Chatbot configuration deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting chatbot config");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost("{configId}/avatar")]
    public async Task<IActionResult> UploadChatbotAvatar(Guid configId, [FromForm] IFormFile avatar)
    {
        try
        {
            var tenantId = _tenantService.GetCurrentTenantId();
            
            if (avatar == null || avatar.Length == 0)
            {
                return BadRequest(new { message = "No file provided" });
            }

            var avatarUrl = await _chatbotConfigService.UploadAvatarAsync(configId, avatar, tenantId);
            return Ok(new { avatarUrl });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading chatbot avatar");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("{configId}/knowledge-base")]
    public async Task<IActionResult> GetKnowledgeBase(Guid configId)
    {
        try
        {
            var tenantId = _tenantService.GetCurrentTenantId();
            var knowledgeBase = await _chatbotConfigService.GetKnowledgeBaseAsync(configId, tenantId);
            return Ok(knowledgeBase);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting knowledge base");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost("{configId}/knowledge-base/documents")]
    public async Task<IActionResult> AddKnowledgeBaseDocument(Guid configId, [FromForm] IFormFile document)
    {
        try
        {
            var tenantId = _tenantService.GetCurrentTenantId();
            
            if (document == null || document.Length == 0)
            {
                return BadRequest(new { message = "No file provided" });
            }

            var success = await _chatbotConfigService.AddKnowledgeBaseDocumentAsync(configId, document, tenantId);
            
            if (!success)
            {
                return NotFound(new { message = "Chatbot configuration not found" });
            }

            return Ok(new { message = "Document added to knowledge base successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding knowledge base document");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpDelete("{configId}/knowledge-base/documents/{documentId}")]
    public async Task<IActionResult> RemoveKnowledgeBaseDocument(Guid configId, Guid documentId)
    {
        try
        {
            var tenantId = _tenantService.GetCurrentTenantId();
            var success = await _chatbotConfigService.RemoveKnowledgeBaseDocumentAsync(configId, documentId, tenantId);
            
            if (!success)
            {
                return NotFound(new { message = "Document not found" });
            }

            return Ok(new { message = "Document removed from knowledge base successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing knowledge base document");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("{configId}/analytics")]
    public async Task<IActionResult> GetChatbotAnalytics(Guid configId)
    {
        try
        {
            var tenantId = _tenantService.GetCurrentTenantId();
            var analytics = await _chatbotConfigService.GetAnalyticsAsync(configId, tenantId);
            return Ok(analytics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting chatbot analytics");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("{configId}/training-data")]
    public async Task<IActionResult> GetTrainingData(Guid configId)
    {
        try
        {
            var tenantId = _tenantService.GetCurrentTenantId();
            var trainingData = await _chatbotConfigService.GetTrainingDataAsync(configId, tenantId);
            return Ok(trainingData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting training data");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost("{configId}/train")]
    public async Task<IActionResult> TrainChatbot(Guid configId)
    {
        try
        {
            var tenantId = _tenantService.GetCurrentTenantId();
            var success = await _chatbotConfigService.TrainChatbotAsync(configId, tenantId);
            
            if (!success)
            {
                return NotFound(new { message = "Chatbot configuration not found" });
            }
            
            return Ok(new { message = "Chatbot training started successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting chatbot training");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

}
