using SecBank.CardRequests.Api.Contracts;

namespace SecBank.CardRequests.Api.Services;

public interface ICardRequestService
{
    Task<CreateCardRequestResult> CreateAsync(CreateCardRequest request, string idempotencyKey, CancellationToken cancellationToken);
    Task<CardRequestResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
}
