using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using System.Text;

namespace SecBank.CardRequests.Api.Middleware;

public class ApiKeyMiddleware(RequestDelegate next, IConfiguration configuration)
{
    private const string HeaderName = "X-Api-Key";

    public async Task InvokeAsync(HttpContext context)
    {
        // Swagger is deliberately public in development so an assessor can inspect the contract.
        if (context.Request.Path.StartsWithSegments("/swagger") &&
            context.RequestServices.GetRequiredService<IHostEnvironment>().IsDevelopment())
        {
            await next(context);
            return;
        }

        var expectedApiKey = configuration["ApiSecurity:ApiKey"];
        if (string.IsNullOrWhiteSpace(expectedApiKey) ||
            !context.Request.Headers.TryGetValue(HeaderName, out var suppliedApiKey) ||
            suppliedApiKey.Count != 1 ||
            !KeysMatch(suppliedApiKey[0], expectedApiKey))
        {
            await Results.Problem(
                title: "Unauthorized",
                detail: "Supply a valid X-Api-Key header.",
                statusCode: StatusCodes.Status401Unauthorized)
                .ExecuteAsync(context);
            return;
        }

        await next(context);
    }

    private static bool KeysMatch(string? suppliedApiKey, string expectedApiKey)
    {
        if (suppliedApiKey is null)
        {
            return false;
        }

        var suppliedHash = SHA256.HashData(Encoding.UTF8.GetBytes(suppliedApiKey));
        var expectedHash = SHA256.HashData(Encoding.UTF8.GetBytes(expectedApiKey));
        return CryptographicOperations.FixedTimeEquals(suppliedHash, expectedHash);
    }
}
