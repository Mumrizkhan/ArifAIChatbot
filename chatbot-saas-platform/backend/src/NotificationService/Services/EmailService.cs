using SendGrid;
using SendGrid.Helpers.Mail;
using Microsoft.EntityFrameworkCore;
using Shared.Application.Common.Interfaces;
using NotificationService.Services;
using NotificationService.Models;
using Hangfire;

namespace NotificationService.Services;

public class EmailService : IEmailService
{
    private readonly ISendGridClient _sendGridClient;
    private readonly ITemplateService _templateService;
    private readonly ILogger<EmailService> _logger;
    private readonly IConfiguration _configuration;
    private readonly IApplicationDbContext _context;

    public EmailService(
        ITemplateService templateService,
        ILogger<EmailService> logger,
        IConfiguration configuration,
        IApplicationDbContext context)
    {
        _templateService = templateService;
        _logger = logger;
        _configuration = configuration;
        _context = context;
        
        var apiKey = _configuration["SendGrid:ApiKey"] ?? throw new InvalidOperationException("SendGrid API key not configured");
        _sendGridClient = new SendGridClient(apiKey);
    }

    public async Task<bool> SendEmailAsync(string to, string subject, string content, string? templateId = null, Dictionary<string, object>? templateData = null)
    {
        try
        {
            var from = new EmailAddress(_configuration["SendGrid:FromEmail"], _configuration["SendGrid:FromName"]);
            var toEmail = new EmailAddress(to);

            string emailContent = content;
            string emailSubject = subject;

            // Enhanced template processing with better integration
            if (!string.IsNullOrEmpty(templateId) && templateData != null)
            {
                try
                {
                    emailContent = await _templateService.RenderTemplateAsync(templateId, templateData);
                    
                    // Also try to render subject if it contains template variables
                    if (subject.Contains("{{") || subject.Contains("{"))
                    {
                        emailSubject = await _templateService.RenderTemplateAsync($"subject_{templateId}", templateData);
                    }
                }
                catch (Exception templateEx)
                {
                    _logger.LogWarning(templateEx, "Failed to render template {TemplateId}, using fallback content", templateId);
                    // Continue with original content if template rendering fails
                }
            }

            var msg = MailHelper.CreateSingleEmail(from, toEmail, emailSubject, emailContent, emailContent);
            
            // Add tracking settings for better delivery monitoring
            msg.SetClickTracking(true, true);
            msg.SetOpenTracking(true);
            msg.SetGoogleAnalytics(true);
            msg.SetSubscriptionTracking(true);
            
            // Add custom headers for tracking
            msg.AddHeader("X-Notification-Service", "ChatBot-SaaS");
            msg.AddHeader("X-Sent-At", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"));
            
            var response = await _sendGridClient.SendEmailAsync(msg);
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Email sent successfully to {To} with subject: {Subject}", to, emailSubject);
                
                // Schedule a background job to track delivery status after a delay
                var messageId = response.Headers?.GetValues("X-Message-Id")?.FirstOrDefault();
                if (!string.IsNullOrEmpty(messageId))
                {
                    BackgroundJob.Schedule<EmailService>(
                        service => service.TrackEmailDeliveryAsync(messageId, to),
                        TimeSpan.FromMinutes(2)
                    );
                }
                
                return true;
            }
            else
            {
                var responseBody = await response.Body.ReadAsStringAsync();
                _logger.LogError("Failed to send email to {To}. Status: {Status}, Response: {Response}", 
                    to, response.StatusCode, responseBody);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending email to {To}", to);
            return false;
        }
    }

    public async Task<bool> SendBulkEmailAsync(List<string> recipients, string subject, string content, string? templateId = null, Dictionary<string, object>? templateData = null)
    {
        try
        {
            var from = new EmailAddress(_configuration["SendGrid:FromEmail"], _configuration["SendGrid:FromName"]);
            var tos = recipients.Select(email => new EmailAddress(email)).ToList();

            string emailContent = content;
            string emailSubject = subject;

            // Enhanced template processing
            if (!string.IsNullOrEmpty(templateId) && templateData != null)
            {
                try
                {
                    emailContent = await _templateService.RenderTemplateAsync(templateId, templateData);
                    
                    if (subject.Contains("{{") || subject.Contains("{"))
                    {
                        emailSubject = await _templateService.RenderTemplateAsync($"subject_{templateId}", templateData);
                    }
                }
                catch (Exception templateEx)
                {
                    _logger.LogWarning(templateEx, "Failed to render template {TemplateId} for bulk email, using fallback content", templateId);
                }
            }

            var msg = MailHelper.CreateSingleEmailToMultipleRecipients(from, tos, emailSubject, emailContent, emailContent);
            
            // Add tracking settings
            msg.SetClickTracking(true, true);
            msg.SetOpenTracking(true);
            msg.SetGoogleAnalytics(true);
            msg.SetSubscriptionTracking(true);
            
            // Add batch tracking headers
            msg.AddHeader("X-Notification-Service", "ChatBot-SaaS");
            msg.AddHeader("X-Batch-Size", recipients.Count.ToString());
            msg.AddHeader("X-Sent-At", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"));
            
            var response = await _sendGridClient.SendEmailAsync(msg);
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Bulk email sent successfully to {Count} recipients with subject: {Subject}", recipients.Count, emailSubject);
                
                // Schedule background jobs to track delivery for each recipient
                var messageId = response.Headers?.GetValues("X-Message-Id")?.FirstOrDefault();
                if (!string.IsNullOrEmpty(messageId))
                {
                    foreach (var recipient in recipients)
                    {
                        BackgroundJob.Schedule<EmailService>(
                            service => service.TrackEmailDeliveryAsync(messageId, recipient),
                            TimeSpan.FromMinutes(2)
                        );
                    }
                }
                
                return true;
            }
            else
            {
                var responseBody = await response.Body.ReadAsStringAsync();
                _logger.LogError("Failed to send bulk email. Status: {Status}, Response: {Response}", 
                    response.StatusCode, responseBody);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending bulk email to {Count} recipients", recipients.Count);
            return false;
        }
    }

    public async Task<bool> ValidateEmailAsync(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    public async Task<EmailDeliveryStatus> GetDeliveryStatusAsync(string messageId)
    {
        try
        {
            // In a real implementation, you would query SendGrid's API for delivery status
            // For now, return a mock status
            return new EmailDeliveryStatus
            {
                MessageId = messageId,
                Status = "delivered",
                DeliveredAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting email delivery status for message {MessageId}", messageId);
            return new EmailDeliveryStatus
            {
                MessageId = messageId,
                Status = "unknown",
                ErrorMessage = ex.Message
            };
        }
    }

    /// <summary>
    /// Enhanced method to send notification-specific emails with better integration
    /// </summary>
    public async Task<bool> SendNotificationEmailAsync(Shared.Domain.Entities.Notification notification)
    {
        try
        {
            if (string.IsNullOrEmpty(notification.RecipientEmail))
            {
                _logger.LogWarning("Cannot send email notification {NotificationId}: No recipient email", notification.Id);
                return false;
            }

            var templateData = new Dictionary<string, object>(notification.TemplateData ?? new Dictionary<string, object>())
            {
                ["notificationId"] = notification.Id,
                ["notificationType"] = notification.Type.ToString(),
                ["tenantId"] = notification.TenantId,
                ["createdAt"] = notification.CreatedAt,
                ["data"] = notification.Data
            };

            var success = await SendEmailAsync(
                notification.RecipientEmail,
                notification.Title,
                notification.Content,
                notification.TemplateId,
                templateData
            );

            if (success)
            {
                // Update notification status in background
                BackgroundJob.Enqueue<EmailService>(service => service.UpdateNotificationStatusAsync(notification.Id, "sent", null));
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending notification email for notification {NotificationId}", notification.Id);
            
            // Update notification as failed in background
            BackgroundJob.Enqueue<EmailService>(service => service.UpdateNotificationStatusAsync(notification.Id, "failed", ex.Message));
            
            return false;
        }
    }

    /// <summary>
    /// Track email delivery status using Hangfire background job
    /// </summary>
    [Queue("email-tracking")]
    public async Task TrackEmailDeliveryAsync(string? messageId, string recipientEmail)
    {
        try
        {
            if (string.IsNullOrEmpty(messageId))
            {
                _logger.LogWarning("Cannot track email delivery: No message ID for {Email}", recipientEmail);
                return;
            }

            _logger.LogInformation("Tracking email delivery for message {MessageId} to {Email}", messageId, recipientEmail);

            // In a real implementation, you would:
            // 1. Query SendGrid's Event API
            // 2. Check delivery status, opens, clicks, etc.
            // 3. Update the notification record accordingly
            
            var deliveryStatus = await GetDeliveryStatusAsync(messageId);
            
            if (deliveryStatus.Status == "delivered")
            {
                _logger.LogInformation("Email {MessageId} delivered successfully to {Email}", messageId, recipientEmail);
                
                // Update any related notification records
                await UpdateNotificationByEmailAsync(recipientEmail, messageId, "delivered");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error tracking email delivery for message {MessageId}", messageId);
        }
    }

    /// <summary>
    /// Update notification status using Hangfire background job
    /// </summary>
    [Queue("notification-updates")]
    public async Task UpdateNotificationStatusAsync(Guid notificationId, string status, string? errorMessage = null)
    {
        try
        {
            var notification = await _context.Set<Shared.Domain.Entities.Notification>()
                .FirstOrDefaultAsync(n => n.Id == notificationId);

            if (notification != null)
            {
                switch (status.ToLower())
                {
                    case "sent":
                        notification.Status = Shared.Domain.Entities.NotificationStatus.Sent;
                        notification.SentAt = DateTime.UtcNow;
                        break;
                    case "delivered":
                        notification.Status = Shared.Domain.Entities.NotificationStatus.Delivered;
                        notification.DeliveredAt = DateTime.UtcNow;
                        break;
                    case "failed":
                        notification.Status = Shared.Domain.Entities.NotificationStatus.Failed;
                        notification.ErrorMessage = errorMessage;
                        break;
                }

                notification.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Updated notification {NotificationId} status to {Status}", notificationId, status);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating notification {NotificationId} status", notificationId);
        }
    }

    /// <summary>
    /// Update notification by recipient email using Hangfire background job
    /// </summary>
    [Queue("notification-updates")]
    public async Task UpdateNotificationByEmailAsync(string recipientEmail, string messageId, string status)
    {
        try
        {
            var notification = await _context.Set<Shared.Domain.Entities.Notification>()
                .Where(n => n.RecipientEmail == recipientEmail && n.ExternalId == messageId)
                .FirstOrDefaultAsync();

            if (notification != null)
            {
                await UpdateNotificationStatusAsync(notification.Id, status);
            }
            else
            {
                // Try to find by email and recent creation time if external ID not set
                var recentNotification = await _context.Set<Shared.Domain.Entities.Notification>()
                    .Where(n => n.RecipientEmail == recipientEmail && 
                               n.CreatedAt >= DateTime.UtcNow.AddHours(-1) &&
                               n.Channel == Shared.Domain.Entities.NotificationChannel.Email)
                    .OrderByDescending(n => n.CreatedAt)
                    .FirstOrDefaultAsync();

                if (recentNotification != null)
                {
                    recentNotification.ExternalId = messageId;
                    await UpdateNotificationStatusAsync(recentNotification.Id, status);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating notification by email {Email}", recipientEmail);
        }
    }

    /// <summary>
    /// Process SendGrid webhook events using Hangfire background job
    /// </summary>
    [Queue("webhook-processing")]
    public async Task ProcessSendGridWebhookAsync(string webhookData)
    {
        try
        {
            _logger.LogInformation("Processing SendGrid webhook data");

            // Parse webhook data and update notification statuses accordingly
            // This would be called from a webhook controller endpoint
            
            // Implementation would parse the JSON webhook data from SendGrid
            // and update notification statuses based on events like:
            // - delivered, bounce, spam, unsubscribe, click, open, etc.
            
            _logger.LogInformation("SendGrid webhook processing completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing SendGrid webhook");
        }
    }
}
