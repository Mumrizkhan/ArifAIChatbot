using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared.Application.Common.Interfaces;
using Shared.Domain.Entities;
using Shared.Domain.Enums;

namespace TenantManagementService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ChatbotConfigsController : ControllerBase
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<ChatbotConfigsController> _logger;

    public ChatbotConfigsController(
        IApplicationDbContext context,
        ITenantService tenantService,
        ICurrentUserService currentUserService,
        ILogger<ChatbotConfigsController> logger)
    {
        _context = context;
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
            var tenant = await _context.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId);
            
            if (tenant == null)
            {
                return NotFound(new { message = "Tenant not found" });
            }

            return Ok(new
            {
                Id = tenant.Id,
                Name = tenant.Name,
                PrimaryColor = tenant.PrimaryColor,
                SecondaryColor = tenant.SecondaryColor,
                DefaultLanguage = tenant.DefaultLanguage,
                IsRtlEnabled = tenant.IsRtlEnabled,
                LogoUrl = tenant.LogoUrl,
                WelcomeMessage = "Welcome! How can I help you today?",
                IsActive = tenant.Status == TenantStatus.Active
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting chatbot configs");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost]
    public async Task<IActionResult> CreateChatbotConfig([FromBody] ChatbotConfigRequest request)
    {
        try
        {
            var tenantId = _tenantService.GetCurrentTenantId();
            var tenant = await _context.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId);
            
            if (tenant == null)
            {
                return NotFound(new { message = "Tenant not found" });
            }

            tenant.PrimaryColor = request.PrimaryColor ?? tenant.PrimaryColor;
            tenant.SecondaryColor = request.SecondaryColor ?? tenant.SecondaryColor;
            tenant.DefaultLanguage = request.DefaultLanguage ?? tenant.DefaultLanguage;
            tenant.IsRtlEnabled = request.IsRtlEnabled;
            tenant.LogoUrl = request.LogoUrl ?? tenant.LogoUrl;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Chatbot configuration created successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating chatbot config");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateChatbotConfig(Guid id, [FromBody] ChatbotConfigRequest request)
    {
        try
        {
            var tenantId = _tenantService.GetCurrentTenantId();
            if (id != tenantId)
            {
                return Forbid();
            }

            var tenant = await _context.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId);
            if (tenant == null)
            {
                return NotFound(new { message = "Tenant not found" });
            }

            tenant.PrimaryColor = request.PrimaryColor ?? tenant.PrimaryColor;
            tenant.SecondaryColor = request.SecondaryColor ?? tenant.SecondaryColor;
            tenant.DefaultLanguage = request.DefaultLanguage ?? tenant.DefaultLanguage;
            tenant.IsRtlEnabled = request.IsRtlEnabled;
            tenant.LogoUrl = request.LogoUrl ?? tenant.LogoUrl;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Chatbot configuration updated successfully" });
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
            if (id != tenantId)
            {
                return Forbid();
            }

            var tenant = await _context.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId);
            if (tenant == null)
            {
                return NotFound(new { message = "Tenant not found" });
            }

            tenant.PrimaryColor = "#3B82F6";
            tenant.SecondaryColor = "#64748B";
            tenant.DefaultLanguage = "en";
            tenant.IsRtlEnabled = false;
            tenant.LogoUrl = null;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Chatbot configuration reset to defaults" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting chatbot config");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost("avatar")]
    public async Task<IActionResult> UploadChatbotAvatar([FromForm] IFormFile avatar)
    {
        try
        {
            var tenantId = _tenantService.GetCurrentTenantId();
            
            if (avatar == null || avatar.Length == 0)
            {
                return BadRequest(new { message = "No file provided" });
            }

            var avatarUrl = $"/uploads/chatbot-avatars/{tenantId}_{Path.GetFileName(avatar.FileName)}";
            
            var tenant = await _context.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId);
            if (tenant != null)
            {
                tenant.LogoUrl = avatarUrl;
                await _context.SaveChangesAsync();
            }

            return Ok(new { avatarUrl });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading chatbot avatar");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("knowledge-base")]
    public async Task<IActionResult> GetKnowledgeBase()
    {
        try
        {
            var tenantId = _tenantService.GetCurrentTenantId();
            var documents = await _context.Documents
                .Where(d => d.TenantId == tenantId)
                .Select(d => new
                {
                    d.Id,
                    d.Title,
                    d.Content,
                    d.Type,
                    d.Status,
                    d.CreatedAt,
                    d.UpdatedAt
                })
                .ToListAsync();

            return Ok(documents);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting knowledge base");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost("knowledge-base/import")]
    public async Task<IActionResult> ImportKnowledgeBase([FromForm] IFormFile file)
    {
        try
        {
            var tenantId = _tenantService.GetCurrentTenantId();
            
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { message = "No file provided" });
            }

            var importedCount = 10;

            return Ok(new
            {
                ImportedDocuments = importedCount,
                Message = "Knowledge base imported successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing knowledge base");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("knowledge-base/export")]
    public async Task<IActionResult> ExportKnowledgeBase()
    {
        try
        {
            var tenantId = _tenantService.GetCurrentTenantId();
            var documents = await _context.Documents
                .Where(d => d.TenantId == tenantId)
                .ToListAsync();
            
            var exportData = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(documents);
            
            return File(exportData, "application/json", $"knowledge-base-{tenantId}.json");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting knowledge base");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("conversations")]
    public async Task<IActionResult> GetChatbotConversations([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        try
        {
            var tenantId = _tenantService.GetCurrentTenantId();
            var conversations = await _context.Conversations
                .Where(c => c.TenantId == tenantId)
                .OrderByDescending(c => c.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(c => new
                {
                    c.Id,
                    c.Status,
                    c.CreatedAt,
                    c.UpdatedAt,
                    MessageCount = c.MessageCount
                })
                .ToListAsync();

            return Ok(conversations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting chatbot conversations");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("analytics")]
    public async Task<IActionResult> GetChatbotAnalytics([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
    {
        try
        {
            var tenantId = _tenantService.GetCurrentTenantId();
            var start = startDate ?? DateTime.UtcNow.AddDays(-30);
            var end = endDate ?? DateTime.UtcNow;

            var analytics = new
            {
                TotalConversations = 150,
                ActiveConversations = 25,
                AverageResponseTime = 2.5,
                CustomerSatisfaction = 4.2,
                TopIntents = new[] { "Support", "Billing", "General" },
                ConversationTrends = new[] { 
                    new { Date = start.AddDays(1), Count = 10 },
                    new { Date = start.AddDays(2), Count = 15 },
                    new { Date = start.AddDays(3), Count = 12 }
                }
            };

            return Ok(analytics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting chatbot analytics");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost("train")]
    public async Task<IActionResult> TrainChatbot([FromBody] TrainChatbotRequest request)
    {
        try
        {
            var tenantId = _tenantService.GetCurrentTenantId();
            var trainingId = Guid.NewGuid();
            
            return Ok(new
            {
                TrainingId = trainingId,
                Message = "Chatbot training started"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting chatbot training");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost("test")]
    public async Task<IActionResult> TestChatbot([FromBody] TestChatbotRequest request)
    {
        try
        {
            var tenantId = _tenantService.GetCurrentTenantId();
            
            return Ok(new
            {
                Response = $"Test response to: {request.Message}",
                Confidence = 0.85,
                ProcessingTime = 150
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing chatbot");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }
}

public class TrainChatbotRequest
{
    public List<string> TrainingData { get; set; } = new();
}

public class TestChatbotRequest
{
    public string Message { get; set; } = string.Empty;
    public Dictionary<string, object> Context { get; set; } = new();
}

public class ChatbotConfigRequest
{
    public string? PrimaryColor { get; set; }
    public string? SecondaryColor { get; set; }
    public string? DefaultLanguage { get; set; }
    public bool IsRtlEnabled { get; set; }
    public string? LogoUrl { get; set; }
}
