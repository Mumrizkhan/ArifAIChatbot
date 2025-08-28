using TenantManagementService.Models;

namespace TenantManagementService.Services;

public interface ISystemSettingsService
{
    Task<SystemSettingsDto> GetSystemSettingsAsync();
    Task<bool> UpdateSystemSettingsAsync(UpdateSystemSettingsRequest request, string? updatedBy);
}
