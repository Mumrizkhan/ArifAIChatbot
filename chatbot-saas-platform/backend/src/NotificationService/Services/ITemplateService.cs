using NotificationService.Models;
using Shared.Domain.Entities;

namespace NotificationService.Services;

public interface ITemplateService
{
    Task<string> RenderTemplateAsync(string templateId, Dictionary<string, object> data, string language = "en");
    Task<NotificationTemplate?> GetTemplateAsync(string templateId, Guid tenantId, string language = "en");
    Task<List<NotificationTemplate>> GetTemplatesAsync(Guid tenantId, NotificationType? type = null, NotificationChannel? channel = null);
    Task<NotificationTemplate> CreateTemplateAsync(NotificationTemplate template);
    Task<bool> UpdateTemplateAsync(NotificationTemplate template,Guid tenantId);
    Task<bool> DeleteTemplateAsync(string templateId, Guid tenantId);
}
