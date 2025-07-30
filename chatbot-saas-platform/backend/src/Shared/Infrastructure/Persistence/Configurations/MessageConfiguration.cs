using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shared.Domain.Entities;

namespace Shared.Infrastructure.Persistence.Configurations;

public class MessageConfiguration : IEntityTypeConfiguration<Message>
{
    public void Configure(EntityTypeBuilder<Message> builder)
    {
        builder.HasKey(m => m.Id);
        
        builder.Property(m => m.Content)
            .IsRequired()
            .HasMaxLength(4000);
            
        builder.Property(m => m.Sender)
            .IsRequired()
            .HasConversion<string>();
            
        builder.Property(m => m.Type)
            .IsRequired()
            .HasConversion<string>();
            
        builder.HasOne(m => m.Conversation)
            .WithMany(c => c.Messages)
            .HasForeignKey(m => m.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);
            
            
        builder.HasIndex(m => m.ConversationId);
        builder.HasIndex(m => m.CreatedAt);
        
    }
}
