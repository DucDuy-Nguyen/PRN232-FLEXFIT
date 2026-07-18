using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FlexFit.Identity.API.Models.Entities;

namespace FlexFit.Identity.API.Data.Configurations;

public sealed class MemberProfileConfiguration : IEntityTypeConfiguration<MemberProfile>
{
    public void Configure(EntityTypeBuilder<MemberProfile> builder)
    {
        // Table Name
        builder.ToTable("MemberProfiles");

        // Primary Key
        builder.HasKey(e => e.MemberProfileId)
            .HasName("PK__MemberPr__0485209F89155E84");

        // Default newid()
        builder.Property(e => e.MemberProfileId)
            .HasDefaultValueSql("(newid())");

        // Unique index on UserId
        builder.HasIndex(e => e.UserId, "UQ__MemberPr__1788CC4D526A5F60")
            .IsUnique();

        builder.Property(e => e.Gender)
            .HasMaxLength(20);

        builder.Property(e => e.HeightCm)
            .HasColumnType("decimal(5, 2)");

        builder.Property(e => e.WeightKg)
            .HasColumnType("decimal(5, 2)");

        builder.Property(e => e.FitnessGoal)
            .HasMaxLength(255);

        builder.Property(e => e.ActivityLevel)
            .HasMaxLength(50);

        builder.Property(e => e.PreferredWorkoutTime)
            .HasMaxLength(50);

        builder.Property(e => e.Bio);

        // One-to-one relationship with User
        builder.HasOne(d => d.User)
            .WithOne(p => p.MemberProfile)
            .HasForeignKey<MemberProfile>(d => d.UserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK__MemberPro__UserI__49C3F6B7");
    }
}
