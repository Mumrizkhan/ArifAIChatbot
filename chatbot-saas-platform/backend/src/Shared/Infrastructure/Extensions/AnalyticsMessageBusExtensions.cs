using AnalyticsService.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shared.Infrastructure.Services;

namespace Shared.Infrastructure.Extensions;

/// <summary>
/// Extension methods for registering analytics message bus services
/// </summary>
public static class AnalyticsMessageBusExtensions
{
    /// <summary>
    /// Add analytics message bus services to the service collection
    /// </summary>
    public static IServiceCollection AddAnalyticsMessageBus(this IServiceCollection services)
    {
        // Register the analytics message bus service
        services.AddSingleton<IAnalyticsMessageBusService, AnalyticsMessageBusService>();
        
        // Register the analytics event handler
        services.AddSingleton<IAnalyticsEventHandler, AnalyticsEventHandler>();
        
        // Register the analytics event handler service for Hangfire
        services.AddScoped<AnalyticsEventHandlerService>();
        
        return services;
    }
}

/// <summary>
/// Extension methods for initializing analytics message bus
/// </summary>
public static class AnalyticsMessageBusApplicationExtensions
{
    /// <summary>
    /// Initialize analytics message bus subscriptions
    /// </summary>
    public static IHost InitializeAnalyticsMessageBus(this IHost host)
    {
        using var scope = host.Services.CreateScope();
        var messageBusService = scope.ServiceProvider.GetRequiredService<IAnalyticsMessageBusService>();
        
        // Initialize subscriptions
        messageBusService.InitializeSubscriptions();
        
        return host;
    }
}