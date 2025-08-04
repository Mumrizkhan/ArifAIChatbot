using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Shared.Domain.Entities;


namespace Shared.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    // Shared domain entities
    DbSet<User> Users { get; }
    DbSet<Tenant> Tenants { get; }
    DbSet<UserTenant> UserTenants { get; }
    DbSet<Conversation> Conversations { get; }
    DbSet<Message> Messages { get; }

    // SubscriptionService entities
    DbSet<Plan> Plans { get; }
    DbSet<Subscription> Subscriptions { get; }
    DbSet<UsageRecord> UsageRecords { get; }
    DbSet<Invoice> Invoices { get; }
    DbSet<PaymentMethod> PaymentMethods { get; }
    DbSet<BillingAddress> BillingAddresses { get; }

    // KnowledgeBaseService entities
    DbSet<Document> Documents { get; }
    DbSet<DocumentChunk> DocumentChunks { get; }

    // WorkflowService entities
    DbSet<Workflow> Workflows { get; }
    DbSet<WorkflowDefinition> WorkflowDefinitions { get; }
    DbSet<WorkflowStep> WorkflowSteps { get; }
    DbSet<WorkflowConnection> WorkflowConnections { get; }
    DbSet<WorkflowTemplate> WorkflowTemplates { get; }

    // NotificationService entities
    DbSet<NotificationTemplate> NotificationTemplates { get; }
    DbSet<NotificationPreference> NotificationPreferences { get; }
    DbSet<ChatbotConfig> ChatbotConfigs { get; }

    // Generic access
    DbSet<TEntity> Set<TEntity>() where TEntity : class;

    EntityEntry<TEntity> Entry<TEntity>(TEntity entity) where TEntity : class;

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
