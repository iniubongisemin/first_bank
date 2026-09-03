using Microsoft.EntityFrameworkCore;

namespace SecBank.CardRequests.Api.Data;

public static class DatabaseSchema
{
    public static async Task EnsureIdempotencySupportAsync(CardRequestsDbContext db, CancellationToken cancellationToken = default)
    {
        // EnsureCreated does not add newly mapped tables to an existing local SQLite database.
        // These idempotent SQLite statements preserve databases created by earlier demo versions.
        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS "CardRequestIdempotencyRecords" (
                "IdempotencyKey" TEXT NOT NULL CONSTRAINT "PK_CardRequestIdempotencyRecords" PRIMARY KEY,
                "RequestFingerprint" TEXT NOT NULL,
                "CardRequestId" TEXT NOT NULL,
                "CreatedAtUtc" TEXT NOT NULL,
                CONSTRAINT "FK_CardRequestIdempotencyRecords_CardRequests_CardRequestId"
                    FOREIGN KEY ("CardRequestId") REFERENCES "CardRequests" ("Id") ON DELETE CASCADE
            );
            """, cancellationToken);
        await db.Database.ExecuteSqlRawAsync("""
            CREATE INDEX IF NOT EXISTS "IX_CardRequestIdempotencyRecords_CardRequestId"
            ON "CardRequestIdempotencyRecords" ("CardRequestId");
            """, cancellationToken);
        await db.Database.ExecuteSqlRawAsync("""
            WITH "RankedActiveRequests" AS (
                SELECT "Id", ROW_NUMBER() OVER (
                    PARTITION BY "AccountNumber", "CardType"
                    ORDER BY "CreatedAtUtc", "Id") AS "Rank"
                FROM "CardRequests"
                WHERE "Status" IN ('Pending', 'Processing')
            )
            UPDATE "CardRequests"
            SET "Status" = 'Superseded',
                "UpdatedAtUtc" = strftime('%Y-%m-%dT%H:%M:%f+00:00', 'now')
            WHERE "Id" IN (
                SELECT "Id" FROM "RankedActiveRequests" WHERE "Rank" > 1
            );
            """, cancellationToken);
        await db.Database.ExecuteSqlRawAsync("""
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_CardRequests_AccountNumber_CardType_Active"
            ON "CardRequests" ("AccountNumber", "CardType")
            WHERE "Status" IN ('Pending', 'Processing');
            """, cancellationToken);
    }
}
