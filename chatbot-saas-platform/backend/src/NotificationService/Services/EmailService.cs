using SendGrid;
using SendGrid.Helpers.Mail;
using Microsoft.EntityFrameworkCore;
using Shared.Application.Common.Interfaces;
using NotificationService.Services;
using NotificationService.Models;

namespace NotificationService.Services;

public class EmailService : IEmailService
{
    private readonly ISendGridClient _sendGridClient;
    private readonly ITemplateService _templateService;
    private readonly ILogger<EmailService> _logger;
    private readonly IConfiguration _configuration;

    public EmailService(
        ITemplateService templateService,
        ILogger<EmailService> logger,
        IConfiguration configuration)
    {
        _templateService = templateService;
        _logger = logger;
        _configuration = configuration;
        
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
            if (!string.IsNullOrEmpty(templateId) && templateData != null)
            {
                emailContent = await _templateService.RenderTemplateAsync(templateId, templateData);
            }

            var msg = MailHelper.CreateSingleEmail(from, toEmail, subject, emailContent, emailContent);
            
            var response = await _sendGridClient.SendEmailAsync(msg);
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Email sent successfully to {To}", to);
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
            if (!string.IsNullOrEmpty(templateId) && templateData != null)
            {
                emailContent = await _templateService.RenderTemplateAsync(templateId, templateData);
            }

            var msg = MailHelper.CreateSingleEmailToMultipleRecipients(from, tos, subject, emailContent, emailContent);
            
            var response = await _sendGridClient.SendEmailAsync(msg);
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Bulk email sent successfully to {Count} recipients", recipients.Count);
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
}
