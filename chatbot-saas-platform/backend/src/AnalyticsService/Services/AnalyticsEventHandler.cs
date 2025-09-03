using Microsoft.Extensions.Logging;
using AnalyticsService.Services;

namespace AnalyticsService.Services;

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
/// Background service to start the analytics event handler
/// </summary>
public class AnalyticsEventHandlerService : BackgroundService
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

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await _eventHandler.StartAsync(stoppingToken);
            
            // Keep the service running
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Analytics event handler service is stopping");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Analytics event handler service encountered an error");
            throw;
        }
        finally
        {
            await _eventHandler.StopAsync(stoppingToken);
        }
    }
}
