using Microsoft.AspNetCore.Http;
using Shared.Application.Common.Interfaces;

namespace Shared.Infrastructure.Services;

public class TenantService : ITenantService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly Dictionary<Guid, string> _tenantConnections = new();

    public TenantService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid GetCurrentTenantId()
    {
        var tenantIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst("tenant_id");
        if (tenantIdClaim != null && Guid.TryParse(tenantIdClaim.Value, out var tenantId))
        {
            return tenantId;
        }
        
        var tenantHeader = _httpContextAccessor.HttpContext?.Request?.Headers["X-Tenant-ID"].FirstOrDefault();
        if (tenantHeader != null && Guid.TryParse(tenantHeader, out var headerTenantId))
        {
            return headerTenantId;
        }

        return Guid.Empty;
    }

    public string GetCurrentTenantConnectionString()
    {
        var tenantId = GetCurrentTenantId();
        return GetTenantConnectionStringAsync(tenantId).Result;
    }

    public async Task<bool> TenantExistsAsync(Guid tenantId)
    {
        return _tenantConnections.ContainsKey(tenantId);//Todo check in cache if not found, get from db
    }

    public async Task<string> GetTenantConnectionStringAsync(Guid tenantId)
    {
        if (_tenantConnections.TryGetValue(tenantId, out var connectionString))
        {
            return connectionString;
        }

        var defaultConnection = "Server=localhost;Database=ChatBot_" + tenantId.ToString("N")[..8] + ";Trusted_Connection=true;TrustServerCertificate=true";
        _tenantConnections[tenantId] = defaultConnection;
        return defaultConnection;
    }
}
