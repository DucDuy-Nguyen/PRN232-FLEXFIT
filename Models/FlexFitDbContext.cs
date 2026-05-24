using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace Flexfit.Models;

public partial class FlexFitDbContext : DbContext
{
    public FlexFitDbContext()
    {
    }

    public FlexFitDbContext(DbContextOptions<FlexFitDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Branch> Branches { get; set; }

    public virtual DbSet<BranchImage> BranchImages { get; set; }

    public virtual DbSet<BranchStaff> BranchStaffs { get; set; }

    public virtual DbSet<Category> Categories { get; set; }

    public virtual DbSet<CheckInLog> CheckInLogs { get; set; }

    public virtual DbSet<Class> Classes { get; set; }

    public virtual DbSet<ClassBooking> ClassBookings { get; set; }

    public virtual DbSet<ClassSchedule> ClassSchedules { get; set; }

    public virtual DbSet<CreditPackage> CreditPackages { get; set; }

    public virtual DbSet<CreditTransaction> CreditTransactions { get; set; }

    public virtual DbSet<FavoriteGym> FavoriteGyms { get; set; }

    public virtual DbSet<Gym> Gyms { get; set; }

    public virtual DbSet<GymAmenity> GymAmenities { get; set; }

    public virtual DbSet<GymBooking> GymBookings { get; set; }

    public virtual DbSet<GymImage> GymImages { get; set; }

    public virtual DbSet<GymSession> GymSessions { get; set; }

    public virtual DbSet<MemberProfile> MemberProfiles { get; set; }

    public virtual DbSet<Notification> Notifications { get; set; }

    public virtual DbSet<Payment> Payments { get; set; }

    public virtual DbSet<Promotion> Promotions { get; set; }

    public virtual DbSet<Review> Reviews { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<SystemLog> SystemLogs { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UserCredit> UserCredits { get; set; }

    public virtual DbSet<UserRole> UserRoles { get; set; }

    public virtual DbSet<UserWorkoutHistory> UserWorkoutHistories { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection");
        optionsBuilder.UseSqlServer(connectionString);
    }
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
                .OnDelete(DeleteBehavior.ClientSetNull)
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

            entity.HasOne(d => d.Staff).WithMany(p => p.BranchStaffs)
                .HasForeignKey(d => d.StaffId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BranchStaffs_Users");
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.CategoryId).HasName("PK__Categori__19093A0BC8C5ABA1");

            entity.HasIndex(e => e.CategoryName, "UQ__Categori__8517B2E053ED6742").IsUnique();

            entity.Property(e => e.CategoryId).HasDefaultValueSql("(newid())");
            entity.Property(e => e.CategoryName).HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(255);
        });

        modelBuilder.Entity<CheckInLog>(entity =>
        {
            entity.HasKey(e => e.CheckInLogId).HasName("PK__CheckInL__D713C446CD31F49B");

            entity.Property(e => e.CheckInLogId).HasDefaultValueSql("(newid())");
            entity.Property(e => e.Message).HasMaxLength(255);
            entity.Property(e => e.ScannedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Status).HasMaxLength(30);

            entity.HasOne(d => d.ClassBooking).WithMany(p => p.CheckInLogs)
                .HasForeignKey(d => d.ClassBookingId)
                .HasConstraintName("FK__CheckInLo__Class__2180FB33");

            entity.HasOne(d => d.GymBooking).WithMany(p => p.CheckInLogs)
                .HasForeignKey(d => d.GymBookingId)
                .HasConstraintName("FK__CheckInLo__GymBo__208CD6FA");

            entity.HasOne(d => d.ScannedByNavigation).WithMany(p => p.CheckInLogScannedByNavigations)
                .HasForeignKey(d => d.ScannedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__CheckInLo__Scann__22751F6C");

            entity.HasOne(d => d.User).WithMany(p => p.CheckInLogUsers)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__CheckInLo__UserI__1F98B2C1");
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

        modelBuilder.Entity<ClassBooking>(entity =>
        {
            entity.HasKey(e => e.BookingId).HasName("PK__ClassBoo__73951AEDEA9CD5B7");

            entity.HasIndex(e => e.BookingCode, "UQ__ClassBoo__C6E56BD580D57C58").IsUnique();

            entity.Property(e => e.BookingId).HasDefaultValueSql("(newid())");
            entity.Property(e => e.BookedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.BookingCode).HasMaxLength(50);
            entity.Property(e => e.CheckInStatus)
                .HasMaxLength(30)
                .HasDefaultValue("NotCheckedIn");
            entity.Property(e => e.QrToken).HasMaxLength(255);
            entity.Property(e => e.Status)
                .HasMaxLength(30)
                .HasDefaultValue("Booked");

            entity.HasOne(d => d.CheckedInByNavigation).WithMany(p => p.ClassBookingCheckedInByNavigations)
                .HasForeignKey(d => d.CheckedInBy)
                .HasConstraintName("FK__ClassBook__Check__1AD3FDA4");

            entity.HasOne(d => d.Class).WithMany(p => p.ClassBookings)
                .HasForeignKey(d => d.ClassId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ClassBook__Class__19DFD96B");

            entity.HasOne(d => d.User).WithMany(p => p.ClassBookingUsers)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ClassBook__UserI__18EBB532");
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

        modelBuilder.Entity<CreditPackage>(entity =>
        {
            entity.HasKey(e => e.PackageId).HasName("PK__CreditPa__322035CC8FA60D13");

            entity.Property(e => e.PackageId).HasDefaultValueSql("(newid())");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.PackageName).HasMaxLength(100);
            entity.Property(e => e.Price).HasColumnType("decimal(18, 2)");
        });

        modelBuilder.Entity<CreditTransaction>(entity =>
        {
            entity.HasKey(e => e.TransactionId).HasName("PK__CreditTr__55433A6B7CD8E897");

            entity.Property(e => e.TransactionId).HasDefaultValueSql("(newid())");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Description).HasMaxLength(255);
            entity.Property(e => e.ReferenceType).HasMaxLength(30);
            entity.Property(e => e.Type).HasMaxLength(30);

            entity.HasOne(d => d.User).WithMany(p => p.CreditTransactions)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__CreditTra__UserI__37703C52");
        });

        modelBuilder.Entity<FavoriteGym>(entity =>
        {
            entity.HasKey(e => new { e.UserId, e.GymId });

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.Gym).WithMany(p => p.FavoriteGyms)
                .HasForeignKey(d => d.GymId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__FavoriteG__GymId__4B7734FF");

            entity.HasOne(d => d.User).WithMany(p => p.FavoriteGyms)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__FavoriteG__UserI__4A8310C6");
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

            entity.HasOne(d => d.Owner).WithMany(p => p.Gyms)
                .HasForeignKey(d => d.OwnerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Gyms__OwnerId__5165187F");
        });

        modelBuilder.Entity<GymAmenity>(entity =>
        {
            entity.HasKey(e => e.AmenityId).HasName("PK__GymAmeni__842AF50BF3A9D836");

            entity.HasIndex(e => e.AmenityName, "UQ__GymAmeni__7B4A459F1F1C7085").IsUnique();

            entity.Property(e => e.AmenityId).HasDefaultValueSql("(newid())");
            entity.Property(e => e.AmenityName).HasMaxLength(100);
        });

        modelBuilder.Entity<GymBooking>(entity =>
        {
            entity.HasKey(e => e.BookingId).HasName("PK__GymBooki__73951AED2EDE095B");

            entity.HasIndex(e => e.BookingCode, "UQ__GymBooki__C6E56BD5C111A3CB").IsUnique();

            entity.Property(e => e.BookingId).HasDefaultValueSql("(newid())");
            entity.Property(e => e.BookedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.BookingCode).HasMaxLength(50);
            entity.Property(e => e.CheckInStatus)
                .HasMaxLength(30)
                .HasDefaultValue("NotCheckedIn");
            entity.Property(e => e.QrToken).HasMaxLength(255);
            entity.Property(e => e.Status)
                .HasMaxLength(30)
                .HasDefaultValue("Booked");

            entity.HasOne(d => d.CheckedInByNavigation).WithMany(p => p.GymBookingCheckedInByNavigations)
                .HasForeignKey(d => d.CheckedInBy)
                .HasConstraintName("FK__GymBookin__Check__0E6E26BF");

            entity.HasOne(d => d.Session).WithMany(p => p.GymBookings)
                .HasForeignKey(d => d.SessionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__GymBookin__Sessi__0D7A0286");

            entity.HasOne(d => d.User).WithMany(p => p.GymBookingUsers)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__GymBookin__UserI__0C85DE4D");
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

        modelBuilder.Entity<MemberProfile>(entity =>
        {
            entity.HasKey(e => e.MemberProfileId).HasName("PK__MemberPr__0485209F89155E84");

            entity.HasIndex(e => e.UserId, "UQ__MemberPr__1788CC4D526A5F60").IsUnique();

            entity.Property(e => e.MemberProfileId).HasDefaultValueSql("(newid())");
            entity.Property(e => e.ActivityLevel).HasMaxLength(50);
            entity.Property(e => e.FitnessGoal).HasMaxLength(255);
            entity.Property(e => e.Gender).HasMaxLength(20);
            entity.Property(e => e.HeightCm).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.PreferredWorkoutTime).HasMaxLength(50);
            entity.Property(e => e.WeightKg).HasColumnType("decimal(5, 2)");

            entity.HasOne(d => d.User).WithOne(p => p.MemberProfile)
                .HasForeignKey<MemberProfile>(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__MemberPro__UserI__49C3F6B7");
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.NotificationId).HasName("PK__Notifica__20CF2E12DD7CF72D");

            entity.Property(e => e.NotificationId).HasDefaultValueSql("(newid())");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Title).HasMaxLength(150);
            entity.Property(e => e.Type).HasMaxLength(50);

            entity.HasOne(d => d.User).WithMany(p => p.Notifications)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Notificat__UserI__51300E55");
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.PaymentId).HasName("PK__Payments__9B556A3832331F50");

            entity.Property(e => e.PaymentId).HasDefaultValueSql("(newid())");
            entity.Property(e => e.Amount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.PaymentMethod).HasMaxLength(50);
            entity.Property(e => e.ProviderTransactionCode).HasMaxLength(100);
            entity.Property(e => e.Status)
                .HasMaxLength(30)
                .HasDefaultValue("Pending");

            entity.HasOne(d => d.Package).WithMany(p => p.Payments)
                .HasForeignKey(d => d.PackageId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Payments__Packag__3E1D39E1");

            entity.HasOne(d => d.User).WithMany(p => p.Payments)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Payments__UserId__3D2915A8");
        });

        modelBuilder.Entity<Promotion>(entity =>
        {
            entity.HasKey(e => e.PromotionId).HasName("PK__Promotio__52C42FCF3B224CF5");

            entity.Property(e => e.PromotionId).HasDefaultValueSql("(newid())");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Title).HasMaxLength(150);
        });

        modelBuilder.Entity<Review>(entity =>
        {
            entity.HasKey(e => e.ReviewId).HasName("PK__Reviews__74BC79CE5A81B34E");

            entity.Property(e => e.ReviewId).HasDefaultValueSql("(newid())");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.Class).WithMany(p => p.Reviews)
                .HasForeignKey(d => d.ClassId)
                .HasConstraintName("FK__Reviews__ClassId__44CA3770");

            entity.HasOne(d => d.Gym).WithMany(p => p.Reviews)
                .HasForeignKey(d => d.GymId)
                .HasConstraintName("FK__Reviews__GymId__43D61337");

            entity.HasOne(d => d.User).WithMany(p => p.Reviews)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Reviews__UserId__42E1EEFE");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.RoleId).HasName("PK__Roles__8AFACE1AD46A3A12");

            entity.HasIndex(e => e.RoleName, "UQ__Roles__8A2B6160ACCD1031").IsUnique();

            entity.Property(e => e.RoleId).HasDefaultValueSql("(newid())");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Description).HasMaxLength(255);
            entity.Property(e => e.RoleName).HasMaxLength(50);
        });

        modelBuilder.Entity<SystemLog>(entity =>
        {
            entity.HasKey(e => e.LogId).HasName("PK__SystemLo__5E548648767A2855");

            entity.Property(e => e.LogId).HasDefaultValueSql("(newid())");
            entity.Property(e => e.Action).HasMaxLength(255);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.IpAddress).HasMaxLength(50);

            entity.HasOne(d => d.User).WithMany(p => p.SystemLogs)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__SystemLog__UserI__625A9A57");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__Users__1788CC4C75B61DB2");

            entity.HasIndex(e => e.Email, "UQ__Users__A9D1053421A327A4").IsUnique();

            entity.Property(e => e.UserId).HasDefaultValueSql("(newid())");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.FullName).HasMaxLength(100);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.PhoneNumber).HasMaxLength(20);
        });

