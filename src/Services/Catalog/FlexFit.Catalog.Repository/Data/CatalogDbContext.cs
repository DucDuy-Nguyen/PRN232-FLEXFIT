using FlexFit.Catalog.Repository.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using FlexFit.Catalog.Repository.Models;

namespace FlexFit.Catalog.Repository.Data;

public partial class CatalogDbContext : DbContext
{
    public CatalogDbContext()
    {
    }

    public CatalogDbContext(DbContextOptions<CatalogDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Branch> Branches { get; set; }
    public virtual DbSet<BranchImage> BranchImages { get; set; }
    public virtual DbSet<BranchStaff> BranchStaffs { get; set; }
    public virtual DbSet<Category> Categories { get; set; }
    public virtual DbSet<Class> Classes { get; set; }
    public virtual DbSet<ClassSchedule> ClassSchedules { get; set; }
    public virtual DbSet<FavoriteGym> FavoriteGyms { get; set; }
    public virtual DbSet<FavoriteClass> FavoriteClasses { get; set; }
    public virtual DbSet<Gym> Gyms { get; set; }
    public virtual DbSet<GymAmenity> GymAmenities { get; set; }
    public virtual DbSet<GymImage> GymImages { get; set; }
    public virtual DbSet<GymSession> GymSessions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Branch>(entity =>
        {
            entity.HasKey(e => e.BranchId).HasName("PK__Branches__A1682FC57F628C8E");

            entity.Property(e => e.BranchId).HasDefaultValueSql("(newid())");
            entity.Property(e => e.Address).HasMaxLength(255);
            entity.Property(e => e.BranchName).HasMaxLength(150);
            entity.Property(e => e.City).HasMaxLength(100);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.District).HasMaxLength(100);
            entity.Property(e => e.IsActive).HasDefaultValue(true);

            entity.HasOne(d => d.Gym).WithMany(p => p.Branches)
                .HasForeignKey(d => d.GymId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Branches__GymId__5812160E");

            entity.HasMany(d => d.Amenities).WithMany(p => p.Branches)
                .UsingEntity<Dictionary<string, object>>(
                    "BranchAmenityMapping",
                    r => r.HasOne<GymAmenity>().WithMany()
                        .HasForeignKey("AmenityId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK__BranchAme__Ameni__6E01572D"),
                    l => l.HasOne<Branch>().WithMany()
                        .HasForeignKey("BranchId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK__BranchAme__Branc__6D0D32F4"),
                    j =>
                    {
                        j.HasKey("BranchId", "AmenityId");
                        j.ToTable("BranchAmenityMappings");
                    });
        });

        modelBuilder.Entity<BranchImage>(entity =>
        {
            entity.HasKey(e => e.BranchImageId).HasName("PK__BranchIm__DEDBCB2E0DD9038B");

            entity.Property(e => e.BranchImageId).HasDefaultValueSql("(newid())");

            entity.HasOne(d => d.Branch).WithMany(p => p.BranchImages)
                .HasForeignKey(d => d.BranchId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK__BranchIma__Branc__66603565");
        });

        modelBuilder.Entity<BranchStaff>(entity =>
        {
            entity.HasKey(e => new { e.StaffId, e.BranchId });

            entity.Property(e => e.AssignedAt).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.Branch).WithMany(p => p.BranchStaffs)
                .HasForeignKey(d => d.BranchId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BranchStaffs_Branches");
            
            // Note: FK to Users is removed
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.CategoryId).HasName("PK__Categori__19093A0BC8C5ABA1");

            entity.HasIndex(e => e.CategoryName, "UQ__Categori__8517B2E053ED6742").IsUnique();

            entity.Property(e => e.CategoryId).HasDefaultValueSql("(newid())");
            entity.Property(e => e.CategoryName).HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(255);
        });

        modelBuilder.Entity<Class>(entity =>
        {
            entity.HasKey(e => e.ClassId).HasName("PK__Classes__CB1927C00781F387");

            entity.Property(e => e.ClassId).HasDefaultValueSql("(newid())");
            entity.Property(e => e.ClassName).HasMaxLength(150);
            entity.Property(e => e.CoachName).HasMaxLength(100);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.DifficultyLevel).HasMaxLength(30);
            entity.Property(e => e.Status)
                .HasMaxLength(30)
                .HasDefaultValue("Open");

            entity.HasOne(d => d.Branch).WithMany(p => p.Classes)
                .HasForeignKey(d => d.BranchId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Classes__BranchI__7D439ABD");

            entity.HasOne(d => d.Category).WithMany(p => p.Classes)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Classes__Categor__7E37BEF6");
        });

        modelBuilder.Entity<ClassSchedule>(entity =>
        {
            entity.HasKey(e => e.ScheduleId).HasName("PK__ClassSch__9C8A5B49CB2537F1");

            entity.Property(e => e.ScheduleId).HasDefaultValueSql("(newid())");

            entity.HasOne(d => d.Class).WithMany(p => p.ClassSchedules)
                .HasForeignKey(d => d.ClassId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ClassSche__Class__02084FDA");
        });

        modelBuilder.Entity<FavoriteGym>(entity =>
        {
            entity.HasKey(e => new { e.UserId, e.GymId });

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.Gym).WithMany(p => p.FavoriteGyms)
                .HasForeignKey(d => d.GymId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__FavoriteG__GymId__4B7734FF");

            // Note: FK to Users is removed
        });

        modelBuilder.Entity<FavoriteClass>(entity =>
        {
            entity.HasKey(e => new { e.UserId, e.ClassId });

            entity.ToTable("FavoriteClasses");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.Class)
                .WithMany(p => p.FavoriteClasses)
                .HasForeignKey(d => d.ClassId)
                .OnDelete(DeleteBehavior.Cascade);
            
            // Note: FK to Users is removed
        });

        modelBuilder.Entity<Gym>(entity =>
        {
            entity.HasKey(e => e.GymId).HasName("PK__Gyms__1A3A7C967D8AF47B");

            entity.Property(e => e.GymId).HasDefaultValueSql("(newid())");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.GymName).HasMaxLength(150);
            entity.Property(e => e.PhoneNumber).HasMaxLength(20);
            entity.Property(e => e.RatingAverage).HasColumnType("decimal(3, 2)");
            entity.Property(e => e.Status)
                .HasMaxLength(30)
                .HasDefaultValue("Pending");

            // Note: FK to Users is removed
        });

