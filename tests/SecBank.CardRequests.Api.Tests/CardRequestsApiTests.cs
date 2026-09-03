using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.Testing;
using SecBank.CardRequests.Api.Contracts;
using SecBank.CardRequests.Api.Domain;
using Xunit;

namespace SecBank.CardRequests.Api.Tests;

public class CardRequestsApiTests(TestWebApplicationFactory factory) : IClassFixture<TestWebApplicationFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    [Fact]
    public async Task Create_then_get_returns_the_same_pending_request()
    {
        var client = CreateAuthorizedClient();
        var create = new CreateCardRequest
        {
            AccountNumber = "1111111111",
            CustomerName = "Chinedu Eze",
            CardType = CardType.Debit
        };

        var createResponse = await client.PostAsJsonAsync("/api/v1/card-requests", create, JsonOptions);

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<CardRequestResponse>(JsonOptions);
        Assert.NotNull(created);
        Assert.Equal(CardRequestStatus.Pending, created.Status);
        Assert.Equal("1111111111", created.AccountNumber);

        var statusResponse = await client.GetAsync($"/api/v1/card-requests/{created.RequestId}");
        Assert.Equal(HttpStatusCode.OK, statusResponse.StatusCode);
        var status = await statusResponse.Content.ReadFromJsonAsync<CardRequestResponse>(JsonOptions);
        Assert.Equal(created.RequestId, status!.RequestId);
    }

    [Fact]
    public async Task Create_without_api_key_returns_unauthorized()
    {
        var response = await factory.CreateClient().PostAsJsonAsync("/api/v1/card-requests", new
        {
            accountNumber = "0123456789",
            customerName = "Chinedu Eze",
            cardType = "Debit"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Create_with_invalid_account_number_returns_bad_request()
    {
        var client = CreateAuthorizedClient();
        var response = await client.PostAsJsonAsync("/api/v1/card-requests", new
        {
            accountNumber = "123",
            customerName = "Chinedu Eze",
            cardType = "Debit"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_without_idempotency_key_returns_bad_request()
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", TestWebApplicationFactory.ApiKey);

        var response = await client.PostAsJsonAsync("/api/v1/card-requests", new
        {
            accountNumber = "2222222222",
            customerName = "Chinedu Eze",
            cardType = "Debit"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_with_whitespace_only_customer_name_returns_bad_request()
    {
        var client = CreateAuthorizedClient();
        var response = await client.PostAsJsonAsync("/api/v1/card-requests", new
        {
            accountNumber = "0123456789",
            customerName = "  ",
            cardType = "Debit"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_with_numeric_card_type_returns_bad_request()
    {
        var client = CreateAuthorizedClient();
        var response = await client.PostAsJsonAsync("/api/v1/card-requests", new
        {
            accountNumber = "0123456789",
            customerName = "Chinedu Eze",
            cardType = 1
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Get_for_unknown_request_returns_not_found()
    {
        var client = CreateAuthorizedClient();

        var response = await client.GetAsync($"/api/v1/card-requests/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Create_replay_with_the_same_idempotency_key_returns_the_original_request()
    {
        var idempotencyKey = Guid.NewGuid().ToString();
        var client = CreateAuthorizedClient(idempotencyKey);
        var request = new
        {
            accountNumber = "3333333333",
            customerName = "Chinedu Eze",
            cardType = "Debit"
        };

        var first = await client.PostAsJsonAsync("/api/v1/card-requests", request);
        var replay = await client.PostAsJsonAsync("/api/v1/card-requests", request);

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, replay.StatusCode);
        Assert.Equal("true", replay.Headers.GetValues("Idempotent-Replayed").Single());
        var firstBody = await first.Content.ReadFromJsonAsync<CardRequestResponse>(JsonOptions);
        var replayBody = await replay.Content.ReadFromJsonAsync<CardRequestResponse>(JsonOptions);
        Assert.Equal(firstBody!.RequestId, replayBody!.RequestId);
    }

    [Fact]
    public async Task Create_reusing_an_idempotency_key_with_a_different_payload_returns_conflict()
    {
        var client = CreateAuthorizedClient(Guid.NewGuid().ToString());
        var first = await client.PostAsJsonAsync("/api/v1/card-requests", new
        {
            accountNumber = "4444444444",
            customerName = "Chinedu Eze",
            cardType = "Debit"
        });
        var second = await client.PostAsJsonAsync("/api/v1/card-requests", new
        {
            accountNumber = "5555555555",
            customerName = "Chinedu Eze",
            cardType = "Debit"
        });

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Create_for_an_account_with_an_active_request_for_the_same_card_type_returns_conflict()
    {
        var request = new
        {
            accountNumber = "6666666666",
            customerName = "Chinedu Eze",
            cardType = "Debit"
        };

        var first = await CreateAuthorizedClient(Guid.NewGuid().ToString())
            .PostAsJsonAsync("/api/v1/card-requests", request);
        var second = await CreateAuthorizedClient(Guid.NewGuid().ToString())
            .PostAsJsonAsync("/api/v1/card-requests", request);

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    private HttpClient CreateAuthorizedClient(string? idempotencyKey = null)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", TestWebApplicationFactory.ApiKey);
        client.DefaultRequestHeaders.Add("Idempotency-Key", idempotencyKey ?? Guid.NewGuid().ToString());
        return client;
    }
}
