using TenantManagementService.Models;
using Shared.Domain.Entities;

namespace TenantManagementService.Services;

public interface ITenantManagementService
{
    Task<TenantDto> CreateTenantAsync(CreateTenantRequest request, Guid? userId);
    Task<PaginatedResult<TenantDto>> GetTenantsAsync(int page, int pageSize, string? search);
    Task<TenantDto?> GetTenantAsync(Guid id, Guid? userId);
    Task<TenantDto?> UpdateTenantAsync(Guid id, UpdateTenantRequest request, Guid? userId);
    Task<TenantSettingsDto?> GetTenantSettingsAsync(Guid id, Guid? userId);
    Task<bool> UpdateTenantSettingsAsync(Guid id, UpdateTenantSettingsRequest request, Guid? userId);
    Task<TenantStatsDto?> GetTenantStatsAsync(Guid id);
    Task<bool> DeleteTenantAsync(Guid id);
    Task<string> UploadTenantLogoAsync(Guid? tenantId, IFormFile logo);
}

public interface IChatbotConfigService
{
    Task<ChatbotConfigDto> CreateChatbotConfigAsync(CreateChatbotConfigRequest request, Guid tenantId);
    Task<List<ChatbotConfigDto>> GetChatbotConfigsAsync(Guid tenantId);
    Task<ChatbotConfigDto?> GetChatbotConfigAsync(Guid id, Guid tenantId);
    Task<ChatbotConfigDto?> UpdateChatbotConfigAsync(Guid id, UpdateChatbotConfigRequest request, Guid tenantId);
    Task<bool> DeleteChatbotConfigAsync(Guid id, Guid tenantId);
}

public interface ITeamService
{
    Task<TeamMemberDto> AddTeamMemberAsync(AddTeamMemberRequest request, Guid tenantId);
    Task<List<TeamMemberDto>> GetTeamMembersAsync(Guid tenantId);
    Task<TeamMemberDto?> UpdateTeamMemberAsync(Guid id, UpdateTeamMemberRequest request, Guid tenantId);
    Task<bool> RemoveTeamMemberAsync(Guid id, Guid tenantId);
}
