using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Hangfire;
using NotificationService.Services;
using Shared.Domain.Events;
using Shared.Domain.Entities;

namespace NotificationService.Controllers;

[ApiController]
[Route("api/[controller]")]
[AllowAnonymous] // For testing purposes only
public class HangfireTestController : ControllerBase
{
    private readonly ILogger<HangfireTestController> _logger;

    public HangfireTestController(ILogger<HangfireTestController> logger)
    {
        _logger = logger;
    }

    [HttpPost("enqueue-test")]
    public IActionResult EnqueueTestJob()
    {
        var jobId = BackgroundJob.Enqueue(() => TestJob("Hello from Hangfire!"));
        
        return Ok(new { 
            message = "Test job enqueued successfully",
            jobId = jobId,
            timestamp = DateTime.UtcNow
        });
    }

    [HttpPost("schedule-test")]
    public IActionResult ScheduleTestJob([FromQuery] int delayMinutes = 1)
    {
        var jobId = BackgroundJob.Schedule(() => TestJob($"Scheduled job executed at {DateTime.UtcNow}"), 
            TimeSpan.FromMinutes(delayMinutes));
        
        return Ok(new { 
            message = $"Test job scheduled to run in {delayMinutes} minute(s)",
            jobId = jobId,
            timestamp = DateTime.UtcNow
        });
    }

    [HttpPost("recurring-test")]
    public IActionResult SetupRecurringTestJob()
    {
        RecurringJob.AddOrUpdate(
            "test-recurring-job",
            () => TestJob("Recurring job execution"),
            "*/2 * * * *" // Every 2 minutes
        );
        
        return Ok(new { 
            message = "Recurring test job set up (every 2 minutes)",
            timestamp = DateTime.UtcNow
        });
    }

    [HttpDelete("recurring-test")]
    public IActionResult RemoveRecurringTestJob()
    {
        RecurringJob.RemoveIfExists("test-recurring-job");
        
        return Ok(new { 
            message = "Recurring test job removed",
            timestamp = DateTime.UtcNow
        });
    }

    [HttpPost("notification-processing-test")]
    public IActionResult TestNotificationProcessing()
    {
        // Test notification event processing
        var notificationEvent = new NotificationCreatedEvent
        {
            NotificationId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            Title = "Test Hangfire Notification",
            Content = "This notification was processed by Hangfire",
            Type = NotificationType.SystemAlert,
            Channels = new List<NotificationChannel> { NotificationChannel.InApp },
            Data = new Dictionary<string, object>
            {
                ["testMode"] = true,
                ["processedBy"] = "Hangfire"
            },
            Language = "en"
        };

        var jobId = BackgroundJob.Enqueue<NotificationMessageBusService>(
            service => service.ProcessNotificationCreatedAsync(notificationEvent));

        return Ok(new { 
            message = "Notification processing job enqueued",
            jobId = jobId,
            notificationId = notificationEvent.NotificationId,
            timestamp = DateTime.UtcNow
        });
    }

    [HttpPost("bulk-notification-test")]
    public IActionResult TestBulkNotificationProcessing()
    {
        // Test bulk notification event processing
        var bulkEvent = new BulkNotificationEvent
        {
            TenantId = Guid.NewGuid(),
            UserIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() },
            Title = "Test Bulk Notification",
            Content = "This bulk notification was processed by Hangfire",
            Type = NotificationType.SystemAlert,
            Channels = new List<NotificationChannel> { NotificationChannel.InApp },
            Data = new Dictionary<string, object>
            {
                ["testMode"] = true,
                ["processedBy"] = "Hangfire",
                ["batchSize"] = 3
            },
            Language = "en"
        };

        var jobId = BackgroundJob.Enqueue<NotificationMessageBusService>(
            service => service.ProcessBulkNotificationAsync(bulkEvent));

        return Ok(new { 
            message = "Bulk notification processing job enqueued",
            jobId = jobId,
            recipientCount = bulkEvent.UserIds.Count,
            timestamp = DateTime.UtcNow
        });
    }

    [HttpPost("maintenance-jobs-test")]
    public IActionResult TestMaintenanceJobs()
    {
        // Enqueue cleanup job
        var cleanupJobId = BackgroundJob.Enqueue<NotificationMessageBusService>(
            service => service.CleanupOldNotificationsAsync());

        // Enqueue retry job
        var retryJobId = BackgroundJob.Enqueue<NotificationMessageBusService>(
            service => service.RetryFailedNotificationsAsync());

        // Enqueue scheduled processing job
        var scheduledJobId = BackgroundJob.Enqueue<NotificationMessageBusService>(
            service => service.ProcessScheduledNotificationsAsync());

        return Ok(new { 
            message = "Maintenance jobs enqueued",
            jobs = new
            {
                cleanupJobId,
                retryJobId,
                scheduledJobId
            },
            timestamp = DateTime.UtcNow
        });
    }

    [HttpGet("job-status/{jobId}")]
    public IActionResult GetJobStatus(string jobId)
    {
        try
        {
            var api = JobStorage.Current.GetMonitoringApi();
            var jobDetails = api.JobDetails(jobId);

            if (jobDetails == null)
            {
                return NotFound(new { message = "Job not found" });
            }

            return Ok(new
            {
                jobId = jobId,
                state = jobDetails.History.LastOrDefault()?.StateName ?? "Unknown",
                createdAt = jobDetails.CreatedAt,
                properties = jobDetails.Properties,
                history = jobDetails.History.Select(h => new
                {
                    stateName = h.StateName,
                    createdAt = h.CreatedAt,
                    reason = h.Reason,
                    data = h.Data
                }).ToList()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting job status for {JobId}", jobId);
            return StatusCode(500, new { message = "Error retrieving job status" });
        }
    }

    [HttpGet("queue-stats")]
    public IActionResult GetQueueStats()
    {
        try
        {
            var api = JobStorage.Current.GetMonitoringApi();
            var statistics = api.GetStatistics();

            return Ok(new
            {
                enqueued = statistics.Enqueued,
                processing = statistics.Processing,
                succeeded = statistics.Succeeded,
                failed = statistics.Failed,
                scheduled = statistics.Scheduled,
                recurring = statistics.Recurring,
                servers = statistics.Servers,
                queues = statistics.Queues,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting queue statistics");
            return StatusCode(500, new { message = "Error retrieving queue statistics" });
        }
    }

    // This is the actual job method that gets executed by Hangfire
    public void TestJob(string message)
    {
        _logger.LogInformation("Hangfire Test Job executed: {Message}", message);
        
        // Simulate some work
        Thread.Sleep(2000);
        
        _logger.LogInformation("Hangfire Test Job completed: {Message}", message);
    }
}