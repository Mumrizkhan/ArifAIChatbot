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
}

public class ChatbotConfigRequest
{
    public string? PrimaryColor { get; set; }
    public string? SecondaryColor { get; set; }
    public string? DefaultLanguage { get; set; }
    public bool IsRtlEnabled { get; set; }
    public string? LogoUrl { get; set; }
}
