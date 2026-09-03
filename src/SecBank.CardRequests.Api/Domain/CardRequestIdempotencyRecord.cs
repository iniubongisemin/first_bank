namespace SecBank.CardRequests.Api.Domain;

public class CardRequestIdempotencyRecord
{
    public string IdempotencyKey { get; set; } = string.Empty;
    public string RequestFingerprint { get; set; } = string.Empty;
    public Guid CardRequestId { get; set; }
    public CardRequest CardRequest { get; set; } = null!;
    public DateTimeOffset CreatedAtUtc { get; set; }
}
