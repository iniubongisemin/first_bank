using Microsoft.EntityFrameworkCore;
using SecBank.CardRequests.Api.Domain;

namespace SecBank.CardRequests.Api.Data;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(CardRequestsDbContext db)
    {
        if (await db.CardRequests.AnyAsync()) return;

        var created = new DateTimeOffset(2026, 1, 15, 9, 30, 0, TimeSpan.Zero);
        db.CardRequests.Add(new CardRequest
        {
            Id = Guid.Parse("b7d4acdc-98ff-4eb4-85e5-d3296bf0efc3"),
            AccountNumber = "0123456789",
            CustomerName = "Adaeze Okafor",
            CardType = CardType.Debit,
            Status = CardRequestStatus.Processing,
            CreatedAtUtc = created,
            UpdatedAtUtc = created.AddHours(2)
        });
        await db.SaveChangesAsync();
    }
}
