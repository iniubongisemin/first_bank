namespace SecBank.CardRequests.Api.Domain;

public class CardRequest
{
    public Guid Id { get; set; }
    public string AccountNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public CardType CardType { get; set; }
    public CardRequestStatus Status { get; set; } = CardRequestStatus.Pending;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}
