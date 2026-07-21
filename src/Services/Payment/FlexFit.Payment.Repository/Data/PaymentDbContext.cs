using FlexFit.Payment.Repository.Entities;
using Microsoft.EntityFrameworkCore;

namespace FlexFit.Payment.Repository.Data
{
    public class PaymentDbContext : DbContext
    {
        public PaymentDbContext(DbContextOptions<PaymentDbContext> options) : base(options)
        {
        }

        public DbSet<Entities.Payment> Payments { get; set; } = null!;
        public DbSet<CreditPackage> CreditPackages { get; set; } = null!;
        public DbSet<UserCredit> UserCredits { get; set; } = null!;
        public DbSet<CreditTransaction> CreditTransactions { get; set; } = null!;
        public DbSet<OutboxMessage> OutboxMessages { get; set; } = null!;
        public DbSet<ProcessedMessage> ProcessedMessages { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<CreditPackage>(entity =>
            {
                entity.HasKey(e => e.PackageId);
                entity.Property(e => e.Price).HasColumnType("decimal(18, 2)");
                entity.Property(e => e.PackageName).HasMaxLength(100);
            });

            modelBuilder.Entity<Entities.Payment>(entity =>
            {
                entity.HasKey(e => e.PaymentId);
                entity.Property(e => e.Amount).HasColumnType("decimal(18, 2)");
                entity.Property(e => e.PaymentMethod).HasMaxLength(50);
                entity.Property(e => e.ProviderTransactionCode).HasMaxLength(100);
                entity.Property(e => e.Status).HasMaxLength(30);

                entity.HasOne(d => d.Package)
                    .WithMany(p => p.Payments)
                    .HasForeignKey(d => d.PackageId)
                    .OnDelete(DeleteBehavior.ClientSetNull);

                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => new { e.UserId, e.CreatedAt });
            });

            modelBuilder.Entity<UserCredit>(entity =>
            {
                entity.HasKey(e => e.UserCreditId);
                entity.HasIndex(e => e.UserId).IsUnique();
            });

            modelBuilder.Entity<CreditTransaction>(entity =>
            {
                entity.HasKey(e => e.TransactionId);
                entity.Property(e => e.Type).HasMaxLength(30);
                entity.Property(e => e.ReferenceType).HasMaxLength(30);
                entity.HasIndex(e => new { e.UserId, e.CreatedAt });
            });

            modelBuilder.Entity<OutboxMessage>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.EventType).HasMaxLength(100);
                entity.HasIndex(e => e.ProcessedAt);
            });

            modelBuilder.Entity<ProcessedMessage>(entity =>
            {
                entity.HasKey(e => e.MessageId);
            });
        }
    }
}
