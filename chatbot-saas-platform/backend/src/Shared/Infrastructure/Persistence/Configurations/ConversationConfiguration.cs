using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shared.Domain.Entities;

namespace Shared.Infrastructure.Persistence.Configurations;

public class ConversationConfiguration : IEntityTypeConfiguration<Conversation>
{
    public void Configure(EntityTypeBuilder<Conversation> builder)
    {
        builder.HasKey(c => c.Id);
        
            
        builder.Property(c => c.Channel)
            .IsRequired()
            .HasConversion<string>();
            
        builder.Property(c => c.Status)
            .IsRequired()
            .HasConversion<string>();
            
        builder.HasMany(c => c.Messages)
            .WithOne(m => m.Conversation)
            .HasForeignKey(m => m.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);
            
        builder.HasOne(c => c.Tenant)
            .WithMany()
            .HasForeignKey(c => c.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
            
        builder.HasIndex(c => c.TenantId);
        builder.HasIndex(c => c.CreatedAt);
        builder.HasIndex(c => c.Status);
        
    }
}
