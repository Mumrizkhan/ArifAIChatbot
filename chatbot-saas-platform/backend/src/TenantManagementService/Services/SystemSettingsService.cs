using Microsoft.EntityFrameworkCore;
using Shared.Application.Common.Interfaces;
using Shared.Domain.Entities;
using TenantManagementService.Models;

namespace TenantManagementService.Services;

public class SystemSettingsService : ISystemSettingsService
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<SystemSettingsService> _logger;

    public SystemSettingsService(IApplicationDbContext context, ILogger<SystemSettingsService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<SystemSettingsDto> GetSystemSettingsAsync()
    {
        var systemSettings = await _context.SystemSettings
            .Where(s => s.Category == "system" && s.IsActive)
            .FirstOrDefaultAsync();

        var notificationSettings = await _context.SystemSettings
            .Where(s => s.Category == "notifications" && s.IsActive)
            .FirstOrDefaultAsync();

        var integrationSettings = await _context.SystemSettings
            .Where(s => s.Category == "integrations" && s.IsActive)
            .FirstOrDefaultAsync();

        return new SystemSettingsDto
        {
            SystemSettings = systemSettings?.Settings ?? new Dictionary<string, object>(),
            NotificationSettings = notificationSettings?.Settings ?? new Dictionary<string, object>(),
            IntegrationSettings = integrationSettings?.Settings ?? new Dictionary<string, object>(),
            LastUpdated = new[] { systemSettings?.UpdatedAt, notificationSettings?.UpdatedAt, integrationSettings?.UpdatedAt }
                .Where(d => d.HasValue)
                .DefaultIfEmpty(DateTime.UtcNow)
                .Max() ?? DateTime.UtcNow,
            UpdatedBy = systemSettings?.UpdatedBy ?? notificationSettings?.UpdatedBy ?? integrationSettings?.UpdatedBy
        };
    }

    public async Task<bool> UpdateSystemSettingsAsync(UpdateSystemSettingsRequest request, string? updatedBy)
    {
        try
        {
            if (request.SystemSettings != null)
            {
                await UpdateCategorySettingsAsync("system", request.SystemSettings, updatedBy);
            }

            if (request.NotificationSettings != null)
            {
                await UpdateCategorySettingsAsync("notifications", request.NotificationSettings, updatedBy);
            }

            if (request.IntegrationSettings != null)
            {
                await UpdateCategorySettingsAsync("integrations", request.IntegrationSettings, updatedBy);
            }

            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating system settings");
            return false;
        }
    }

    private async Task UpdateCategorySettingsAsync(string category, Dictionary<string, object> settings, string? updatedBy)
    {
        var existingSettings = await _context.SystemSettings
            .FirstOrDefaultAsync(s => s.Category == category && s.IsActive);

        if (existingSettings == null)
        {
            existingSettings = new SystemSettings
            {
                Category = category,
                Settings = settings,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = updatedBy
            };
            _context.SystemSettings.Add(existingSettings);
        }
        else
        {
            foreach (var setting in settings)
            {
                existingSettings.Settings[setting.Key] = setting.Value;
            }
            existingSettings.UpdatedAt = DateTime.UtcNow;
            existingSettings.UpdatedBy = updatedBy;
        }
    }
}
