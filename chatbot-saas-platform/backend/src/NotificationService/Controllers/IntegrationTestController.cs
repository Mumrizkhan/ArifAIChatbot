using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Hangfire;
using NotificationService.Services;
using NotificationService.Models;
using Shared.Domain.Events;
using Shared.Domain.Entities;
using Shared.Application.Common.Interfaces;

namespace NotificationService.Controllers;

[ApiController]
[Route("api/[controller]")]
[AllowAnonymous] // For testing purposes only
public class IntegrationTestController : ControllerBase
{
    private readonly INotificationService _notificationService;
    private readonly IEmailService _emailService;
    private readonly IMessageBusNotificationService _messageBusNotificationService;
    private readonly ILogger<IntegrationTestController> _logger;

    public IntegrationTestController(
        INotificationService notificationService,
        IEmailService emailService,
        IMessageBusNotificationService messageBusNotificationService,
        ILogger<IntegrationTestController> logger)
    {
        _notificationService = notificationService;
        _emailService = emailService;
        _messageBusNotificationService = messageBusNotificationService;
        _logger = logger;
    }

    /// <summary>
    /// Test complete notification flow from API to delivery
    /// </summary>
    [HttpPost("complete-flow")]
    public async Task<IActionResult> TestCompleteNotificationFlow([FromBody] TestCompleteFlowRequest request)
    {
        try
        {
            var testResults = new List<object>();
            var tenantId = request.TenantId ?? Guid.NewGuid();

            // 1. Test Direct API Notification
            _logger.LogInformation("Testing direct API notification...");
            var apiNotificationId = await _notificationService.SendNotificationAsync(new SendNotificationRequest
            {
                Title = "API Test Notification",
                Content = "This notification was sent directly through the API",
                Type = NotificationType.SystemAlert,
                Channels = new List<NotificationChannel> { NotificationChannel.InApp, NotificationChannel.Email },
                UserId = request.UserId,
                RecipientEmail = request.TestEmail,
                Data = new Dictionary<string, object>
                {
                    ["testType"] = "direct_api",
                    ["timestamp"] = DateTime.UtcNow
                }
            }, tenantId);

            testResults.Add(new
            {
                test = "Direct API Notification",
                success = true,
                notificationId = apiNotificationId,
                message = "Direct API notification sent successfully"
            });

            // 2. Test Message Bus Event
            _logger.LogInformation("Testing message bus event notification...");
            var messageBusEvent = new NotificationCreatedEvent
            {
                NotificationId = Guid.NewGuid(),
                UserId = request.UserId,
                TenantId = tenantId,
                Title = "Message Bus Test Notification",
                Content = "This notification was sent through the message bus system",
                Type = NotificationType.Welcome,
                Channels = new List<NotificationChannel> { NotificationChannel.Email, NotificationChannel.InApp },
                RecipientEmail = request.TestEmail,
                Data = new Dictionary<string, object>
                {
                    ["testType"] = "message_bus",
                    ["timestamp"] = DateTime.UtcNow
                }
            };

            await _messageBusNotificationService.PublishNotificationAsync(messageBusEvent);

            testResults.Add(new
            {
                test = "Message Bus Event",
                success = true,
                notificationId = messageBusEvent.NotificationId,
                message = "Message bus event published successfully"
            });

            // 3. Test Hangfire Background Job
            _logger.LogInformation("Testing Hangfire background job...");
            var hangfireJobId = BackgroundJob.Enqueue<IntegrationTestController>(
                controller => controller.TestHangfireNotificationJob(request.TestEmail ?? "", tenantId));

            testResults.Add(new
            {
                test = "Hangfire Background Job",
                success = true,
                jobId = hangfireJobId,
                message = "Hangfire background job enqueued successfully"
            });

            // 4. Test Bulk Notification
            if (request.TestBulkEmails?.Any() == true)
            {
                _logger.LogInformation("Testing bulk notification...");
                var bulkNotificationIds = await _notificationService.SendBulkNotificationAsync(new BulkNotificationRequest
                {
                    Title = "Bulk Test Notification",
                    Content = "This is a bulk notification test",
                    Type = NotificationType.SystemAlert,
                    Channels = new List<NotificationChannel> { NotificationChannel.Email },
                    RecipientEmails = request.TestBulkEmails,
                    Data = new Dictionary<string, object>
                    {
                        ["testType"] = "bulk",
                        ["timestamp"] = DateTime.UtcNow,
                        ["batchSize"] = request.TestBulkEmails.Count
                    }
                }, tenantId);

                testResults.Add(new
                {
                    test = "Bulk Notification",
                    success = true,
                    notificationIds = bulkNotificationIds,
                    count = bulkNotificationIds.Count,
                    message = "Bulk notification sent successfully"
                });
            }

            // 5. Test Template Processing
            if (!string.IsNullOrEmpty(request.TemplateId))
            {
                _logger.LogInformation("Testing template processing...");
                var templateNotificationId = await _notificationService.SendNotificationAsync(new SendNotificationRequest
                {
                    Title = "Template Test: {{userName}} Welcome",
                    Content = "Template content will be replaced",
                    Type = NotificationType.Welcome,
                    Channels = new List<NotificationChannel> { NotificationChannel.Email },
                    RecipientEmail = request.TestEmail,
                    TemplateId = request.TemplateId,
                    TemplateData = new Dictionary<string, object>
                    {
                        ["userName"] = request.UserName ?? "Test User",
                        ["testType"] = "template",
                        ["timestamp"] = DateTime.UtcNow
                    }
                }, tenantId);

                testResults.Add(new
                {
                    test = "Template Processing",
                    success = true,
                    notificationId = templateNotificationId,
                    templateId = request.TemplateId,
                    message = "Template notification sent successfully"
                });
            }

            // 6. Test Scheduled Notification
            _logger.LogInformation("Testing scheduled notification...");
            var scheduledTime = DateTime.UtcNow.AddMinutes(2);
            var scheduledNotificationId = await _notificationService.SendNotificationAsync(new SendNotificationRequest
            {
                Title = "Scheduled Test Notification",
                Content = "This notification was scheduled for delivery",
                Type = NotificationType.SystemAlert,
                Channels = new List<NotificationChannel> { NotificationChannel.Email },
                RecipientEmail = request.TestEmail,
                ScheduledAt = scheduledTime,
                Data = new Dictionary<string, object>
                {
                    ["testType"] = "scheduled",
                    ["scheduledFor"] = scheduledTime
                }
            }, tenantId);

            testResults.Add(new
            {
                test = "Scheduled Notification",
                success = true,
                notificationId = scheduledNotificationId,
                scheduledFor = scheduledTime,
                message = "Scheduled notification created successfully"
            });

            return Ok(new
            {
                message = "Complete notification flow test completed",
                testResults = testResults,
                summary = new
                {
                    totalTests = testResults.Count,
                    successfulTests = testResults.Count(r => (bool)r.GetType().GetProperty("success")!.GetValue(r)!),
                    tenantId = tenantId,
                    timestamp = DateTime.UtcNow
                },
                instructions = new
                {
                    hangfireDashboard = "Check Hangfire dashboard at /hangfire for job status",
                    emailDelivery = "Check your email for test notifications",
                    scheduledDelivery = $"Scheduled notification will be sent at {scheduledTime}",
                    logs = "Check application logs for detailed processing information"
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during complete notification flow test");
            return StatusCode(500, new { message = "Test failed", error = ex.Message });
        }
    }

    /// <summary>
    /// Test email delivery tracking and webhook integration
    /// </summary>
    [HttpPost("email-tracking")]
    public async Task<IActionResult> TestEmailTracking([FromBody] TestEmailTrackingRequest request)
    {
        try
        {
            // Send test email with tracking
            var success = await _emailService.SendEmailAsync(
                request.TestEmail,
                "Email Tracking Test",
                "This email is being used to test delivery tracking and webhook integration.",
                templateData: new Dictionary<string, object>
                {
                    ["testType"] = "email_tracking",
                    ["timestamp"] = DateTime.UtcNow
                }
            );

            if (success)
            {
                // Schedule tracking job
                var trackingJobId = BackgroundJob.Schedule<IEmailService>(
                    service => service.TrackEmailDeliveryAsync("test-message-id", request.TestEmail),
                    TimeSpan.FromSeconds(30)
                );

                return Ok(new
                {
                    message = "Email tracking test initiated",
                    emailSent = success,
                    trackingJobId = trackingJobId,
                    testEmail = request.TestEmail,
                    instructions = new
                    {
                        tracking = "Delivery tracking will be processed in 30 seconds",
                        webhook = "Send test webhook data to /api/webhooks/test to simulate delivery events",
                        hangfire = "Monitor tracking job progress in Hangfire dashboard"
                    }
                });
            }
            else
            {
                return BadRequest(new { message = "Failed to send test email" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during email tracking test");
            return StatusCode(500, new { message = "Email tracking test failed", error = ex.Message });
        }
    }

    /// <summary>
    /// Test notification system performance with load testing
    /// </summary>
    [HttpPost("load-test")]
    public async Task<IActionResult> TestNotificationLoad([FromBody] TestLoadRequest request)
    {
        try
        {
            if (request.NotificationCount > 100)
            {
                return BadRequest(new { message = "Maximum 100 notifications allowed for load testing" });
            }

            var tenantId = request.TenantId ?? Guid.NewGuid();
            var jobIds = new List<string>();
            var startTime = DateTime.UtcNow;

            // Create multiple notifications using Hangfire for parallel processing
            for (int i = 0; i < request.NotificationCount; i++)
            {
                var jobId = BackgroundJob.Enqueue<IntegrationTestController>(
                    controller => controller.SendLoadTestNotificationAsync(
                        i, 
                        request.TestEmail, 
                        tenantId, 
                        request.UseTemplates));
                
                jobIds.Add(jobId);
            }

            return Ok(new
            {
                message = "Load test initiated",
                notificationCount = request.NotificationCount,
                jobIds = jobIds,
                tenantId = tenantId,
                startTime = startTime,
                estimatedCompletion = startTime.AddMinutes(2),
                instructions = new
                {
                    monitoring = "Monitor job progress in Hangfire dashboard at /hangfire",
                    performance = "Check application logs for performance metrics",
                    email = $"You should receive {request.NotificationCount} test emails"
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during load test");
            return StatusCode(500, new { message = "Load test failed", error = ex.Message });
        }
    }

    /// <summary>
    /// Hangfire job for notification testing
    /// </summary>
    [Queue("test-jobs")]
    public async Task TestHangfireNotificationJob(string testEmail, Guid tenantId)
    {
        try
        {
            _logger.LogInformation("Executing Hangfire notification test job");

            await _notificationService.SendNotificationAsync(new SendNotificationRequest
            {
                Title = "Hangfire Test Notification",
                Content = "This notification was processed by a Hangfire background job",
                Type = NotificationType.SystemAlert,
                Channels = new List<NotificationChannel> { NotificationChannel.Email },
                RecipientEmail = testEmail,
                Data = new Dictionary<string, object>
                {
                    ["processedBy"] = "hangfire",
                    ["jobExecutedAt"] = DateTime.UtcNow
                }
            }, tenantId);

            _logger.LogInformation("Hangfire notification test job completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Hangfire notification test job");
            throw;
        }
    }

    /// <summary>
    /// Load test notification job
    /// </summary>
    [Queue("load-test")]
    public async Task SendLoadTestNotificationAsync(int notificationIndex, string testEmail, Guid tenantId, bool useTemplates)
    {
        try
        {
            await _notificationService.SendNotificationAsync(new SendNotificationRequest
            {
                Title = $"Load Test Notification #{notificationIndex + 1}",
                Content = $"This is load test notification number {notificationIndex + 1} sent at {DateTime.UtcNow}",
                Type = NotificationType.SystemAlert,
                Channels = new List<NotificationChannel> { NotificationChannel.Email },
                RecipientEmail = testEmail,
                TemplateId = useTemplates ? "load_test_template" : null,
                Data = new Dictionary<string, object>
                {
                    ["loadTestIndex"] = notificationIndex,
                    ["batchProcessedAt"] = DateTime.UtcNow
                }
            }, tenantId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in load test notification {Index}", notificationIndex);
            throw;
        }
    }
}

public class TestCompleteFlowRequest
{
    public Guid? TenantId { get; set; }
    public Guid? UserId { get; set; }
    public string TestEmail { get; set; } = string.Empty;
    public List<string>? TestBulkEmails { get; set; }
    public string? TemplateId { get; set; }
    public string? UserName { get; set; }
}

public class TestEmailTrackingRequest
{
    public string TestEmail { get; set; } = string.Empty;
}

public class TestLoadRequest
{
    public Guid? TenantId { get; set; }
    public string TestEmail { get; set; } = string.Empty;
    public int NotificationCount { get; set; } = 10;
    public bool UseTemplates { get; set; } = false;
}