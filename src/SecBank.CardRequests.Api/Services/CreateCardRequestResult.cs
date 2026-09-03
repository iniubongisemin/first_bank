using SecBank.CardRequests.Api.Contracts;

namespace SecBank.CardRequests.Api.Services;

public record CreateCardRequestResult(CardRequestResponse Response, bool IsReplay);
