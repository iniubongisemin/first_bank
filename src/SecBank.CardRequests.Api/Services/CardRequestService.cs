using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using SecBank.CardRequests.Api.Contracts;
using SecBank.CardRequests.Api.Data;
using SecBank.CardRequests.Api.Domain;

namespace SecBank.CardRequests.Api.Services;

public class CardRequestService(CardRequestsDbContext db) : ICardRequestService
{
    public async Task<CreateCardRequestResult> CreateAsync(CreateCardRequest request, string idempotencyKey, CancellationToken cancellationToken)
    {
        var accountNumber = request.AccountNumber.Trim();
        var customerName = request.CustomerName.Trim();
        var cardType = request.CardType!.Value;
        var fingerprint = CreateFingerprint(accountNumber, customerName, cardType);

        var existing = await db.IdempotencyRecords
            .Include(x => x.CardRequest)
            .SingleOrDefaultAsync(x => x.IdempotencyKey == idempotencyKey, cancellationToken);
        if (existing is not null)
        {
            return existing.RequestFingerprint == fingerprint
                ? new CreateCardRequestResult(ToResponse(existing.CardRequest), true)
                : throw new IdempotencyKeyReuseException();
        }

        var activeRequestExists = await db.CardRequests.AsNoTracking().AnyAsync(
            x => x.AccountNumber == accountNumber &&
                 x.CardType == cardType &&
                 (x.Status == CardRequestStatus.Pending || x.Status == CardRequestStatus.Processing),
            cancellationToken);
        if (activeRequestExists)
        {
            throw new ActiveCardRequestExistsException();
        }

        var now = DateTimeOffset.UtcNow;
        var entity = new CardRequest
        {
            Id = Guid.NewGuid(),
            AccountNumber = accountNumber,
            CustomerName = customerName,
            CardType = cardType,
            Status = CardRequestStatus.Pending,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        db.CardRequests.Add(entity);
        db.IdempotencyRecords.Add(new CardRequestIdempotencyRecord
        {
            IdempotencyKey = idempotencyKey,
            RequestFingerprint = fingerprint,
            CardRequestId = entity.Id,
            CreatedAtUtc = now
        });

        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return new CreateCardRequestResult(ToResponse(entity), false);
        }
        catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
        {
            db.ChangeTracker.Clear();
            var persisted = await db.IdempotencyRecords
                .Include(x => x.CardRequest)
                .SingleOrDefaultAsync(x => x.IdempotencyKey == idempotencyKey, cancellationToken);

            if (persisted is not null)
            {
                return persisted.RequestFingerprint == fingerprint
                    ? new CreateCardRequestResult(ToResponse(persisted.CardRequest), true)
                    : throw new IdempotencyKeyReuseException();
            }

            throw new ActiveCardRequestExistsException();
        }
    }

    public async Task<CardRequestResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await db.CardRequests.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        return entity is null ? null : ToResponse(entity);
    }

    private static CardRequestResponse ToResponse(CardRequest entity) => new(
        entity.Id, entity.AccountNumber, entity.CustomerName, entity.CardType,
        entity.Status, entity.CreatedAtUtc, entity.UpdatedAtUtc);

    private static string CreateFingerprint(string accountNumber, string customerName, CardType cardType)
    {
        var payload = JsonSerializer.Serialize(new { accountNumber, customerName, cardType });
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload)));
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException exception) =>
        exception.InnerException is SqliteException { SqliteErrorCode: 19 };
}
