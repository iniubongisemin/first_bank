using SecBank.CardRequests.Api.Domain;

namespace SecBank.CardRequests.Api.Contracts;

public record CardRequestResponse(
    Guid RequestId,
    string AccountNumber,
    string CustomerName,
    CardType CardType,
    CardRequestStatus Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);
