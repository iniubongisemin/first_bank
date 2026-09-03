# SecBank Card Request API Case Study

My ASP.NET Core 8 REST API for a SecBank partner to create a customer card request and check its status.

## Delivered API

| Method | Endpoint | Purpose |
|---|---|---|
| `POST` | `/api/v1/card-requests` | Create a request; its initial status is `Pending`. Requires `Idempotency-Key`. |
| `GET` | `/api/v1/card-requests/{requestId}` | Retrieve the request and its current status. |

Every API call requires `X-Api-Key`. POST requests also require a unique `Idempotency-Key`, retained by the client for safe retries.

## Run 

1. Install the .NET 8 SDK from https://dotnet.microsoft.com/download/dotnet/8.0.
2. From this repository run:

   ```bash
   dotnet user-secrets set --project src/SecBank.CardRequests.Api "ApiSecurity:ApiKey" "a-local-development-secret"
   dotnet restore
   dotnet run --project src/SecBank.CardRequests.Api
   ```

   Or, copy `appsettings.Development.example.json` to `appsettings.Development.json` and replace the placeholder key. The Development file is deliberately ignored by Git.

The database file is created automatically on first start. A representative request is seeded with ID `b7d4acdc-98ff-4eb4-85e5-d3296bf0efc3`.

## Run tests

```bash
dotnet test
```
