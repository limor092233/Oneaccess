using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OneAccess.Domain.Entities;

namespace OneAccess.Infrastructure.Persistence.Configurations;

public class UserSubSystemAccessConfiguration : IEntityTypeConfiguration<UserSubSystemAccess>
{
    public void Configure(EntityTypeBuilder<UserSubSystemAccess> builder)
    {
        builder.ToTable("UserSubSystemAccesses");

        builder.HasKey(usa => new { usa.UserId, usa.SubSystemId });

        builder.HasIndex(usa => new { usa.UserId, usa.SubSystemId })
            .IsUnique();

        builder.Property(usa => usa.GrantedAt)
            .IsRequired();

        builder.HasOne(usa => usa.User)
            .WithMany(u => u.UserSubSystemAccesses)
            .HasForeignKey(usa => usa.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(usa => usa.SubSystem)
            .WithMany(s => s.UserSubSystemAccesses)
            .HasForeignKey(usa => usa.SubSystemId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
