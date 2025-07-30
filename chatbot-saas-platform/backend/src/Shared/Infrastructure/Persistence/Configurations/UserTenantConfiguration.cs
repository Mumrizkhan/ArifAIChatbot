using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shared.Domain.Entities;

namespace Shared.Infrastructure.Persistence.Configurations;

public class UserTenantConfiguration : IEntityTypeConfiguration<UserTenant>
{
    public void Configure(EntityTypeBuilder<UserTenant> builder)
    {
        // Composite key: UserId + TenantId
        builder.HasKey(ut => new { ut.UserId, ut.TenantId });

        // Role property: required, enum to string conversion
        builder.Property(ut => ut.Role)
            .IsRequired()
            .HasConversion<string>();

        // IsActive property: required
        builder.Property(ut => ut.IsActive)
            .IsRequired();

        // JoinedAt property: required
        builder.Property(ut => ut.JoinedAt)
            .IsRequired();

        // Relationships
        builder.HasOne(ut => ut.User)
            .WithMany(u => u.UserTenants)
            .HasForeignKey(ut => ut.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(ut => ut.Tenant)
            .WithMany(t => t.UserTenants)
            .HasForeignKey(ut => ut.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(ut => ut.TenantId);
        builder.HasIndex(ut => ut.Role);
        builder.HasIndex(ut => ut.IsActive);
    }
}
