using FlexFit.BookingService.Models;
using Microsoft.EntityFrameworkCore;

namespace FlexFit.BookingService.Data
{
    public class BookingDbContext : DbContext
    {
        public BookingDbContext(DbContextOptions<BookingDbContext> options)
            : base(options)
        {
        }

        public virtual DbSet<GymBooking> GymBookings { get; set; } = null!;
        public virtual DbSet<ClassBooking> ClassBookings { get; set; } = null!;
        public virtual DbSet<CheckInLog> CheckInLogs { get; set; } = null!;
        public virtual DbSet<OutboxMessage> OutboxMessages { get; set; } = null!;
        public virtual DbSet<InboxMessage> InboxMessages { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<GymBooking>(entity =>
            {
                entity.HasKey(e => e.BookingId);
                entity.ToTable("GymBookings");

                entity.Property(e => e.BookingId).HasDefaultValueSql("(newid())");
                entity.Property(e => e.BookingCode).HasMaxLength(50);
                entity.HasIndex(e => e.BookingCode).IsUnique();

                entity.Property(e => e.CheckInStatus).HasMaxLength(30).HasDefaultValue("NotCheckedIn");
                entity.Property(e => e.Status).HasMaxLength(30).HasDefaultValue("Booked");
                entity.Property(e => e.QrToken).HasMaxLength(255);
                entity.Property(e => e.BookedAt).HasDefaultValueSql("(getutcdate())");
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(getutcdate())");

                entity.Property(e => e.BranchId).IsRequired();
                entity.Property(e => e.GymId).IsRequired();

                entity.Property(e => e.GymNameSnapshot).HasMaxLength(150);
                entity.Property(e => e.SessionNameSnapshot).HasMaxLength(150);
                entity.Property(e => e.BranchNameSnapshot).HasMaxLength(150);
                entity.Property(e => e.BranchAddressSnapshot).HasMaxLength(255);

                entity.Property(e => e.RowVersion).IsRowVersion();
            });

            modelBuilder.Entity<ClassBooking>(entity =>
            {
                entity.HasKey(e => e.BookingId);
                entity.ToTable("ClassBookings");

                entity.Property(e => e.BookingId).HasDefaultValueSql("(newid())");
                entity.Property(e => e.BookingCode).HasMaxLength(50);
                entity.HasIndex(e => e.BookingCode).IsUnique();

                entity.Property(e => e.CheckInStatus).HasMaxLength(30).HasDefaultValue("NotCheckedIn");
                entity.Property(e => e.Status).HasMaxLength(30).HasDefaultValue("Booked");
                entity.Property(e => e.QrToken).HasMaxLength(255);
                entity.Property(e => e.BookedAt).HasDefaultValueSql("(getutcdate())");
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(getutcdate())");

                entity.Property(e => e.BranchId).IsRequired();
                entity.Property(e => e.GymId).IsRequired();

                entity.Property(e => e.GymNameSnapshot).HasMaxLength(150);
                entity.Property(e => e.ClassNameSnapshot).HasMaxLength(150);
                entity.Property(e => e.BranchNameSnapshot).HasMaxLength(150);
                entity.Property(e => e.BranchAddressSnapshot).HasMaxLength(255);
                entity.Property(e => e.CoachNameSnapshot).HasMaxLength(100);

                entity.Property(e => e.RowVersion).IsRowVersion();
            });

            modelBuilder.Entity<CheckInLog>(entity =>
            {
                entity.HasKey(e => e.CheckInLogId);
                entity.ToTable("CheckInLogs");

                entity.Property(e => e.CheckInLogId).HasDefaultValueSql("(newid())");
                entity.Property(e => e.Message).HasMaxLength(255);
                entity.Property(e => e.ScannedAt).HasDefaultValueSql("(getutcdate())");
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
                entity.Property(e => e.Status).HasMaxLength(30);

                entity.HasOne(d => d.ClassBooking)
                    .WithMany(p => p.CheckInLogs)
                    .HasForeignKey(d => d.ClassBookingId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(d => d.GymBooking)
                    .WithMany(p => p.CheckInLogs)
                    .HasForeignKey(d => d.GymBookingId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<OutboxMessage>(entity =>
            {
                entity.HasKey(e => e.OutboxMessageId);
                entity.ToTable("OutboxMessages");
                entity.Property(e => e.OutboxMessageId).HasDefaultValueSql("(newid())");
                entity.Property(e => e.EventType).HasMaxLength(150).IsRequired();
                entity.Property(e => e.AggregateType).HasMaxLength(150).IsRequired();
                entity.Property(e => e.AggregateId).IsRequired();
                entity.Property(e => e.Payload).IsRequired();
                entity.Property(e => e.CorrelationId).HasMaxLength(150);
                entity.Property(e => e.OccurredAt).IsRequired();
                entity.HasIndex(e => e.ProcessedAt);
            });

            modelBuilder.Entity<InboxMessage>(entity =>
            {
                entity.HasKey(e => e.EventId);
                entity.ToTable("InboxMessages");
                entity.Property(e => e.EventType).HasMaxLength(150).IsRequired();
                entity.Property(e => e.ConsumerName).HasMaxLength(255).IsRequired();
                entity.Property(e => e.ReceivedAt).IsRequired();
            });
        }
    }
}
