namespace Shared.Application.Common.Interfaces;

public interface ITenantService
{
    Guid GetCurrentTenantId();
    string GetCurrentTenantConnectionString();
    Task<bool> TenantExistsAsync(Guid tenantId);
    Task<string> GetTenantConnectionStringAsync(Guid tenantId);
}
