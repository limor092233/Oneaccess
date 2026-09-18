using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OneAccess.Domain.Entities;

namespace OneAccess.Infrastructure.Persistence.Configurations;

public class SectionConfiguration : IEntityTypeConfiguration<Section>
{
    public void Configure(EntityTypeBuilder<Section> builder)
    {
        builder.ToTable("Sections");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(s => new { s.DivisionId, s.Name })
            .IsUnique();

        builder.Property(s => s.Description)
            .HasMaxLength(256);

        builder.Property(s => s.CreatedAt)
            .IsRequired();

        builder.HasOne(s => s.Division)
            .WithMany(d => d.Sections)
            .HasForeignKey(s => s.DivisionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
