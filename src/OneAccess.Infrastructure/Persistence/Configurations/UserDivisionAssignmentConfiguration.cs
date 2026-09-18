using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OneAccess.Domain.Entities;

namespace OneAccess.Infrastructure.Persistence.Configurations;

public class UserDivisionAssignmentConfiguration : IEntityTypeConfiguration<UserDivisionAssignment>
{
    public void Configure(EntityTypeBuilder<UserDivisionAssignment> builder)
    {
        builder.ToTable("UserDivisionAssignments");

        builder.HasKey(uda => new { uda.UserId, uda.DivisionId });

        builder.Property(uda => uda.GrantedAt)
            .IsRequired();

        builder.HasOne(uda => uda.User)
            .WithMany(u => u.UserDivisionAssignments)
            .HasForeignKey(uda => uda.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(uda => uda.Division)
            .WithMany(d => d.UserDivisionAssignments)
            .HasForeignKey(uda => uda.DivisionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
