namespace SecBank.CardRequests.Api.Services;

public sealed class IdempotencyKeyReuseException : Exception
{
    public IdempotencyKeyReuseException() : base("The Idempotency-Key was already used with a different request payload.") { }
}

public sealed class ActiveCardRequestExistsException : Exception
{
    public ActiveCardRequestExistsException() : base("An active request already exists for this account and card type.") { }
}
