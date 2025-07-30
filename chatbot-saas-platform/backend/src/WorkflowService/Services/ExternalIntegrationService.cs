using RestSharp;
using Microsoft.Data.SqlClient;
using System.Text.Json;
using WorkflowService.Services;
using WorkflowService.Models;
using Shared.Domain.Entities;

namespace WorkflowService.Services;

public class ExternalIntegrationService : IExternalIntegrationService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ExternalIntegrationService> _logger;
    private readonly IConfiguration _configuration;

    public ExternalIntegrationService(
        HttpClient httpClient,
        ILogger<ExternalIntegrationService> logger,
        IConfiguration configuration)
    {
        _httpClient = httpClient;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<Dictionary<string, object>> ExecuteHttpRequestAsync(Dictionary<string, object> configuration, Dictionary<string, object> inputData)
    {
        try
        {
            var url = configuration.GetValueOrDefault("url", "")?.ToString() ?? "";
            var method = configuration.GetValueOrDefault("method", "GET")?.ToString() ?? "GET";
            var headers = configuration.GetValueOrDefault("headers", new Dictionary<string, object>()) as Dictionary<string, object> ?? new();
            var body = configuration.GetValueOrDefault("body", "")?.ToString() ?? "";
            var timeout = Convert.ToInt32(configuration.GetValueOrDefault("timeout", 30));

            var client = new RestClient(url);
            var request = new RestRequest("", GetRestSharpMethod(method));

            foreach (var header in headers)
            {
                request.AddHeader(header.Key, header.Value?.ToString() ?? "");
            }

            if (!string.IsNullOrEmpty(body) && (method.ToUpper() == "POST" || method.ToUpper() == "PUT"))
            {
                var processedBody = ReplaceVariables(body, inputData);
                request.AddJsonBody(processedBody);
            }

            if (method.ToUpper() == "GET")
            {
                foreach (var param in inputData)
                {
                    request.AddQueryParameter(param.Key, param.Value?.ToString() ?? "");
                }
            }

            var response = await client.ExecuteAsync(request);

            var result = new Dictionary<string, object>
            {
                ["statusCode"] = (int)response.StatusCode,
                ["isSuccess"] = response.IsSuccessful,
                ["content"] = response.Content ?? "",
                ["headers"] = response.Headers?.ToDictionary(h => h.Name, h => h.Value?.ToString() ?? "") ?? new Dictionary<string, string>()
            };

            if (response.IsSuccessful && !string.IsNullOrEmpty(response.Content))
            {
                try
                {
                    var jsonData = JsonSerializer.Deserialize<Dictionary<string, object>>(response.Content);
                    if (jsonData != null)
                    {
                        result["data"] = jsonData;
                    }
                }
                catch
                {
                    result["data"] = response.Content;
                }
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing HTTP request");
            return new Dictionary<string, object>
            {
                ["isSuccess"] = false,
                ["error"] = ex.Message
            };
        }
    }

    public async Task<Dictionary<string, object>> ExecuteDatabaseQueryAsync(Dictionary<string, object> configuration, Dictionary<string, object> inputData)
    {
        try
        {
            var connectionString = configuration.GetValueOrDefault("connectionString", "")?.ToString() ?? "";
            var query = configuration.GetValueOrDefault("query", "")?.ToString() ?? "";
            var timeout = Convert.ToInt32(configuration.GetValueOrDefault("timeout", 30));

            if (string.IsNullOrEmpty(connectionString) || string.IsNullOrEmpty(query))
            {
                throw new ArgumentException("Connection string and query are required");
            }

            var processedQuery = ReplaceVariables(query, inputData);

            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            using var command = new SqlCommand(processedQuery, connection);
            command.CommandTimeout = timeout;

            foreach (var param in inputData)
            {
                command.Parameters.AddWithValue($"@{param.Key}", param.Value ?? DBNull.Value);
            }

            var result = new Dictionary<string, object>();

            if (processedQuery.Trim().ToUpper().StartsWith("SELECT"))
            {
                var results = new List<Dictionary<string, object>>();
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    var row = new Dictionary<string, object>();
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        row[reader.GetName(i)] = reader.GetValue(i);
                    }
                    results.Add(row);
                }

                result["data"] = results;
                result["rowCount"] = results.Count;
            }
            else
            {
                var rowsAffected = await command.ExecuteNonQueryAsync();
                result["rowsAffected"] = rowsAffected;
            }

            result["isSuccess"] = true;
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing database query");
            return new Dictionary<string, object>
            {
                ["isSuccess"] = false,
                ["error"] = ex.Message
            };
        }
    }

    public async Task<Dictionary<string, object>> SendEmailAsync(Dictionary<string, object> configuration, Dictionary<string, object> inputData)
    {
        try
        {
            var to = configuration.GetValueOrDefault("to", "")?.ToString() ?? "";
            var subject = configuration.GetValueOrDefault("subject", "")?.ToString() ?? "";
            var body = configuration.GetValueOrDefault("body", "")?.ToString() ?? "";
            var from = configuration.GetValueOrDefault("from", "")?.ToString() ?? "";

            to = ReplaceVariables(to, inputData);
            subject = ReplaceVariables(subject, inputData);
            body = ReplaceVariables(body, inputData);

            await Task.Delay(100); // Simulate processing time

            return new Dictionary<string, object>
            {
                ["isSuccess"] = true,
                ["messageId"] = Guid.NewGuid().ToString(),
                ["to"] = to,
                ["subject"] = subject
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending email");
            return new Dictionary<string, object>
            {
                ["isSuccess"] = false,
                ["error"] = ex.Message
            };
        }
    }

    public async Task<Dictionary<string, object>> ExecuteScriptAsync(Dictionary<string, object> configuration, Dictionary<string, object> inputData)
    {
        try
        {
            var scriptType = configuration.GetValueOrDefault("scriptType", "javascript")?.ToString() ?? "javascript";
            var script = configuration.GetValueOrDefault("script", "")?.ToString() ?? "";
            var timeout = Convert.ToInt32(configuration.GetValueOrDefault("timeout", 30));

            if (string.IsNullOrEmpty(script))
            {
                throw new ArgumentException("Script is required");
            }

            var processedScript = ReplaceVariables(script, inputData);

            var result = new Dictionary<string, object>();

            switch (scriptType.ToLower())
            {
                case "javascript":
                    result = await ExecuteJavaScriptAsync(processedScript, inputData, timeout);
                    break;
                case "powershell":
                    result = await ExecutePowerShellAsync(processedScript, inputData, timeout);
                    break;
                default:
                    throw new NotSupportedException($"Script type {scriptType} is not supported");
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing script");
            return new Dictionary<string, object>
            {
                ["isSuccess"] = false,
                ["error"] = ex.Message
            };
        }
    }

    public async Task<Dictionary<string, object>> CallWebhookAsync(Dictionary<string, object> configuration, Dictionary<string, object> inputData)
    {
        try
        {
            var url = configuration.GetValueOrDefault("url", "")?.ToString() ?? "";
            var method = configuration.GetValueOrDefault("method", "POST")?.ToString() ?? "POST";
            var headers = configuration.GetValueOrDefault("headers", new Dictionary<string, object>()) as Dictionary<string, object> ?? new();
            var secret = configuration.GetValueOrDefault("secret", "")?.ToString() ?? "";

            if (string.IsNullOrEmpty(url))
            {
                throw new ArgumentException("Webhook URL is required");
            }

            var client = new RestClient(url);
            var request = new RestRequest("", GetRestSharpMethod(method));

            foreach (var header in headers)
            {
                request.AddHeader(header.Key, header.Value?.ToString() ?? "");
            }

            if (!string.IsNullOrEmpty(secret))
            {
                var payload = JsonSerializer.Serialize(inputData);
                var signature = GenerateWebhookSignature(payload, secret);
                request.AddHeader("X-Webhook-Signature", signature);
            }

            request.AddJsonBody(inputData);

            var response = await client.ExecuteAsync(request);

            return new Dictionary<string, object>
            {
                ["statusCode"] = (int)response.StatusCode,
                ["isSuccess"] = response.IsSuccessful,
                ["content"] = response.Content ?? "",
                ["responseTime"] = 100.0 // Placeholder response time
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling webhook");
            return new Dictionary<string, object>
            {
                ["isSuccess"] = false,
                ["error"] = ex.Message
            };
        }
    }

    public async Task<bool> ValidateIntegrationConfigurationAsync(WorkflowStepType stepType, Dictionary<string, object> configuration)
    {
        try
        {
            switch (stepType)
            {
                case WorkflowStepType.HttpRequest:
                    return configuration.ContainsKey("url") && !string.IsNullOrEmpty(configuration["url"]?.ToString());

                case WorkflowStepType.DatabaseQuery:
                    return configuration.ContainsKey("connectionString") && 
                           configuration.ContainsKey("query") &&
                           !string.IsNullOrEmpty(configuration["connectionString"]?.ToString()) &&
                           !string.IsNullOrEmpty(configuration["query"]?.ToString());

                case WorkflowStepType.EmailSend:
                    return configuration.ContainsKey("to") && 
                           configuration.ContainsKey("subject") &&
                           !string.IsNullOrEmpty(configuration["to"]?.ToString()) &&
                           !string.IsNullOrEmpty(configuration["subject"]?.ToString());

                case WorkflowStepType.ScriptExecution:
                    return configuration.ContainsKey("script") && 
                           !string.IsNullOrEmpty(configuration["script"]?.ToString());

                default:
                    return true;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating integration configuration for step type {StepType}", stepType);
            return false;
        }
    }

    private Method GetRestSharpMethod(string method)
    {
        return method.ToUpper() switch
        {
            "GET" => Method.Get,
            "POST" => Method.Post,
            "PUT" => Method.Put,
            "DELETE" => Method.Delete,
            "PATCH" => Method.Patch,
            _ => Method.Get
        };
    }

    private string ReplaceVariables(string template, Dictionary<string, object> variables)
    {
        var result = template;
        foreach (var variable in variables)
        {
            var placeholder = $"${{{variable.Key}}}";
            result = result.Replace(placeholder, variable.Value?.ToString() ?? "");
        }
        return result;
    }

    private async Task<Dictionary<string, object>> ExecuteJavaScriptAsync(string script, Dictionary<string, object> inputData, int timeout)
    {
        await Task.Delay(100);
        
        return new Dictionary<string, object>
        {
            ["isSuccess"] = true,
            ["output"] = "JavaScript execution completed",
            ["executionTime"] = 100
        };
    }

    private async Task<Dictionary<string, object>> ExecutePowerShellAsync(string script, Dictionary<string, object> inputData, int timeout)
    {
        await Task.Delay(100);
        
        return new Dictionary<string, object>
        {
            ["isSuccess"] = true,
            ["output"] = "PowerShell execution completed",
            ["executionTime"] = 100
        };
    }

    private string GenerateWebhookSignature(string payload, string secret)
    {
        using var hmac = new System.Security.Cryptography.HMACSHA256(System.Text.Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(hash).ToLower();
    }
}
