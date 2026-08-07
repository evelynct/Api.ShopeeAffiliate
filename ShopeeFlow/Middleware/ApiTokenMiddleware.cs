using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using ShopeeFlow.Configurations;
using ShopeeFlow.DTOs.Common;

namespace ShopeeFlow.Middleware;

public class ApiTokenMiddleware
{
    private readonly RequestDelegate _next;

    public ApiTokenMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IOptions<ApiSecuritySettings> apiSecurityOptions)
    {
        if (IsAnonymousPath(context.Request.Path))
        {
            await _next(context);
            return;
        }

        var configuredToken = apiSecurityOptions.Value.AccessToken;
        if (string.IsNullOrWhiteSpace(configuredToken))
        {
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsJsonAsync(new BaseResponse
            {
                ErrorDescription = "API access token is not configured."
            });
            return;
        }

        var providedToken = context.Request.Headers[ApiSecuritySettings.HeaderName].ToString();
        if (!TokensMatch(providedToken, configuredToken))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new BaseResponse
            {
                ErrorDescription = "Invalid or missing API token."
            });
            return;
        }

        await _next(context);
    }

    private static bool IsAnonymousPath(PathString path)
    {
        return path.StartsWithSegments("/swagger");
    }

    private static bool TokensMatch(string providedToken, string configuredToken)
    {
        if (string.IsNullOrWhiteSpace(providedToken))
            return false;

        var providedBytes = Encoding.UTF8.GetBytes(providedToken);
        var configuredBytes = Encoding.UTF8.GetBytes(configuredToken);

        if (providedBytes.Length != configuredBytes.Length)
            return false;

        return CryptographicOperations.FixedTimeEquals(providedBytes, configuredBytes);
    }
}
