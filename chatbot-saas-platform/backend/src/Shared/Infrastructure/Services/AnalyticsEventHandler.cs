using Microsoft.Extensions.Logging;
using AnalyticsService.Services;

namespace Shared.Infrastructure.Services;

/// <summary>
/// Service that handles analytics events from RabbitMQ using the message bus service pattern
/// Updated to use AnalyticsMessageBusService instead of direct event handling
/// </summary>
public interface IAnalyticsEventHandler
{
    Task StartAsync(CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Implementation of analytics event handler using the message bus service
/// </summary>
public class AnalyticsEventHandler : IAnalyticsEventHandler
{
    private readonly IAnalyticsMessageBusService _messageBusService;
    private readonly ILogger<AnalyticsEventHandler> _logger;

    public AnalyticsEventHandler(
        IAnalyticsMessageBusService messageBusService,
        ILogger<AnalyticsEventHandler> logger)
    {
        _messageBusService = messageBusService;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Initialize all analytics event subscriptions through the message bus service
            _messageBusService.InitializeSubscriptions();

            _logger.LogInformation("Analytics event handler started successfully using message bus service");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start analytics event handler");
            throw;
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // The message bus service handles cleanup internally
            _logger.LogInformation("Analytics event handler stopped successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping analytics event handler");
        }
    }
}

/// <summary>
/// Service to start the analytics event handler via Hangfire jobs
/// </summary>
public class AnalyticsEventHandlerService
{
    private readonly IAnalyticsEventHandler _eventHandler;
    private readonly ILogger<AnalyticsEventHandlerService> _logger;

    public AnalyticsEventHandlerService(
        IAnalyticsEventHandler eventHandler,
        ILogger<AnalyticsEventHandlerService> logger)
    {
        _eventHandler = eventHandler;
        _logger = logger;
    }

    /// <summary>
    /// Starts the analytics event handler - to be called by Hangfire
    /// </summary>
    public async Task StartAnalyticsEventHandlerAsync()
    {
        try
        {
            _logger.LogInformation("Starting analytics event handler from Hangfire job");
            await _eventHandler.StartAsync();
            _logger.LogInformation("Analytics event handler started successfully from Hangfire job");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Analytics event handler service encountered an error in Hangfire job");
            throw;
        }
    }

    /// <summary>
    /// Stops the analytics event handler - to be called by Hangfire if needed
    /// </summary>
    public async Task StopAnalyticsEventHandlerAsync()
    {
        try
        {
            _logger.LogInformation("Stopping analytics event handler from Hangfire job");
            await _eventHandler.StopAsync();
            _logger.LogInformation("Analytics event handler stopped successfully from Hangfire job");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping analytics event handler in Hangfire job");
            throw;
        }
    }
}
