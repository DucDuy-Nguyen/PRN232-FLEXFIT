using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FlexFit.Identity.Domain.Entities;

namespace FlexFit.Identity.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        // Table Name
        builder.ToTable("Users");

        // Primary Key
        builder.HasKey(e => e.UserId)
            .HasName("PK__Users__1788CC4C75B61DB2");

        // Guid PK default to newid() in SQL Server
        builder.Property(e => e.UserId)
            .HasDefaultValueSql("(newid())");

        // Properties and constraints matching monolith
        builder.Property(e => e.FullName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.Email)
            .HasMaxLength(100)
            .IsRequired();

        // Unique index on Email matching monolith
        builder.HasIndex(e => e.Email, "UQ__Users__A9D1053421A327A4")
            .IsUnique();

        builder.Property(e => e.PasswordHash)
            .IsRequired();

        builder.Property(e => e.PhoneNumber)
            .HasMaxLength(20);

        builder.Property(e => e.IsActive)
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(e => e.IsEmailVerified)
            .IsRequired();

        builder.Property(e => e.GoogleSubject)
            .HasMaxLength(255);

        // Filtered unique index on GoogleSubject so duplicate links are rejected but nulls are allowed
        builder.HasIndex(e => e.GoogleSubject, "UQ_Users_GoogleSubject")
            .IsUnique()
            .HasFilter("[GoogleSubject] IS NOT NULL");

        builder.Property(e => e.CreatedAt)
            .HasDefaultValueSql("(getdate())")
            .IsRequired();

        // Deprecated OTP fields retained for seamless legacy migration
        #pragma warning disable CS0618
        builder.Property(e => e.EmailVerificationToken);
        builder.Property(e => e.VerificationTokenExpires);
        #pragma warning restore CS0618
    }
}
