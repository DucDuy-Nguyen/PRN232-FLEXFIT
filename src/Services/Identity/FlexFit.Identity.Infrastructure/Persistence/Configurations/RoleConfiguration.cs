using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FlexFit.Identity.Domain.Entities;

namespace FlexFit.Identity.Infrastructure.Persistence.Configurations;

public sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        // Table Name
        builder.ToTable("Roles");

        // Primary Key
        builder.HasKey(e => e.RoleId)
            .HasName("PK__Roles__8AFACE1AD46A3A12");

        // Guid PK default to newid() in SQL Server
        builder.Property(e => e.RoleId)
            .HasDefaultValueSql("(newid())");

        builder.Property(e => e.RoleName)
            .HasMaxLength(50)
            .IsRequired();

        // Unique index on RoleName
        builder.HasIndex(e => e.RoleName, "UQ__Roles__8A2B6160ACCD1031")
            .IsUnique();

        builder.Property(e => e.Description)
            .HasMaxLength(255);

        builder.Property(e => e.CreatedAt)
            .HasDefaultValueSql("(getdate())")
            .IsRequired();

        // Seed default roles with deterministic GUIDs
        // EF Core requires using private reflection or adding a constructor, but since entity properties
        // have private setters in clean architecture, we can populate it via reflection or set it in seed data:
        // Actually, EF Core can map properties with private setters during seed data if we construct them using an anonymous type or private constructor:
        // Let's use anonymous objects inside HasData, which EF Core natively maps to shadowed properties / private setters.
        builder.HasData(
            new { RoleId = Guid.Parse("d7f8a7cf-cf06-4447-9204-ccf3b2ee0001"), RoleName = "Admin", Description = "System Administrator", CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new { RoleId = Guid.Parse("d7f8a7cf-cf06-4447-9204-ccf3b2ee0002"), RoleName = "Member", Description = "Gym Member", CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new { RoleId = Guid.Parse("d7f8a7cf-cf06-4447-9204-ccf3b2ee0003"), RoleName = "GymPartner", Description = "Gym Owner / Partner", CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new { RoleId = Guid.Parse("d7f8a7cf-cf06-4447-9204-ccf3b2ee0004"), RoleName = "Staff", Description = "Gym Staff member", CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
        );
    }
}
