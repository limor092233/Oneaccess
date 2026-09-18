using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OneAccess.Domain.Entities;

namespace OneAccess.Infrastructure.Persistence.Configurations;

public class SetupCodeConfiguration : IEntityTypeConfiguration<SetupCode>
{
    public void Configure(EntityTypeBuilder<SetupCode> builder)
    {
        builder.ToTable("SetupCodes");

        builder.HasKey(sc => sc.Id);

        builder.Property(sc => sc.CodeHash)
            .IsRequired()
            .HasMaxLength(256);

        builder.HasIndex(sc => sc.CodeHash)
            .IsUnique();

        builder.Property(sc => sc.ExpiresAt)
            .IsRequired();
    }
}
