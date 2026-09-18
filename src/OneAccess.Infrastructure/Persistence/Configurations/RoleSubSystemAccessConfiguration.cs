using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OneAccess.Domain.Entities;

namespace OneAccess.Infrastructure.Persistence.Configurations;

public class RoleSubSystemAccessConfiguration : IEntityTypeConfiguration<RoleSubSystemAccess>
{
    public void Configure(EntityTypeBuilder<RoleSubSystemAccess> builder)
    {
        builder.ToTable("RoleSubSystemAccesses");

        builder.HasKey(rsa => new { rsa.RoleId, rsa.SubSystemId });

        builder.Property(rsa => rsa.GrantedAt)
            .IsRequired();

        builder.HasOne(rsa => rsa.Role)
            .WithMany(r => r.RoleSubSystemAccesses)
            .HasForeignKey(rsa => rsa.RoleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(rsa => rsa.SubSystem)
            .WithMany(s => s.RoleSubSystemAccesses)
            .HasForeignKey(rsa => rsa.SubSystemId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
