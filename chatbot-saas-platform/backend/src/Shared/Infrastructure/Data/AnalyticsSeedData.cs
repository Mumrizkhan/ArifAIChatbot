using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shared.Domain.Entities;
using Shared.Domain.Enums;
using Shared.Infrastructure.Persistence;

namespace Shared.Infrastructure.Data;

public static class AnalyticsSeedData
{
    public static async Task SeedConversationsAndMessagesAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();

        try
        {
            if (!await context.Conversations.AnyAsync())
            {
                await SeedConversationsAsync(context, logger);
            }

            if (!await context.Messages.AnyAsync())
            {
                await SeedMessagesAsync(context, logger);
            }

            await context.SaveChangesAsync();
            logger.LogInformation("Analytics seed data completed successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while seeding analytics data");
            throw;
        }
    }

    private static async Task SeedConversationsAsync(ApplicationDbContext context, ILogger logger)
    {
        logger.LogInformation("Seeding conversations...");

        var tenants = await context.Tenants.ToListAsync();
        var users = await context.Users.Where(u => u.Role == UserRole.Agent).ToListAsync();

        var conversations = new List<Conversation>();
        var random = new Random();

        foreach (var tenant in tenants)
        {
            var tenantAgents = await context.UserTenants
                .Where(ut => ut.TenantId == tenant.Id && ut.Role == TenantRole.Agent)
                .Include(ut => ut.User)
                .ToListAsync();

            for (int i = 0; i < 50; i++)
            {
                var startTime = DateTime.UtcNow.AddDays(-random.Next(1, 30)).AddHours(-random.Next(0, 24));
                var endTime = startTime.AddMinutes(random.Next(5, 120));
                var isActive = random.Next(1, 10) <= 2;

                var conversation = new Conversation
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenant.Id,
                    CustomerName = GenerateCustomerName(),
                    CustomerEmail = GenerateCustomerEmail(),
                    Channel = (ConversationChannel)random.Next(0, 4),
                    Status = isActive ? ConversationStatus.Active : 
                            (ConversationStatus)random.Next(1, 4),
                    StartedAt = startTime,
                    EndedAt = isActive ? null : endTime,
                    AssignedAgentId = tenantAgents.Any() ? 
                        tenantAgents[random.Next(tenantAgents.Count)].UserId : null,
                    Rating = isActive ? null : random.Next(1, 6),
                    Tags = GenerateTags().Split(',').ToList(),
                    CreatedAt = startTime,
                    UpdatedAt = isActive ? DateTime.UtcNow : endTime
                };

                conversations.Add(conversation);
            }
        }

        await context.Conversations.AddRangeAsync(conversations);
        logger.LogInformation($"Added {conversations.Count} conversations");
    }

    private static async Task SeedMessagesAsync(ApplicationDbContext context, ILogger logger)
    {
        logger.LogInformation("Seeding messages...");

        var conversations = await context.Conversations.ToListAsync();
        var messages = new List<Message>();
        var random = new Random();

        foreach (var conversation in conversations)
        {
            var messageCount = random.Next(3, 20);
            var currentTime = conversation.StartedAt;

            for (int i = 0; i < messageCount; i++)
            {
                var isFromCustomer = i % 2 == 0;
                var messageType = random.Next(1, 10) <= 8 ? MessageType.Text : 
                                random.Next(1, 3) == 1 ? MessageType.Image : MessageType.File;

                var message = new Message
                {
                    Id = Guid.NewGuid(),
                    ConversationId = conversation.Id,
                    Content = GenerateMessageContent(messageType, isFromCustomer),
                    Type = messageType,
                    Sender = isFromCustomer ? MessageSender.Customer : 
                            (random.Next(1, 3) == 1 ? MessageSender.Agent : MessageSender.Bot),
                    SentAt = currentTime,
                    IsRead = true,
                    CreatedAt = currentTime
                };

                messages.Add(message);
                currentTime = currentTime.AddMinutes(random.Next(1, 10));
            }
        }

        await context.Messages.AddRangeAsync(messages);
        logger.LogInformation($"Added {messages.Count} messages");
    }

    private static string GenerateCustomerName()
    {
        var firstNames = new[] { "John", "Sarah", "Mike", "Emma", "David", "Lisa", "Ahmed", "Fatima", "Carlos", "Maria" };
        var lastNames = new[] { "Smith", "Johnson", "Williams", "Brown", "Jones", "Garcia", "Miller", "Davis", "Rodriguez", "Martinez" };
        var random = new Random();
        
        return $"{firstNames[random.Next(firstNames.Length)]} {lastNames[random.Next(lastNames.Length)]}";
    }

    private static string GenerateCustomerEmail()
    {
        var domains = new[] { "gmail.com", "yahoo.com", "hotmail.com", "outlook.com", "company.com" };
        var random = new Random();
        var name = GenerateCustomerName().Replace(" ", ".").ToLower();
        
        return $"{name}@{domains[random.Next(domains.Length)]}";
    }

    private static string GenerateTags()
    {
        var tags = new[] { "support", "billing", "technical", "sales", "urgent", "resolved", "escalated", "feedback" };
        var random = new Random();
        var selectedTags = tags.OrderBy(x => random.Next()).Take(random.Next(1, 4));
        
        return string.Join(",", selectedTags);
    }

    private static string GenerateMessageContent(MessageType type, bool isFromCustomer)
    {
        var random = new Random();

        return type switch
        {
            MessageType.Text => isFromCustomer ? GenerateCustomerMessage() : GenerateAgentMessage(),
            MessageType.Image => "image_attachment.jpg",
            MessageType.File => "document.pdf",
            _ => "Hello, how can I help you today?"
        };
    }

    private static string GenerateCustomerMessage()
    {
        var messages = new[]
        {
            "Hi, I need help with my account",
            "I'm having trouble logging in",
            "Can you help me with billing?",
            "My order hasn't arrived yet",
            "I want to cancel my subscription",
            "How do I reset my password?",
            "The app is not working properly",
            "I need technical support",
            "Can you explain this feature?",
            "Thank you for your help!"
        };
        
        return messages[new Random().Next(messages.Length)];
    }

    private static string GenerateAgentMessage()
    {
        var messages = new[]
        {
            "Hello! How can I assist you today?",
            "I'd be happy to help you with that",
            "Let me check that for you",
            "I understand your concern",
            "Here's what I found...",
            "Is there anything else I can help you with?",
            "Thank you for contacting us",
            "I've resolved the issue for you",
            "Please let me know if you need further assistance",
            "Have a great day!"
        };
        
        return messages[new Random().Next(messages.Length)];
    }
}
