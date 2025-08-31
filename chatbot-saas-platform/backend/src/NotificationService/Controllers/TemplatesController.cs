using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NotificationService.Services;
using Shared.Application.Common.Interfaces;
using Shared.Domain.Entities;

namespace NotificationService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TemplatesController : ControllerBase
{
    private readonly ITemplateService _templateService;
    private readonly ITenantService _tenantService;
    private readonly ILogger<TemplatesController> _logger;

    public TemplatesController(
        ITemplateService templateService,
        ITenantService tenantService,
        ILogger<TemplatesController> logger)
    {
        _templateService = templateService;
        _tenantService = tenantService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetTemplates()
    {
        try
        {
            var tenantId = _tenantService.GetCurrentTenantId();
            var templates = await _templateService.GetTemplatesAsync(tenantId);
            return Ok(templates);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting templates");
            return StatusCode(500, new { error = "Failed to get templates" });
        }
    }

    [HttpPost]
    public async Task<IActionResult> CreateTemplate([FromBody] CreateTemplateRequest request)
    {
        try
        {
            var tenantId = _tenantService.GetCurrentTenantId();
            
            var template = new NotificationTemplate
            {
                Name = request.Name,
                Subject = request.Subject,
                Content = request.Content,
                Type = request.Type,
                Channel = request.Channel,
                TenantId = tenantId,
                Language = request.Language ?? "en",
                IsActive = request.IsActive,
                DefaultData = request.DefaultData ?? new Dictionary<string, object>()
            };

            var templateId = await _templateService.CreateTemplateAsync(template);
            return Ok(new { TemplateId = templateId, Message = "Template created successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating template");
            return StatusCode(500, new { error = "Failed to create template" });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateTemplate(Guid id, [FromBody] UpdateTemplateRequest request)
    {
        try
        {
            var tenantId = _tenantService.GetCurrentTenantId();
            
            // Get the existing template
            var existingTemplate = await _templateService.GetTemplateAsync(id.ToString(), tenantId);
            if (existingTemplate == null)
            {
                return NotFound(new { error = "Template not found" });
            }

            // Update only the provided properties
            if (!string.IsNullOrEmpty(request.Name))
                existingTemplate.Name = request.Name;
            if (!string.IsNullOrEmpty(request.Subject))
                existingTemplate.Subject = request.Subject;
            if (!string.IsNullOrEmpty(request.Content))
                existingTemplate.Content = request.Content;
            if (request.Type.HasValue)
                existingTemplate.Type = request.Type.Value;
            if (request.Channel.HasValue)
                existingTemplate.Channel = request.Channel.Value;
            if (!string.IsNullOrEmpty(request.Language))
                existingTemplate.Language = request.Language;
            if (request.IsActive.HasValue)
                existingTemplate.IsActive = request.IsActive.Value;
            if (request.DefaultData != null)
                existingTemplate.DefaultData = request.DefaultData;

            await _templateService.UpdateTemplateAsync( existingTemplate, tenantId);
            return Ok(new { Message = "Template updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating template");
            return StatusCode(500, new { error = "Failed to update template" });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTemplate(Guid id)
    {
        try
        {
            var tenantId = _tenantService.GetCurrentTenantId();
            await _templateService.DeleteTemplateAsync(id.ToString(), tenantId);
            return Ok(new { Message = "Template deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting template");
            return StatusCode(500, new { error = "Failed to delete template" });
        }
    }
}

public class CreateTemplateRequest
{
    public string Name { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public NotificationType Type { get; set; }
    public NotificationChannel Channel { get; set; }
    public string? Language { get; set; }
    public bool IsActive { get; set; } = true;
    public Dictionary<string, object>? DefaultData { get; set; }
}

public class UpdateTemplateRequest
{
    public string? Name { get; set; }
    public string? Subject { get; set; }
    public string? Content { get; set; }
    public NotificationType? Type { get; set; }
    public NotificationChannel? Channel { get; set; }
    public string? Language { get; set; }
    public bool? IsActive { get; set; }
    public Dictionary<string, object>? DefaultData { get; set; }
}
