using Microsoft.EntityFrameworkCore;
using SecBank.CardRequests.Api.Domain;

namespace SecBank.CardRequests.Api.Data;

public class CardRequestsDbContext(DbContextOptions<CardRequestsDbContext> options) : DbContext(options)
{
    public DbSet<CardRequest> CardRequests => Set<CardRequest>();
    public DbSet<CardRequestIdempotencyRecord> IdempotencyRecords => Set<CardRequestIdempotencyRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var request = modelBuilder.Entity<CardRequest>();
        request.ToTable("CardRequests");
        request.HasKey(x => x.Id);
        request.Property(x => x.AccountNumber).HasMaxLength(10).IsRequired();
        request.Property(x => x.CustomerName).HasMaxLength(120).IsRequired();
        request.Property(x => x.CardType).HasConversion<string>().HasMaxLength(20).IsRequired();
        request.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        request.Property(x => x.CreatedAtUtc).IsRequired();
        request.Property(x => x.UpdatedAtUtc).IsRequired();
        request.HasIndex(x => new { x.AccountNumber, x.CreatedAtUtc });
        request.HasIndex(x => new { x.AccountNumber, x.CardType })
            .HasDatabaseName("IX_CardRequests_AccountNumber_CardType_Active")
            .HasFilter("\"Status\" IN ('Pending', 'Processing')")
            .IsUnique();

        var idempotency = modelBuilder.Entity<CardRequestIdempotencyRecord>();
        idempotency.ToTable("CardRequestIdempotencyRecords");
        idempotency.HasKey(x => x.IdempotencyKey);
        idempotency.Property(x => x.IdempotencyKey).HasMaxLength(128).IsRequired();
        idempotency.Property(x => x.RequestFingerprint).HasMaxLength(64).IsRequired();
        idempotency.Property(x => x.CreatedAtUtc).IsRequired();
        idempotency.HasIndex(x => x.CardRequestId);
        idempotency.HasOne(x => x.CardRequest)
            .WithMany()
            .HasForeignKey(x => x.CardRequestId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
