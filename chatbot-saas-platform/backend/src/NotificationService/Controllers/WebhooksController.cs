using Microsoft.AspNetCore.Mvc;
using NotificationService.Services;
using Hangfire;

namespace NotificationService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WebhooksController : ControllerBase
{
    private readonly IEmailService _emailService;
    private readonly ILogger<WebhooksController> _logger;

    public WebhooksController(
        IEmailService emailService,
        ILogger<WebhooksController> logger)
    {
        _emailService = emailService;
        _logger = logger;
    }

    /// <summary>
    /// Handle SendGrid webhook events for email delivery tracking
    /// </summary>
    [HttpPost("sendgrid")]
    public async Task<IActionResult> HandleSendGridWebhook()
    {
        try
        {
            var requestBody = await new StreamReader(Request.Body).ReadToEndAsync();
            
            if (string.IsNullOrEmpty(requestBody))
            {
                _logger.LogWarning("Received empty SendGrid webhook payload");
                return BadRequest("Empty payload");
            }

            _logger.LogInformation("Received SendGrid webhook with {Length} characters", requestBody.Length);

            // Process webhook asynchronously using Hangfire
            BackgroundJob.Enqueue<IEmailService>(service => service.ProcessSendGridWebhookAsync(requestBody));

            return Ok(new { message = "Webhook received and queued for processing" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling SendGrid webhook");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Handle Twilio webhook events for SMS delivery tracking
    /// </summary>
    [HttpPost("twilio")]
    public async Task<IActionResult> HandleTwilioWebhook()
    {
        try
        {
            var requestBody = await new StreamReader(Request.Body).ReadToEndAsync();
            
            if (string.IsNullOrEmpty(requestBody))
            {
                _logger.LogWarning("Received empty Twilio webhook payload");
                return BadRequest("Empty payload");
            }

            _logger.LogInformation("Received Twilio webhook with {Length} characters", requestBody.Length);

            // Process webhook asynchronously using Hangfire
            BackgroundJob.Enqueue<WebhooksController>(controller => controller.ProcessTwilioWebhookAsync(requestBody));

            return Ok(new { message = "Webhook received and queued for processing" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling Twilio webhook");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Process Twilio webhook data using Hangfire background job
    /// </summary>
    [Queue("webhook-processing")]
    public async Task ProcessTwilioWebhookAsync(string webhookData)
    {
        try
        {
            _logger.LogInformation("Processing Twilio webhook data");

            // Parse webhook data and update SMS notification statuses
            // Implementation would parse the form data from Twilio
            // and update notification statuses based on message status
            
            _logger.LogInformation("Twilio webhook processing completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Twilio webhook");
        }
    }

    /// <summary>
    /// Test webhook endpoint for development
    /// </summary>
    [HttpPost("test")]
    public async Task<IActionResult> TestWebhook([FromBody] object payload)
    {
        try
        {
            _logger.LogInformation("Received test webhook: {Payload}", payload);
            
            // Process test webhook with Hangfire
            BackgroundJob.Enqueue<WebhooksController>(controller => controller.ProcessTestWebhookAsync(payload.ToString() ?? ""));

            return Ok(new { 
                message = "Test webhook received",
                timestamp = DateTime.UtcNow,
                payload = payload
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling test webhook");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Process test webhook data using Hangfire background job
    /// </summary>
    [Queue("webhook-processing")]
    public async Task ProcessTestWebhookAsync(string webhookData)
    {
        try
        {
            _logger.LogInformation("Processing test webhook data: {Data}", webhookData);
            
            // Simulate webhook processing
            await Task.Delay(1000);
            
            _logger.LogInformation("Test webhook processing completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing test webhook");
        }
    }

    /// <summary>
    /// Get webhook processing statistics
    /// </summary>
    [HttpGet("statistics")]
    public IActionResult GetWebhookStatistics()
    {
        try
        {
            // In a real implementation, you would query your database
            // for webhook processing statistics
            
            return Ok(new
            {
                sendGridWebhooks = new
                {
                    total = 0,
                    processed = 0,
                    failed = 0,
                    lastProcessed = (DateTime?)null
                },
                twilioWebhooks = new
                {
                    total = 0,
                    processed = 0,
                    failed = 0,
                    lastProcessed = (DateTime?)null
                },
                testWebhooks = new
                {
                    total = 0,
                    processed = 0,
                    failed = 0,
                    lastProcessed = (DateTime?)null
                },
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting webhook statistics");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }
}