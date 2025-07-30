using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shared.Infrastructure.Persistence;

namespace Shared.Infrastructure.Data;

public static class KnowledgeBaseSeedData
{
    public static async Task SeedKnowledgeBaseDataAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();

        try
        {
            logger.LogInformation("Knowledge base seed data will be handled by KnowledgeBaseService");

            await context.SaveChangesAsync();
            logger.LogInformation("Knowledge base seed data completed successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while seeding knowledge base data");
            throw;
        }
    }

    private static async Task SeedDocumentsAsync(ApplicationDbContext context, ILogger logger)
    {
        logger.LogInformation("Knowledge base documents will be seeded by KnowledgeBaseService");
    }
}
