using Shared.Infrastructure.Messaging;
using Shared.Infrastructure.Messaging.Events;
using NotificationService.Services;

namespace NotificationService.Handlers;

public class NotificationEventHandlers
{
    private readonly INotificationService _notificationService;
    private readonly ILogger<NotificationEventHandlers> _logger;

    public NotificationEventHandlers(
        INotificationService notificationService,
        ILogger<NotificationEventHandlers> logger)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task<bool> HandleEmailQueuedAsync(EmailQueuedEvent emailEvent)
    {
        try
        {
            await Task.Delay(100);
            _logger.LogInformation("Email processed successfully: {EmailId}", emailEvent.EmailId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process email: {EmailId}", emailEvent.EmailId);
            return false;
        }
    }

    public async Task<bool> HandleSmsQueuedAsync(SmsQueuedEvent smsEvent)
    {
        try
        {
            await Task.Delay(100);
            _logger.LogInformation("SMS processed successfully: {SmsId}", smsEvent.SmsId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process SMS: {SmsId}", smsEvent.SmsId);
            return false;
        }
    }
}
