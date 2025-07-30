using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shared.Infrastructure.Persistence;
using Shared.Domain.Enums;

namespace Shared.Infrastructure.Data;

public static class NotificationSeedData
{
    public static async Task SeedNotificationDataAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();

        try
        {
            await SeedNotificationTemplatesAsync(context, logger);
            await SeedNotificationPreferencesAsync(context, logger);

            await context.SaveChangesAsync();
            logger.LogInformation("Notification seed data completed successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while seeding notification data");
            throw;
        }
    }

    private static async Task SeedNotificationTemplatesAsync(ApplicationDbContext context, ILogger logger)
    {
        logger.LogInformation("Notification templates will be seeded by NotificationService");
    }

    private static async Task SeedNotificationPreferencesAsync(ApplicationDbContext context, ILogger logger)
    {
        logger.LogInformation("Notification preferences will be seeded by NotificationService");
    }
}
