using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FlexFit.Identity.API.Models.Entities;

namespace FlexFit.Identity.API.Data.Configurations;

public sealed class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> builder)
    {
        // Table Name
        builder.ToTable("UserRoles");

        // Composite Primary Key
        builder.HasKey(e => new { e.UserId, e.RoleId });

        builder.Property(e => e.AssignedAt)
            .HasDefaultValueSql("(getdate())")
            .IsRequired();

        // Foreign Key Relationships
        builder.HasOne(d => d.Role)
            .WithMany(p => p.UserRoles)
            .HasForeignKey(d => d.RoleId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_UserRoles_Roles");

        builder.HasOne(d => d.User)
            .WithMany(p => p.UserRoles)
            .HasForeignKey(d => d.UserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_UserRoles_Users");
    }
}