        modelBuilder.Entity<GymAmenity>(entity =>
        {
            entity.HasKey(e => e.AmenityId).HasName("PK__GymAmeni__842AF50BF3A9D836");

            entity.HasIndex(e => e.AmenityName, "UQ__GymAmeni__7B4A459F1F1C7085").IsUnique();

            entity.Property(e => e.AmenityId).HasDefaultValueSql("(newid())");
            entity.Property(e => e.AmenityName).HasMaxLength(100);
        });

        modelBuilder.Entity<GymImage>(entity =>
        {
            entity.HasKey(e => e.GymImageId).HasName("PK__GymImage__659DCAAE6F641A53");

            entity.Property(e => e.GymImageId).HasDefaultValueSql("(newid())");

            entity.HasOne(d => d.Gym).WithMany(p => p.GymImages)
                .HasForeignKey(d => d.GymId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__GymImages__GymId__619B8048");
        });

        modelBuilder.Entity<GymSession>(entity =>
        {
            entity.HasKey(e => e.SessionId).HasName("PK__GymSessi__C9F492905AF95180");

            entity.Property(e => e.SessionId).HasDefaultValueSql("(newid())");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.SessionName).HasMaxLength(150);
            entity.Property(e => e.Status)
                .HasMaxLength(30)
                .HasDefaultValue("Open");

            entity.HasOne(d => d.Branch).WithMany(p => p.GymSessions)
                .HasForeignKey(d => d.BranchId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__GymSessio__Branc__778AC167");
        });
    }
}


