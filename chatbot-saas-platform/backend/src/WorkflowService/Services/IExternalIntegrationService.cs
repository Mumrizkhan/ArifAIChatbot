using Shared.Domain.Entities;
using WorkflowService.Models;

namespace WorkflowService.Services;

public interface IExternalIntegrationService
{
    Task<Dictionary<string, object>> ExecuteHttpRequestAsync(Dictionary<string, object> configuration, Dictionary<string, object> inputData);
    Task<Dictionary<string, object>> ExecuteDatabaseQueryAsync(Dictionary<string, object> configuration, Dictionary<string, object> inputData);
    Task<Dictionary<string, object>> SendEmailAsync(Dictionary<string, object> configuration, Dictionary<string, object> inputData);
    Task<Dictionary<string, object>> ExecuteScriptAsync(Dictionary<string, object> configuration, Dictionary<string, object> inputData);
    Task<Dictionary<string, object>> CallWebhookAsync(Dictionary<string, object> configuration, Dictionary<string, object> inputData);
    Task<bool> ValidateIntegrationConfigurationAsync(WorkflowStepType stepType, Dictionary<string, object> configuration);
}
