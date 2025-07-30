namespace Shared.Infrastructure.Dashboard;

public interface IDashboardAuthorizationFilter
{
    bool Authorize(object context);
}
