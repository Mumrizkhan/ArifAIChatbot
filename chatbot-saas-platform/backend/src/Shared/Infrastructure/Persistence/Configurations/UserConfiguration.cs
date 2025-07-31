using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shared.Domain.Entities;
using Shared.Domain.Enums;

namespace Shared.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(u => u.Id);

        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(256);

        builder.HasIndex(u => u.Email)
            .IsUnique();

        builder.Property(u => u.FirstName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(u => u.LastName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(u => u.PasswordHash)
            .IsRequired();

        builder.Property(u => u.PreferredLanguage)
            .HasMaxLength(10)
            .HasDefaultValue("en");

        builder.Property(u => u.PhoneNumber)
            .HasMaxLength(20);

        builder.Property(u => u.IsActive)
            .IsRequired();

        builder.Property(u => u.EmailConfirmed)
            .IsRequired();

        builder.Property(u => u.PhoneNumberConfirmed)
            .IsRequired();

        builder.Property(u => u.TwoFactorEnabled)
            .IsRequired();

        builder.Property(u => u.LastLoginAt);

        builder.Property(u => u.Role)
            .IsRequired();

        builder.HasMany(u => u.UserTenants)
            .WithOne(ut => ut.User)
            .HasForeignKey(ut => ut.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(u => u.AssignedConversations)
    .WithOne(c => c.AssignedAgent)
    .HasForeignKey(c => c.AssignedAgentId)
    .OnDelete(DeleteBehavior.SetNull);
        builder.Property(u => u.AvatarUrl)
            .HasMaxLength(500);
        builder.Property(u => u.PasswordResetToken)
            .HasMaxLength(100);
        builder.Property(u => u.PasswordResetTokenExpiry);
        builder.Property(u => u.IsOnline)
            .IsRequired()
            .HasDefaultValue(false);
        builder.Property(u => u.Status);
           
    }
}
