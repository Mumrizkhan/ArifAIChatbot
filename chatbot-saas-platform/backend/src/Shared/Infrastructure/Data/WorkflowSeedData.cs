using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shared.Infrastructure.Persistence;

namespace Shared.Infrastructure.Data;

public static class WorkflowSeedData
{
    public static async Task SeedWorkflowDataAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();

        try
        {
            logger.LogInformation("Workflow seed data will be handled by WorkflowService");

            await context.SaveChangesAsync();
            logger.LogInformation("Workflow seed data completed successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while seeding workflow data");
            throw;
        }
    }

    private static async Task SeedWorkflowTemplatesAsync(ApplicationDbContext context, ILogger logger)
    {
        logger.LogInformation("Workflow templates will be seeded by WorkflowService");
    }

    private static async Task SeedWorkflowsAsync(ApplicationDbContext context, ILogger logger)
    {
        logger.LogInformation("Workflows will be seeded by WorkflowService");
    }
}
