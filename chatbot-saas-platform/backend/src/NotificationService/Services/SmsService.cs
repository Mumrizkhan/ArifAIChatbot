using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;
using NotificationService.Services;
using NotificationService.Models;

namespace NotificationService.Services;

public class SmsService : ISmsService
{
    private readonly ITemplateService _templateService;
    private readonly ILogger<SmsService> _logger;
    private readonly IConfiguration _configuration;

    public SmsService(
        ITemplateService templateService,
        ILogger<SmsService> logger,
        IConfiguration configuration)
    {
        _templateService = templateService;
        _logger = logger;
        _configuration = configuration;
        
        var accountSid = _configuration["Twilio:AccountSid"] ?? throw new InvalidOperationException("Twilio Account SID not configured");
        var authToken = _configuration["Twilio:AuthToken"] ?? throw new InvalidOperationException("Twilio Auth Token not configured");
        
        TwilioClient.Init(accountSid, authToken);
    }

    public async Task<bool> SendSmsAsync(string to, string content, string? templateId = null, Dictionary<string, object>? templateData = null)
    {
        try
        {
            string smsContent = content;
            if (!string.IsNullOrEmpty(templateId) && templateData != null)
            {
                smsContent = await _templateService.RenderTemplateAsync(templateId, templateData);
            }

            var fromNumber = _configuration["Twilio:FromNumber"];
            
            var message = await MessageResource.CreateAsync(
                body: smsContent,
                from: new PhoneNumber(fromNumber),
                to: new PhoneNumber(to)
            );

            if (message.Status == MessageResource.StatusEnum.Queued || 
                message.Status == MessageResource.StatusEnum.Sent ||
                message.Status == MessageResource.StatusEnum.Delivered)
            {
                _logger.LogInformation("SMS sent successfully to {To}. Message SID: {MessageSid}", to, message.Sid);
                return true;
            }
            else
            {
                _logger.LogError("Failed to send SMS to {To}. Status: {Status}, Error: {Error}", 
                    to, message.Status, message.ErrorMessage);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending SMS to {To}", to);
            return false;
        }
    }

    public async Task<bool> SendBulkSmsAsync(List<string> recipients, string content, string? templateId = null, Dictionary<string, object>? templateData = null)
    {
        try
        {
            string smsContent = content;
            if (!string.IsNullOrEmpty(templateId) && templateData != null)
            {
                smsContent = await _templateService.RenderTemplateAsync(templateId, templateData);
            }

            var fromNumber = _configuration["Twilio:FromNumber"];
            var tasks = new List<Task<bool>>();

            foreach (var recipient in recipients)
            {
                tasks.Add(SendSmsAsync(recipient, smsContent));
            }

            var results = await Task.WhenAll(tasks);
            var successCount = results.Count(r => r);

            _logger.LogInformation("Bulk SMS completed. {Success}/{Total} messages sent successfully", 
                successCount, recipients.Count);

            return successCount > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending bulk SMS to {Count} recipients", recipients.Count);
            return false;
        }
    }

    public async Task<bool> ValidatePhoneNumberAsync(string phoneNumber)
    {
        try
        {
            var cleanNumber = phoneNumber.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "");
            return cleanNumber.StartsWith("+") && cleanNumber.Length >= 10 && cleanNumber.All(c => char.IsDigit(c) || c == '+');
        }
        catch
        {
            return false;
        }
    }

    public async Task<SmsDeliveryStatus> GetDeliveryStatusAsync(string messageId)
    {
        try
        {
            var message = await MessageResource.FetchAsync(messageId);
            
            return new SmsDeliveryStatus
            {
                MessageId = messageId,
                Status = message.Status.ToString(),
                DeliveredAt = message.DateSent,
                ErrorMessage = message.ErrorMessage
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting SMS delivery status for message {MessageId}", messageId);
            return new SmsDeliveryStatus
            {
                MessageId = messageId,
                Status = "unknown",
                ErrorMessage = ex.Message
            };
        }
    }
}
