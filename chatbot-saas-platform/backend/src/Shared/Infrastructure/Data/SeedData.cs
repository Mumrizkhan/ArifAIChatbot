using BCrypt.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shared.Domain.Entities;
using Shared.Domain.Enums;
using Shared.Infrastructure.Persistence;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Shared.Infrastructure.Data;

public static class SeedData
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();

        try
        {
            await context.Database.EnsureCreatedAsync();
            
            if (!await context.Users.AnyAsync())
            {
                await SeedUsersAsync(context, logger);
            }

            if (!await context.Tenants.AnyAsync())
            {
                await SeedTenantsAsync(context, logger);
            }

            if (!await context.UserTenants.AnyAsync())
            {
                await SeedUserTenantsAsync(context, logger);
                await context.SaveChangesAsync();
            }
            
            await AnalyticsSeedData.SeedConversationsAndMessagesAsync(serviceProvider);
            await SubscriptionSeedData.SeedSubscriptionDataAsync(serviceProvider);
            await WorkflowSeedData.SeedWorkflowDataAsync(serviceProvider);
            await KnowledgeBaseSeedData.SeedKnowledgeBaseDataAsync(serviceProvider);
            await NotificationSeedData.SeedNotificationDataAsync(serviceProvider);
            
            logger.LogInformation("Database seeding completed successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while seeding the database");
            throw;
        }
    }

    private static async Task SeedUsersAsync(ApplicationDbContext context, ILogger logger)
    {
        logger.LogInformation("Seeding users...");

        var users = new List<User>
        {
            new User
            {
                Id = Guid.NewGuid(),
                Email = "admin@arifplatform.com",
                FirstName = "System",
                LastName = "Administrator",
                PasswordHash = HashPassword("Admin123!"),
                Role = UserRole.SuperAdmin,
                IsActive = true,
                EmailConfirmed = true,
                CreatedAt = DateTime.UtcNow,
                LastLoginAt = DateTime.UtcNow
            },
            new User
            {
                Id = Guid.NewGuid(),
                Email = "tenant1@example.com",
                FirstName = "John",
                LastName = "Smith",
                PasswordHash = HashPassword("Tenant123!"),
                Role = UserRole.TenantAdmin,
                IsActive = true,
                EmailConfirmed = true,
                CreatedAt = DateTime.UtcNow,
                LastLoginAt = DateTime.UtcNow.AddDays(-1)
            },
            new User
            {
                Id = Guid.NewGuid(),
                Email = "tenant2@example.com",
                FirstName = "Sarah",
                LastName = "Johnson",
                PasswordHash = HashPassword("Tenant123!"),
                Role = UserRole.TenantAdmin,
                IsActive = true,
                EmailConfirmed = true,
                CreatedAt = DateTime.UtcNow,
                LastLoginAt = DateTime.UtcNow.AddDays(-2)
            },
            new User
            {
                Id = Guid.NewGuid(),
                Email = "agent1@example.com",
                FirstName = "Mike",
                LastName = "Wilson",
                PasswordHash = HashPassword("Agent123!"),
                Role = UserRole.Agent,
                IsActive = true,
                EmailConfirmed = true,
                CreatedAt = DateTime.UtcNow,
                LastLoginAt = DateTime.UtcNow.AddHours(-2)
            },
            new User
            {
                Id = Guid.NewGuid(),
                Email = "agent2@example.com",
                FirstName = "Emma",
                LastName = "Davis",
                PasswordHash = HashPassword("Agent123!"),
                Role = UserRole.Agent,
                IsActive = true,
                EmailConfirmed = true,
                CreatedAt = DateTime.UtcNow,
                LastLoginAt = DateTime.UtcNow.AddHours(-1)
            },
            new User
            {
                Id = Guid.NewGuid(),
                Email = "agent3@example.com",
                FirstName = "Ahmed",
                LastName = "Al-Rashid",
                PasswordHash = HashPassword("Agent123!"),
                Role = UserRole.Agent,
                IsActive = true,
                EmailConfirmed = true,
                CreatedAt = DateTime.UtcNow,
                LastLoginAt = DateTime.UtcNow.AddMinutes(-30)
            }
        };

        await context.Users.AddRangeAsync(users);
        logger.LogInformation($"Added {users.Count} users");
    }

    private static async Task SeedTenantsAsync(ApplicationDbContext context, ILogger logger)
    {
        logger.LogInformation("Seeding tenants...");

        var tenants = new List<Tenant>
        {
            new Tenant
            {
                Id = Guid.NewGuid(),
                Name = "TechCorp Solutions",
                Subdomain = "techcorp",
                Domain = "techcorp.example.com",
                Status = TenantStatus.Active,
                SubscriptionPlan = "Enterprise",
                MaxUsers = 100,
                MaxAgents = 20,
                MaxConversations = 10000,
                Settings = JsonSerializer.Deserialize<Dictionary<string, object>>("""
                {
                    "theme": {
                        "primaryColor": "#3B82F6",
                        "secondaryColor": "#1E40AF",
                        "isDarkMode": false
                    },
                    "features": {
                        "aiChatbot": true,
                        "liveAgent": true,
                        "analytics": true,
                        "workflows": true
                    },
                    "branding": {
                        "companyName": "TechCorp Solutions",
                        "logoUrl": "/logos/techcorp.png"
                    }
                }
                """) ?? new Dictionary<string, object>(),
                CreatedAt = DateTime.UtcNow.AddDays(-30),
                UpdatedAt = DateTime.UtcNow.AddDays(-1)
            },
            new Tenant
            {
                Id = Guid.NewGuid(),
                Name = "Global Retail Inc",
                Subdomain = "globalretail",
                Domain = "globalretail.example.com",
                Status = TenantStatus.Active,
                SubscriptionPlan = "Professional",
                MaxUsers = 50,
                MaxAgents = 10,
                MaxConversations = 5000,
                Settings = JsonSerializer.Deserialize<Dictionary<string, object>>("""
                {
                    "theme": {
                        "primaryColor": "#10B981",
                        "secondaryColor": "#059669",
                        "isDarkMode": false
                    },
                    "features": {
                        "aiChatbot": true,
                        "liveAgent": true,
                        "analytics": true,
                        "workflows": false
                    },
                    "branding": {
                        "companyName": "Global Retail Inc",
                        "logoUrl": "/logos/globalretail.png"
                    }
                }
                """) ?? new Dictionary<string, object>(),
                CreatedAt = DateTime.UtcNow.AddDays(-20),
                UpdatedAt = DateTime.UtcNow.AddDays(-3)
            },
            new Tenant
            {
                Id = Guid.NewGuid(),
                Name = "Healthcare Plus",
                Subdomain = "healthcareplus",
                Domain = "healthcareplus.example.com",
                Status = TenantStatus.Active,
                SubscriptionPlan = "Starter",
                MaxUsers = 25,
                MaxAgents = 5,
                MaxConversations = 1000,
                Settings = JsonSerializer.Deserialize<Dictionary<string, object>>("""
                {
                    "theme": {
                        "primaryColor": "#EF4444",
                        "secondaryColor": "#DC2626",
                        "isDarkMode": false
                    },
                    "features": {
                        "aiChatbot": true,
                        "liveAgent": false,
                        "analytics": true,
                        "workflows": false
                    },
                    "branding": {
                        "companyName": "Healthcare Plus",
                        "logoUrl": "/logos/healthcareplus.png"
                    }
                }
                """) ?? new Dictionary<string, object>(),
                CreatedAt = DateTime.UtcNow.AddDays(-10),
                UpdatedAt = DateTime.UtcNow.AddDays(-2)
            }
        };

        await context.Tenants.AddRangeAsync(tenants);
        logger.LogInformation($"Added {tenants.Count} tenants");
    }

    private static async Task SeedUserTenantsAsync(ApplicationDbContext context, ILogger logger)
    {
        logger.LogInformation("Seeding user-tenant relationships...");

        var users = await context.Users.ToListAsync();
        var tenants = await context.Tenants.ToListAsync();

        var userTenants = new List<UserTenant>();

        var tenant1Admin = users.FirstOrDefault(u => u.Email == "tenant1@example.com");
        var tenant2Admin = users.FirstOrDefault(u => u.Email == "tenant2@example.com");
        var agent1 = users.FirstOrDefault(u => u.Email == "agent1@example.com");
        var agent2 = users.FirstOrDefault(u => u.Email == "agent2@example.com");
        var agent3 = users.FirstOrDefault(u => u.Email == "agent3@example.com");

        var techCorp = tenants.FirstOrDefault(t => t.Name == "TechCorp Solutions");
        var globalRetail = tenants.FirstOrDefault(t => t.Name == "Global Retail Inc");
        var healthcarePlus = tenants.FirstOrDefault(t => t.Name == "Healthcare Plus");

        if (tenant1Admin != null && techCorp != null)
        {
            var exists = await context.UserTenants.AnyAsync(ut => ut.UserId == tenant1Admin.Id && ut.TenantId == techCorp.Id);
            if (!exists)
            {
                userTenants.Add(new UserTenant
                {
                    UserId = tenant1Admin.Id,
                    TenantId = techCorp.Id,
                    Role = TenantRole.Admin,
                    IsActive = true,
                    JoinedAt = DateTime.UtcNow.AddDays(-30)
                });
            }
        }

        if (tenant2Admin != null && globalRetail != null)
        {
            var exists = await context.UserTenants.AnyAsync(ut => ut.UserId == tenant2Admin.Id && ut.TenantId == globalRetail.Id);
            if (!exists)
            {
                userTenants.Add(new UserTenant
                {
                    UserId = tenant2Admin.Id,
                    TenantId = globalRetail.Id,
                    Role = TenantRole.Admin,
                    IsActive = true,
                    JoinedAt = DateTime.UtcNow.AddDays(-20)
                });
            }
        }

        if (agent1 != null && techCorp != null)
        {
            var exists = await context.UserTenants.AnyAsync(ut => ut.UserId == agent1.Id && ut.TenantId == techCorp.Id);
            if (!exists)
            {
                userTenants.Add(new UserTenant
                {
                    UserId = agent1.Id,
                    TenantId = techCorp.Id,
                    Role = TenantRole.Agent,
                    IsActive = true,
                    JoinedAt = DateTime.UtcNow.AddDays(-25)
                });
            }
        }

        if (agent2 != null && techCorp != null)
        {
            var exists = await context.UserTenants.AnyAsync(ut => ut.UserId == agent2.Id && ut.TenantId == techCorp.Id);
            if (!exists)
            {
                userTenants.Add(new UserTenant
                {
                    UserId = agent2.Id,
                    TenantId = techCorp.Id,
                    Role = TenantRole.Agent,
                    IsActive = true,
                    JoinedAt = DateTime.UtcNow.AddDays(-20)
                });
            }
        }

        if (agent3 != null && globalRetail != null)
        {
            var exists = await context.UserTenants.AnyAsync(ut => ut.UserId == agent3.Id && ut.TenantId == globalRetail.Id);
            if (!exists)
            {
                userTenants.Add(new UserTenant
                {
                    UserId = agent3.Id,
                    TenantId = globalRetail.Id,
                    Role = TenantRole.Agent,
                    IsActive = true,
                    JoinedAt = DateTime.UtcNow.AddDays(-15)
                });
            }
        }

        if (userTenants.Any())
        {
            await context.UserTenants.AddRangeAsync(userTenants);
            logger.LogInformation($"Added {userTenants.Count} user-tenant relationships");
        }
    }

    private static string HashPassword(string password)
    {
        //using var sha256 = SHA256.Create();
        //var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password + "ArifPlatformSalt"));
        //return Convert.ToBase64String(hashedBytes);
        return BCrypt.Net.BCrypt.HashPassword(password);
    }
}
