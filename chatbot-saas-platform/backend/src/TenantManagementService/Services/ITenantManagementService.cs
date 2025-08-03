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
    Task<TenantDto?> GetTenantByUserAsync(Guid tenantId, Guid userId);
    Task<TenantDto?> UpdateTenantByUserAsync(Guid tenantId, UpdateTenantRequest request, Guid userId);
}

public interface IChatbotConfigService
{
    Task<ChatbotConfigDto> CreateChatbotConfigAsync(CreateChatbotConfigRequest request, Guid tenantId);
    Task<List<ChatbotConfigDto>> GetChatbotConfigsAsync(Guid tenantId);
    Task<ChatbotConfigDto?> GetChatbotConfigAsync(Guid id, Guid tenantId);
    Task<ChatbotConfigDto?> UpdateChatbotConfigAsync(Guid id, UpdateChatbotConfigRequest request, Guid tenantId);
    Task<bool> DeleteChatbotConfigAsync(Guid id, Guid tenantId);
    Task<string> UploadAvatarAsync(Guid configId, IFormFile avatar, Guid tenantId);
    Task<object> GetAnalyticsAsync(Guid configId, Guid tenantId);
    Task<object> GetTrainingDataAsync(Guid configId, Guid tenantId);
    Task<bool> TrainChatbotAsync(Guid configId, Guid tenantId);
    Task<object> GetKnowledgeBaseAsync(Guid configId, Guid tenantId);
    Task<bool> AddKnowledgeBaseDocumentAsync(Guid configId, IFormFile document, Guid tenantId);
    Task<bool> RemoveKnowledgeBaseDocumentAsync(Guid configId, Guid documentId, Guid tenantId);
}

public interface ITeamService
{
    Task<TeamMemberDto> AddTeamMemberAsync(AddTeamMemberRequest request, Guid tenantId);
    Task<List<TeamMemberDto>> GetTeamMembersAsync(Guid tenantId);
    Task<TeamMemberDto?> UpdateTeamMemberAsync(Guid id, UpdateTeamMemberRequest request, Guid tenantId);
    Task<bool> RemoveTeamMemberAsync(Guid id, Guid tenantId);
    Task<List<object>> GetTeamRolesAsync();
    Task<object> GetTeamStatsAsync(Guid tenantId);
    Task<object> GetTeamPermissionsAsync();
    Task<bool> ResendInvitationAsync(Guid id, Guid tenantId);
    Task<bool> CancelInvitationAsync(Guid id, Guid tenantId);
    Task<bool> BulkUpdateMembersAsync(BulkUpdateMembersRequest request, Guid tenantId);
    Task<List<object>> ExportTeamDataAsync(Guid tenantId);
}