        modelBuilder.Entity<UserCredit>(entity =>
        {
            entity.HasKey(e => e.UserCreditId).HasName("PK__UserCred__6D8AC3E925FF6ADC");

            entity.HasIndex(e => e.UserId, "UQ__UserCred__1788CC4DBC67384A").IsUnique();

            entity.Property(e => e.UserCreditId).HasDefaultValueSql("(newid())");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.User).WithOne(p => p.UserCredit)
                .HasForeignKey<UserCredit>(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__UserCredi__UserI__32AB8735");
        });

        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.HasKey(e => new { e.UserId, e.RoleId });

            entity.Property(e => e.AssignedAt).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.Role).WithMany(p => p.UserRoles)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UserRoles_Roles");

            entity.HasOne(d => d.User).WithMany(p => p.UserRoles)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UserRoles_Users");
        });

        modelBuilder.Entity<UserWorkoutHistory>(entity =>
        {
            entity.HasKey(e => e.WorkoutHistoryId).HasName("PK__UserWork__8D7C9B3DD9D92244");

            entity.Property(e => e.WorkoutHistoryId).HasDefaultValueSql("(newid())");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.ClassBooking).WithMany(p => p.UserWorkoutHistories)
                .HasForeignKey(d => d.ClassBookingId)
                .HasConstraintName("FK__UserWorko__Class__5CA1C101");

            entity.HasOne(d => d.GymBooking).WithMany(p => p.UserWorkoutHistories)
                .HasForeignKey(d => d.GymBookingId)
                .HasConstraintName("FK__UserWorko__GymBo__5BAD9CC8");

            entity.HasOne(d => d.User).WithMany(p => p.UserWorkoutHistories)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__UserWorko__UserI__5AB9788F");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
