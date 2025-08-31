using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shared.Application.Common.Interfaces;
using Shared.Domain.Events;

namespace Shared.Infrastructure.Services;

public class SystemMonitoringService : BackgroundService
{
    private readonly IMessageBusNotificationService _notificationService;
    private readonly ILogger<SystemMonitoringService> _logger;

    public SystemMonitoringService(
        IMessageBusNotificationService notificationService,
        ILogger<SystemMonitoringService> logger)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("System Monitoring Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckSystemHealth();
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in system monitoring");
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }

    private async Task CheckSystemHealth()
    {
        try
        {
            // Example: Check memory usage
            var memoryUsage = GC.GetTotalMemory(false);
            var memoryThreshold = 1024 * 1024 * 1024; // 1GB

            if (memoryUsage > memoryThreshold)
            {
                await PublishSystemAlert("HighMemoryUsage", "High Memory Usage Detected",
                    $"Current memory usage: {memoryUsage / (1024 * 1024)} MB", "Warning",
                    new Dictionary<string, object>
                    {
                        ["currentMemoryMB"] = memoryUsage / (1024 * 1024),
                        ["thresholdMB"] = memoryThreshold / (1024 * 1024)
                    });
            }

            // Example: Check available disk space
            var drives = DriveInfo.GetDrives();
            foreach (var drive in drives.Where(d => d.IsReady))
            {
                var freeSpacePercent = (double)drive.AvailableFreeSpace / drive.TotalSize * 100;
                if (freeSpacePercent < 10) // Less than 10% free space
                {
                    await PublishSystemAlert("LowDiskSpace", "Low Disk Space Warning",
                        $"Drive {drive.Name} has only {freeSpacePercent:F1}% free space remaining", "Warning",
                        new Dictionary<string, object>
                        {
                            ["driveName"] = drive.Name,
                            ["freeSpacePercent"] = freeSpacePercent,
                            ["freeSpaceGB"] = drive.AvailableFreeSpace / (1024 * 1024 * 1024),
                            ["totalSizeGB"] = drive.TotalSize / (1024 * 1024 * 1024)
                        });
                }
            }

            _logger.LogDebug("System health check completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during system health check");
        }
    }

    private async Task PublishSystemAlert(string alertType, string title, string description, 
        string severity, Dictionary<string, object> metadata)
    {
        try
        {
            var systemAlert = new SystemAlertEvent
            {
                AlertType = alertType,
                Title = title,
                Description = description,
                Severity = severity,
                TenantId = Guid.Empty, // System-wide alert
                Metadata = metadata
            };

            await _notificationService.PublishSystemAlertAsync(systemAlert);
            _logger.LogWarning("System alert published: {AlertType} - {Title}", alertType, title);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish system alert: {AlertType}", alertType);
        }
    }

    public async Task PublishCustomAlert(Guid tenantId, string alertType, string title, 
        string description, string severity = "Info", Dictionary<string, object>? metadata = null)
    {
        var systemAlert = new SystemAlertEvent
        {
            AlertType = alertType,
            Title = title,
            Description = description,
            Severity = severity,
            TenantId = tenantId,
            Metadata = metadata ?? new Dictionary<string, object>()
        };

        await _notificationService.PublishSystemAlertAsync(systemAlert);
        _logger.LogInformation("Custom alert published for tenant {TenantId}: {AlertType}", 
            tenantId, alertType);
    }
}