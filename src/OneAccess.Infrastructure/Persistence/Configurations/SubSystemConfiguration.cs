using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OneAccess.Domain.Entities;

namespace OneAccess.Infrastructure.Persistence.Configurations;

public class SubSystemConfiguration : IEntityTypeConfiguration<SubSystem>
{
    public void Configure(EntityTypeBuilder<SubSystem> builder)
    {
        builder.ToTable("SubSystems");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Code)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(s => s.Code)
            .IsUnique();

        builder.Property(s => s.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(s => s.BaseUrl)
            .HasMaxLength(256);

        builder.Property(s => s.Audience)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(s => s.Audience)
            .IsUnique();

        builder.Property(s => s.IsActive)
            .IsRequired();
    }
}
