using Microsoft.Extensions.Diagnostics.HealthChecks;
using Shared.Infrastructure.Messaging;
using StackExchange.Redis;

namespace NotificationService.HealthChecks;

public class NotificationSystemHealthCheck : IHealthCheck
{
    private readonly IMessageBus _messageBus;
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<NotificationSystemHealthCheck> _logger;

    public NotificationSystemHealthCheck(
        IMessageBus messageBus,
        IConnectionMultiplexer redis,
        ILogger<NotificationSystemHealthCheck> logger)
    {
        _messageBus = messageBus;
        _redis = redis;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var healthData = new Dictionary<string, object>();

            // Check RabbitMQ connection
            var rabbitMqHealthy = await CheckRabbitMQHealth();
            healthData["RabbitMQ"] = rabbitMqHealthy ? "Healthy" : "Unhealthy";

            // Check Redis connection
            var redisHealthy = await CheckRedisHealth();
            healthData["Redis"] = redisHealthy ? "Healthy" : "Unhealthy";

            // Check overall health
            var isHealthy = rabbitMqHealthy && redisHealthy;

            if (isHealthy)
            {
                return HealthCheckResult.Healthy("All notification services are healthy", healthData);
            }
            else
            {
                return HealthCheckResult.Degraded("Some notification services are unhealthy", data: healthData);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed");
            return HealthCheckResult.Unhealthy("Health check failed", ex);
        }
    }

    private async Task<bool> CheckRabbitMQHealth()
    {
        try
        {
            // Try to publish a simple health check message
            await _messageBus.PublishAsync(new { HealthCheck = true, Timestamp = DateTime.UtcNow }, "health.check", "system");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "RabbitMQ health check failed");
            return false;
        }
    }

    private async Task<bool> CheckRedisHealth()
    {
        try
        {
            var database = _redis.GetDatabase();
            await database.PingAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis health check failed");
            return false;
        }
    }
}