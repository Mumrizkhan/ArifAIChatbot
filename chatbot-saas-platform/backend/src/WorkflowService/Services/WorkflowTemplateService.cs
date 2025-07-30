using Microsoft.EntityFrameworkCore;
using Shared.Application.Common.Interfaces;
using WorkflowService.Services;
using WorkflowService.Models;
using Shared.Domain.Entities;

namespace WorkflowService.Services;

public class WorkflowTemplateService : IWorkflowTemplateService
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<WorkflowTemplateService> _logger;

    public WorkflowTemplateService(
        IApplicationDbContext context,
        ILogger<WorkflowTemplateService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<WorkflowTemplate>> GetTemplatesAsync(string? category = null, bool publicOnly = true)
    {
        try
        {
            var query = _context.Set<WorkflowTemplate>().AsQueryable();

            if (publicOnly)
            {
                query = query.Where(t => t.IsPublic);
            }

            if (!string.IsNullOrEmpty(category))
            {
                query = query.Where(t => t.Category == category);
            }

            return await query
                .OrderByDescending(t => t.Rating)
                .ThenByDescending(t => t.UsageCount)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting workflow templates");
            throw;
        }
    }

    public async Task<WorkflowTemplate?> GetTemplateAsync(Guid templateId)
    {
        try
        {
            return await _context.Set<WorkflowTemplate>()
                .FirstOrDefaultAsync(t => t.Id == templateId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting workflow template {TemplateId}", templateId);
            throw;
        }
    }

    public async Task<WorkflowTemplate> CreateTemplateAsync(WorkflowTemplate template, Guid tenantId, Guid userId)
    {
        try
        {
            template.Id = Guid.NewGuid();
            template.TenantId = template.IsPublic ? null : tenantId;
            template.CreatedAt = DateTime.UtcNow;

            _context.Set<WorkflowTemplate>().Add(template);
            await _context.SaveChangesAsync();

            return template;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating workflow template");
            throw;
        }
    }

    public async Task<bool> UpdateTemplateAsync(WorkflowTemplate template)
    {
        try
        {
            var existingTemplate = await _context.Set<WorkflowTemplate>()
                .FirstOrDefaultAsync(t => t.Id == template.Id);

            if (existingTemplate == null)
                return false;

            existingTemplate.Name = template.Name;
            existingTemplate.Description = template.Description;
            existingTemplate.Category = template.Category;
            existingTemplate.Definition = template.Definition;
            existingTemplate.DefaultVariables = template.DefaultVariables;
            existingTemplate.Tags = template.Tags;
            existingTemplate.IsPublic = template.IsPublic;
            existingTemplate.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating workflow template {TemplateId}", template.Id);
            return false;
        }
    }

    public async Task<bool> DeleteTemplateAsync(Guid templateId, Guid tenantId)
    {
        try
        {
            var template = await _context.Set<WorkflowTemplate>()
                .FirstOrDefaultAsync(t => t.Id == templateId && (t.TenantId == tenantId || t.IsPublic));

            if (template == null)
                return false;

            _context.Set<WorkflowTemplate>().Remove(template);
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting workflow template {TemplateId}", templateId);
            return false;
        }
    }

    public async Task<Workflow> CreateWorkflowFromTemplateAsync(Guid templateId, string workflowName, Guid tenantId, Guid userId)
    {
        try
        {
            var template = await GetTemplateAsync(templateId);
            if (template == null)
                throw new ArgumentException("Template not found");

            var workflow = new Workflow
            {
                Id = Guid.NewGuid(),
                Name = workflowName,
                Description = $"Created from template: {template.Name}",
                TenantId = tenantId,
                CreatedByUserId = userId,
                Status = WorkflowStatus.Draft,
                Definition = template.Definition,
                Variables = new Dictionary<string, object>(template.DefaultVariables),
                Tags = new List<string>(template.Tags),
                TemplateId = templateId.ToString(),
                CreatedAt = DateTime.UtcNow
            };

            template.UsageCount++;
            await _context.SaveChangesAsync();

            return workflow;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating workflow from template {TemplateId}", templateId);
            throw;
        }
    }

    public async Task<List<string>> GetCategoriesAsync()
    {
        try
        {
            return await _context.Set<WorkflowTemplate>()
                .Where(t => t.IsPublic)
                .Select(t => t.Category)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting workflow template categories");
            throw;
        }
    }
}
