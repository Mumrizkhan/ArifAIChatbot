using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shared.Application.Common.Interfaces;
using Shared.Infrastructure.Messaging;
using Shared.Infrastructure.Persistence;
using Shared.Infrastructure.Services;
using StackExchange.Redis;

namespace Shared.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<ITenantService, TenantService>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = configuration.GetConnectionString("Redis") ?? "localhost:6379";
            options.InstanceName = "ArifChatbot";   
// Log the Redis configuration
            var logger = services.BuildServiceProvider().GetRequiredService<ILoggerFactory>().CreateLogger("ServiceCollectionExtensions");
            logger.LogInformation("Configuring Redis cache with connection string: {ConnectionString}", options.Configuration);

        });
        services.AddMemoryCache();
        services.AddSingleton<IConnectionMultiplexer>(provider =>
        {
            // Replace this line:
            // var logger = provider.GetRequiredService<ILogger<ServiceCollectionExtensions>>();
            // With the following:
            var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger("ServiceCollectionExtensions");
            var connectionString = configuration.GetConnectionString("Redis") ?? "localhost:6379";

            // Log the Redis connection string
            logger.LogInformation("Connecting to Redis with connection string: {ConnectionString}", connectionString);
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException("Redis connection string is not configured.");
            }
            return ConnectionMultiplexer.Connect(connectionString);
        });
        services.AddScoped<ICacheService, CacheService>();

        services.AddSingleton<IMessageBus, RabbitMQMessageBus>();

        return services;
    }
}
