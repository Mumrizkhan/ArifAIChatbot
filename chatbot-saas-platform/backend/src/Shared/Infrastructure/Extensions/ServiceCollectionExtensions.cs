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
        // Add Entity Framework
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());

        // Add common services
        services.AddScoped<ITenantService, TenantService>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IFileStorageService, FileStorageService>();

        // Add Redis and caching
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = configuration.GetValue<string>("Redis:ConnectionString") ?? "localhost:6379";
            options.InstanceName = "ArifChatbot";
        });
        
        services.AddMemoryCache();
        
        services.AddSingleton<IConnectionMultiplexer>(provider =>
        {
            var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger("ServiceCollectionExtensions");
            var connectionString = configuration.GetValue<string>("Redis:ConnectionString") ?? "localhost:6379";

            logger.LogInformation("Connecting to Redis with connection string: {ConnectionString}", connectionString);
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException("Redis connection string is not configured.");
            }
            return ConnectionMultiplexer.Connect(connectionString);
        });
        
        // Add cache service
        services.AddScoped<ICacheService, CacheService>();

        // Add message bus
        services.AddSingleton<IMessageBus, RabbitMQMessageBus>();
        
        // Add message bus notification service
        services.AddScoped<IMessageBusNotificationService, MessageBusNotificationService>();

        return services;
    }
}
