using Microsoft.EntityFrameworkCore;
using Payments.Domain.Entities;

namespace Payments.Infrastructure.Persistence;

public class PaymentsDbContext(DbContextOptions<PaymentsDbContext> options) : DbContext(options)
{
    public DbSet<Batch> Batches => Set<Batch>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<PaymentAuditEvent> PaymentAuditEvents => Set<PaymentAuditEvent>();
    public DbSet<IdempotencyRecord> IdempotencyRecords => Set<IdempotencyRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Batch>(builder =>
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.ClientBatchReference).HasMaxLength(100).IsRequired();
            builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(50).IsRequired();
            builder.HasMany(x => x.Payments).WithOne().HasForeignKey(x => x.BatchId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Payment>(builder =>
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.ClientPaymentReference).HasMaxLength(100).IsRequired();
            builder.Property(x => x.Currency).HasMaxLength(3).IsRequired();
            builder.Property(x => x.Amount).HasPrecision(18, 2);
            builder.Property(x => x.BeneficiaryName).HasMaxLength(200).IsRequired();
            builder.Property(x => x.DestinationAccount).HasMaxLength(200).IsRequired();
            builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(50).IsRequired();
            builder.Property(x => x.FailureCode).HasMaxLength(50);
            builder.Property(x => x.FailureReason).HasMaxLength(500);
            builder.Property(x => x.Version).IsConcurrencyToken().ValueGeneratedOnAddOrUpdate().HasDefaultValueSql("randomblob(8)");
            builder.HasIndex(x => new { x.BatchId, x.ClientPaymentReference }).IsUnique();
            builder.HasIndex(x => new { x.Status, x.RetryAfterUtc });
        });

        modelBuilder.Entity<PaymentAuditEvent>(builder =>
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.OldStatus).HasMaxLength(50).IsRequired();
            builder.Property(x => x.NewStatus).HasMaxLength(50).IsRequired();
            builder.Property(x => x.Reason).HasMaxLength(500);
            builder.Property(x => x.CorrelationId).HasMaxLength(100).IsRequired();
            builder.HasIndex(x => x.PaymentId);
        });

        modelBuilder.Entity<IdempotencyRecord>(builder =>
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.RequestId).HasMaxLength(100).IsRequired();
            builder.Property(x => x.ResourceType).HasMaxLength(50).IsRequired();
            builder.HasIndex(x => x.RequestId).IsUnique();
        });
    }
}
