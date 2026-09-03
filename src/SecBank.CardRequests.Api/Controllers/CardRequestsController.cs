using Microsoft.AspNetCore.Mvc;
using SecBank.CardRequests.Api.Contracts;
using SecBank.CardRequests.Api.Services;

namespace SecBank.CardRequests.Api.Controllers;

[ApiController]
[Route("api/v1/card-requests")]
public class CardRequestsController(ICardRequestService cardRequestService) : ControllerBase
{
    /// <summary>Creates a new card request with a Pending status.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(CardRequestResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(CardRequestResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CardRequestResponse>> Create(
        [FromBody] CreateCardRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey) || idempotencyKey.Length > 128)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "A valid Idempotency-Key header is required.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        try
        {
            var result = await cardRequestService.CreateAsync(request, idempotencyKey.Trim(), cancellationToken);
            if (result.IsReplay)
            {
                Response.Headers.Append("Idempotent-Replayed", "true");
                return Ok(result.Response);
            }

            return CreatedAtAction(nameof(GetStatus), new { requestId = result.Response.RequestId }, result.Response);
        }
        catch (IdempotencyKeyReuseException exception)
        {
            return Conflict(new ProblemDetails { Title = exception.Message, Status = StatusCodes.Status409Conflict });
        }
        catch (ActiveCardRequestExistsException exception)
        {
            return Conflict(new ProblemDetails { Title = exception.Message, Status = StatusCodes.Status409Conflict });
        }
    }

    /// <summary>Returns the current status and details for a card request.</summary>
    [HttpGet("{requestId:guid}")]
    [ProducesResponseType(typeof(CardRequestResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CardRequestResponse>> GetStatus(Guid requestId, CancellationToken cancellationToken)
    {
        var response = await cardRequestService.GetByIdAsync(requestId, cancellationToken);
        return response is null
            ? NotFound(new ProblemDetails { Title = "Card request not found.", Status = StatusCodes.Status404NotFound })
            : Ok(response);
    }
}
