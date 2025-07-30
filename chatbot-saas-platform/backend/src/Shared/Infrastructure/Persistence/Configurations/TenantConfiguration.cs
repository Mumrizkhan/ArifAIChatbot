using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shared.Domain.Entities;

namespace Shared.Infrastructure.Persistence.Configurations;

public class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.HasKey(t => t.Id);
        
        builder.Property(t => t.Name)
            .IsRequired()
            .HasMaxLength(200);
            
        builder.Property(t => t.Subdomain)
            .IsRequired()
            .HasMaxLength(50);
            
        builder.HasIndex(t => t.Subdomain)
            .IsUnique();
            
        builder.Property(t => t.CustomDomain)
            .HasMaxLength(100);
            
        builder.Property(t => t.DatabaseConnectionString)
            .IsRequired();
            
        builder.Property(t => t.LogoUrl)
            .HasMaxLength(500);
            
        builder.Property(t => t.PrimaryColor)
            .HasMaxLength(7)
            .HasDefaultValue("#3B82F6");
            
        builder.Property(t => t.SecondaryColor)
            .HasMaxLength(7)
            .HasDefaultValue("#64748B");
            
        builder.Property(t => t.DefaultLanguage)
            .HasMaxLength(10)
            .HasDefaultValue("en");
            
        builder.Ignore(t => t.Settings);
    }
}
