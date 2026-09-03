namespace SecBank.CardRequests.Api.Domain;

public enum CardRequestStatus
{
    Pending = 1,
    Processing = 2,
    Approved = 3,
    Rejected = 4,
    Superseded = 5
}
