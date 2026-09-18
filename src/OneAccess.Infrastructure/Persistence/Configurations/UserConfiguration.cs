using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OneAccess.Domain.Entities;

namespace OneAccess.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Username)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(u => u.Username)
            .IsUnique();

        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(256);

        builder.HasIndex(u => u.Email)
            .IsUnique();

        builder.Property(u => u.PasswordHash)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(u => u.FullName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(u => u.Status)
            .IsRequired();

        builder.Property(u => u.IsSystemAdministrator)
            .IsRequired();

        builder.HasOne(u => u.Division)
            .WithMany(d => d.Users)
            .HasForeignKey(u => u.DivisionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(u => u.Section)
            .WithMany(s => s.Users)
            .HasForeignKey(u => u.SectionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
