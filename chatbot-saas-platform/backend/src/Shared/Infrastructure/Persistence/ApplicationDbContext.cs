using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Shared.Application.Common.Interfaces;
using Shared.Domain.Common;
using Shared.Domain.Entities;
using System.Reflection;

namespace Shared.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    private readonly ICurrentUserService _currentUserService;
    private readonly ITenantService _tenantService;

    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        ICurrentUserService currentUserService,
        ITenantService tenantService) : base(options)
    {
        _currentUserService = currentUserService;
        _tenantService = tenantService;
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<UserTenant> UserTenants => Set<UserTenant>();
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<Message> Messages => Set<Message>();

    public DbSet<Plan> Plans => Set<Plan>();

    public DbSet<Subscription> Subscriptions => Set<Subscription>();

    public DbSet<UsageRecord> UsageRecords => Set<UsageRecord>();

    public DbSet<Invoice> Invoices => Set<Invoice>();

    public DbSet<PaymentMethod> PaymentMethods => Set<PaymentMethod>();

    public DbSet<BillingAddress> BillingAddresses => Set<BillingAddress>();

    public DbSet<Document> Documents => Set<Document>();

    public DbSet<DocumentChunk> DocumentChunks => Set<DocumentChunk>();

    public DbSet<WorkflowDefinition> WorkflowDefinitions => Set<WorkflowDefinition>();

    public DbSet<WorkflowStep> WorkflowSteps => Set<WorkflowStep>();

    public DbSet<WorkflowConnection> WorkflowConnections => Set<WorkflowConnection>();

    public DbSet<WorkflowTemplate> WorkflowTemplates => Set<WorkflowTemplate>();

    public DbSet<NotificationTemplate> NotificationTemplates => Set<NotificationTemplate>();

    public DbSet<NotificationPreference> NotificationPreferences => Set<NotificationPreference>();

    public DbSet<ChatbotConfig> ChatbotConfigs => Set<ChatbotConfig>();

    public new DbSet<TEntity> Set<TEntity>() where TEntity : class => base.Set<TEntity>();
    public EntityEntry<TEntity> Entry<TEntity>(TEntity entity) where TEntity : class
       => base.Entry(entity);
    // Example usage of ApplicationDbContextOptionsBuilder.Configure
    // This would typically be in your startup or composition root, not inside ApplicationDbContext itself.


    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        var tenantId = _tenantService.GetCurrentTenantId();

        builder.Entity<Conversation>().HasQueryFilter(c => c.TenantId == tenantId);
        builder.Entity<Message>().HasQueryFilter(m => m.TenantId == tenantId);
        //builder.Entity<UserTenant>().HasQueryFilter(ut => ut.TenantId == tenantId);

        // Example: Use nvarchar(max) for SQL Server instead of jsonb
        builder.Entity<Plan>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(128);
            entity.Property(e => e.Description).HasMaxLength(512);
            entity.Property(e => e.MonthlyPrice).HasColumnType("decimal(18,2)");
            entity.Property(e => e.YearlyPrice).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Currency).IsRequired().HasMaxLength(8);
            entity.Property(e => e.IsActive).IsRequired();
            entity.Property(e => e.IsPublic).IsRequired();
            entity.Property(e => e.Type).IsRequired();
            entity.Property(e => e.Features)
                .HasColumnType("nvarchar(max)")
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, new System.Text.Json.JsonSerializerOptions()),
                    v => System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, PlanFeature>>(v, new System.Text.Json.JsonSerializerOptions())
                );
            entity.Property(e => e.Limits)
                .HasColumnType("nvarchar(max)")
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, new System.Text.Json.JsonSerializerOptions()),
                    v => System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, int>>(v, new System.Text.Json.JsonSerializerOptions())
                );
            entity.Property(e => e.StripePriceIdMonthly).HasMaxLength(64);
            entity.Property(e => e.StripePriceIdYearly).HasMaxLength(64);
            entity.Property(e => e.TrialDays).IsRequired();
            entity.Property(e => e.SortOrder).IsRequired();
            entity.HasMany(e => e.Subscriptions)
                  .WithOne(s => s.Plan)
                  .HasForeignKey(s => s.PlanId);
        });

        builder.Entity<Subscription>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TenantId).IsRequired();
            entity.Property(e => e.PlanId).IsRequired();
            entity.Property(e => e.Status).IsRequired();
            entity.Property(e => e.StartDate).IsRequired();
            entity.Property(e => e.EndDate);
            entity.Property(e => e.NextBillingDate).IsRequired();
            entity.Property(e => e.BillingCycle).IsRequired();
            entity.Property(e => e.Amount).HasColumnType("decimal(18,2)").IsRequired();
            entity.Property(e => e.Currency).IsRequired().HasMaxLength(8);
            entity.Property(e => e.StripeSubscriptionId).HasMaxLength(64);
            entity.Property(e => e.StripeCustomerId).HasMaxLength(64);
            entity.Property(e => e.Metadata)
                .HasColumnType("nvarchar(max)")
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, new System.Text.Json.JsonSerializerOptions()),
                    v => System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(v, new System.Text.Json.JsonSerializerOptions())
                );
            entity.Property(e => e.TrialDays);
            entity.Property(e => e.TrialEndDate);

            entity.HasOne(e => e.Plan)
                .WithMany(p => p.Subscriptions)
                .HasForeignKey(e => e.PlanId);

            entity.HasMany(e => e.Invoices)
                .WithOne(i => i.Subscription)
                .HasForeignKey(i => i.SubscriptionId);

            entity.HasMany(e => e.UsageRecords)
                .WithOne(ur => ur.Subscription)
                .HasForeignKey(ur => ur.SubscriptionId);
        });

        builder.Entity<Invoice>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TenantId);
            entity.Property(e => e.IssueDate);
            entity.Property(e => e.DueDate);
            entity.Property(e => e.PaidDate);
            entity.Property(e => e.PeriodStart);
            entity.Property(e => e.PeriodEnd);
            entity.Property(e => e.Metadata)
                 .HasColumnType("nvarchar(max)")
                 .HasConversion(
                     v => System.Text.Json.JsonSerializer.Serialize(v, new System.Text.Json.JsonSerializerOptions()),
                     v => System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(v, new System.Text.Json.JsonSerializerOptions())
                 );
        });

        builder.Entity<UsageRecord>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TenantId).IsRequired();
            entity.Property(e => e.SubscriptionId);
            entity.Property(e => e.MetricName).IsRequired().HasMaxLength(128);
            entity.Property(e => e.Quantity).IsRequired();
            entity.Property(e => e.RecordedAt).IsRequired();
            entity.Property(e => e.PeriodStart).IsRequired();
            entity.Property(e => e.PeriodEnd).IsRequired();
            entity.Property(e => e.Metadata)
                .HasColumnType("nvarchar(max)")
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, new System.Text.Json.JsonSerializerOptions()),
                    v => System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(v, new System.Text.Json.JsonSerializerOptions())
                );
            entity.HasOne(e => e.Subscription)
                .WithMany(s => s.UsageRecords)
                .HasForeignKey(e => e.SubscriptionId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<PaymentMethod>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TenantId);
            entity.Property(e => e.Metadata)
                .HasColumnType("nvarchar(max)")
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, new System.Text.Json.JsonSerializerOptions()),
                    v => System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(v, new System.Text.Json.JsonSerializerOptions())
                );
        });

        builder.Entity<BillingAddress>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TenantId);
        });

        builder.Entity<InvoiceLineItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.InvoiceId).IsRequired();
            entity.Property(e => e.Description);
            entity.Property(e => e.Amount).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Quantity);
            entity.Property(e => e.Metadata)
                  .HasColumnType("nvarchar(max)")
                  .HasConversion(
                      v => System.Text.Json.JsonSerializer.Serialize(v, new System.Text.Json.JsonSerializerOptions()),
                      v => System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(v, new System.Text.Json.JsonSerializerOptions())
                  );
            entity.HasOne<Invoice>()
                  .WithMany(i => i.LineItems)
                  .HasForeignKey(e => e.InvoiceId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<NotificationPreference>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UserId);
            entity.Property(e => e.TenantId);
            entity.Property(e => e.Settings)
                  .HasColumnType("nvarchar(max)")
                  .HasConversion(
                      v => System.Text.Json.JsonSerializer.Serialize(v, new System.Text.Json.JsonSerializerOptions()),
                      v => System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(v, new System.Text.Json.JsonSerializerOptions())
                  );
        });

        builder.Entity<DocumentChunk>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.DocumentId);
            entity.Property(e => e.TenantId);
            entity.Property(e => e.Metadata)
                 .HasColumnType("nvarchar(max)")
                 .HasConversion(
                     v => System.Text.Json.JsonSerializer.Serialize(v, new System.Text.Json.JsonSerializerOptions()),
                     v => System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(v, new System.Text.Json.JsonSerializerOptions())
                 );
        });

        builder.Entity<Document>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(256);
            entity.Property(e => e.Content);
            entity.Property(e => e.OriginalFileName).HasMaxLength(256);
            entity.Property(e => e.FileType).HasMaxLength(64);
            entity.Property(e => e.FileSize);
            entity.Property(e => e.FilePath).HasMaxLength(512);
            entity.Property(e => e.Status);
            entity.Property(e => e.TenantId).IsRequired();
            entity.Property(e => e.UploadedByUserId).IsRequired();
            entity.Property(e => e.Summary);
            entity.Property(e => e.Tags)
                .HasConversion(
                    v => string.Join(',', v),
                    v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList()
                );
            entity.Property(e => e.Language).HasMaxLength(16);
            entity.Property(e => e.Metadata)
                .HasColumnType("nvarchar(max)")
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, new System.Text.Json.JsonSerializerOptions()),
                    v => System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(v, new System.Text.Json.JsonSerializerOptions())
                );
            entity.Property(e => e.ChunkCount);
            entity.Property(e => e.IsEmbedded);
            entity.Property(e => e.VectorCollectionName).HasMaxLength(128);
        });

        builder.Entity<WorkflowDefinition>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasMany(e => e.Steps)
                  .WithOne();
        });

        builder.Entity<WorkflowStep>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).IsRequired().HasMaxLength(64);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(128);
            entity.Property(e => e.Type).IsRequired();
            entity.Property(e => e.Configuration)
                .HasColumnType("nvarchar(max)")
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, new System.Text.Json.JsonSerializerOptions()),
                    v => System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(v, new System.Text.Json.JsonSerializerOptions())
                );
            entity.Property(e => e.Position)
                .HasColumnType("nvarchar(max)")
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, new System.Text.Json.JsonSerializerOptions()),
                    v => System.Text.Json.JsonSerializer.Deserialize<WorkflowStepPosition>(v, new System.Text.Json.JsonSerializerOptions())
                );
            entity.Property(e => e.InputPorts)
                .HasConversion(
                    v => string.Join(',', v),
                    v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList()
                );
            entity.Property(e => e.OutputPorts)
                .HasConversion(
                    v => string.Join(',', v),
                    v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList()
                );
            entity.Property(e => e.IsStartStep).IsRequired();
            entity.Property(e => e.IsEndStep).IsRequired();
            entity.Property(e => e.Condition)
                .HasColumnType("nvarchar(max)")
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, new System.Text.Json.JsonSerializerOptions()),
                    v => System.Text.Json.JsonSerializer.Deserialize<WorkflowStepCondition>(v, new System.Text.Json.JsonSerializerOptions())
                );
        });

        builder.Entity<WorkflowLayout>(entity =>
        {
            entity.HasKey("Width", "Height", "Zoom");
            entity.Property(e => e.Width).IsRequired();
            entity.Property(e => e.Height).IsRequired();
            entity.Property(e => e.Zoom).IsRequired();
            entity.Property(e => e.ViewportPosition)
                .HasColumnType("nvarchar(max)")
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, new System.Text.Json.JsonSerializerOptions()),
                    v => System.Text.Json.JsonSerializer.Deserialize<WorkflowStepPosition>(v, new System.Text.Json.JsonSerializerOptions())
                );
        });

        builder.Entity<WorkflowConnection>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).IsRequired().HasMaxLength(64);
            entity.Property(e => e.SourceStepId).IsRequired().HasMaxLength(64);
            entity.Property(e => e.TargetStepId).IsRequired().HasMaxLength(64);
        });

        builder.Entity<WorkflowTemplate>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TenantId);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(128);
            entity.Property(e => e.Description).HasMaxLength(512);
            entity.Property(e => e.Category).HasMaxLength(64);
            entity.Property(e => e.Definition)
                .HasColumnType("nvarchar(max)")
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, new System.Text.Json.JsonSerializerOptions()),
                    v => System.Text.Json.JsonSerializer.Deserialize<WorkflowDefinition>(v, new System.Text.Json.JsonSerializerOptions())
                );
            entity.Property(e => e.DefaultVariables)
                .HasColumnType("nvarchar(max)")
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, new System.Text.Json.JsonSerializerOptions()),
                    v => System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(v, new System.Text.Json.JsonSerializerOptions())
                );
            entity.Property(e => e.Tags)
                .HasConversion(
                    v => string.Join(',', v),
                    v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList()
                );
            entity.Property(e => e.IsPublic).IsRequired();
            entity.Property(e => e.UsageCount).IsRequired();
            entity.Property(e => e.Rating).IsRequired();
        });

        builder.Entity<NotificationTemplate>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TenantId);
            entity.Property(e => e.DefaultData)
                  .HasColumnType("nvarchar(max)")
                  .HasConversion(
                      v => System.Text.Json.JsonSerializer.Serialize(v, new System.Text.Json.JsonSerializerOptions()),
                      v => System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(v, new System.Text.Json.JsonSerializerOptions())
                  );
        });

        builder.Entity<NotificationPreference>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UserId);
            entity.Property(e => e.TenantId);
        });

        builder.Entity<WorkflowConnectionCondition>(entity =>
        {
            entity.HasKey("Expression");
            entity.Property(e => e.Expression).IsRequired().HasMaxLength(512);
            entity.Property(e => e.Variables)
                .HasColumnType("nvarchar(max)")
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, new System.Text.Json.JsonSerializerOptions()),
                    v => System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(v, new System.Text.Json.JsonSerializerOptions())
                );
        });
        builder.Entity<ChatbotConfig>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TenantId).IsRequired();
            entity.Property(e => e.Name).IsRequired().HasMaxLength(128);
            entity.Property(e => e.Description).HasMaxLength(512);
            entity.Property(e => e.Configuration)
                .HasColumnType("nvarchar(max)")
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, new System.Text.Json.JsonSerializerOptions()),
                    v => System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(v, new System.Text.Json.JsonSerializerOptions())
                );
            
            entity.Property(e => e.IsActive).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt);
        });
        base.OnModelCreating(builder);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedBy = _currentUserService.UserId?.ToString();
                    entry.Entity.CreatedAt = DateTime.UtcNow;
                    entry.Entity.TenantId = _tenantService.GetCurrentTenantId();
                    break;

                case EntityState.Modified:
                    entry.Entity.UpdatedBy = _currentUserService.UserId?.ToString();
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                    break;
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}

