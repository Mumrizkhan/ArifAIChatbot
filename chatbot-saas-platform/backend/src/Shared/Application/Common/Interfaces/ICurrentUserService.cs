namespace Shared.Application.Common.Interfaces;

public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? Email { get; }
    bool IsAuthenticated { get; }
    Guid? TenantId { get; }
    string? Role { get; }
}
