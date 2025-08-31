using Microsoft.EntityFrameworkCore;
using RazorEngine;
using RazorEngine.Templating;
using Shared.Application.Common.Interfaces;
using NotificationService.Services;
using NotificationService.Models;
using Shared.Domain.Entities;

namespace NotificationService.Services;

public class TemplateService : ITemplateService
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<TemplateService> _logger;

    public TemplateService(
        IApplicationDbContext context,
        ILogger<TemplateService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<string> RenderTemplateAsync(string templateId, Dictionary<string, object> data, string language = "en")
    {
        try
        {
            var template = await _context.Set<NotificationTemplate>()
                .FirstOrDefaultAsync(t => t.Name == templateId && t.Language == language && t.IsActive);

            if (template == null)
            {
                _logger.LogWarning("Template {TemplateId} not found for language {Language}", templateId, language);
                return string.Empty;
            }

            var mergedData = new Dictionary<string, object>(template.DefaultData);
            foreach (var kvp in data)
            {
                mergedData[kvp.Key] = kvp.Value;
            }

            var templateKey = $"{template.Id}_{language}";
            
            if (!Engine.Razor.IsTemplateCached(templateKey, null))
            {
                Engine.Razor.AddTemplate(templateKey, template.Content);
                Engine.Razor.Compile(templateKey);
            }

            var result = Engine.Razor.Run(templateKey, null, mergedData);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rendering template {TemplateId} for language {Language}", templateId, language);
            return string.Empty;
        }
    }

    public async Task<NotificationTemplate?> GetTemplateAsync(string templateId, Guid tenantId, string language = "en")
    {
        try
        {
            return await _context.Set<NotificationTemplate>()
                .FirstOrDefaultAsync(t => t.Name == templateId && t.TenantId == tenantId && t.Language == language);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting template {TemplateId} for tenant {TenantId}", templateId, tenantId);
            return null;
        }
    }

    public async Task<List<NotificationTemplate>> GetTemplatesAsync(Guid tenantId, NotificationType? type = null, NotificationChannel? channel = null)
    {
        try
        {
            var query = _context.Set<NotificationTemplate>()
                .Where(t => t.TenantId == tenantId && t.IsActive);

            if (type.HasValue)
            {
                query = query.Where(t => t.Type == type.Value);
            }

            if (channel.HasValue)
            {
                query = query.Where(t => t.Channel == channel.Value);
            }

            return await query.OrderBy(t => t.Name).ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting templates for tenant {TenantId}", tenantId);
            return new List<NotificationTemplate>();
        }
    }

    public async Task<NotificationTemplate> CreateTemplateAsync(NotificationTemplate template)
    {
        try
        {
            template.Id = Guid.NewGuid();
            template.CreatedAt = DateTime.UtcNow;
            
            _context.Set<NotificationTemplate>().Add(template);
            await _context.SaveChangesAsync();

            return template;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating template {TemplateName}", template.Name);
            throw;
        }
    }

    public async Task<bool> UpdateTemplateAsync(NotificationTemplate template, Guid tenantId)
    {
        try
        {
            var existingTemplate = await _context.Set<NotificationTemplate>()
                .FirstOrDefaultAsync(t => t.Id == template.Id);

            if (existingTemplate == null)
                return false;

            existingTemplate.Name = template.Name;
            existingTemplate.Subject = template.Subject;
            existingTemplate.Content = template.Content;
            existingTemplate.Type = template.Type;
            existingTemplate.Channel = template.Channel;
            existingTemplate.Language = template.Language;
            existingTemplate.IsActive = template.IsActive;
            existingTemplate.DefaultData = template.DefaultData;
            existingTemplate.UpdatedAt = DateTime.UtcNow;
            existingTemplate.TenantId = tenantId;
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating template {TemplateId}", template.Id);
            return false;
        }
    }

    public async Task<bool> DeleteTemplateAsync(string templateId, Guid tenantId)
    {
        try
        {
            var template = await _context.Set<NotificationTemplate>()
                .FirstOrDefaultAsync(t => t.Name == templateId && t.TenantId == tenantId);

            if (template == null)
                return false;

            _context.Set<NotificationTemplate>().Remove(template);
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting template {TemplateId} for tenant {TenantId}", templateId, tenantId);
            return false;
        }
    }
}
