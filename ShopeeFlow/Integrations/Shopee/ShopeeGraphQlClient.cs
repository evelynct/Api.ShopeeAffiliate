using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using ShopeeFlow.Configurations;
using ShopeeFlow.DTOs.Common;
using ShopeeFlow.Integrations.Shopee.Contracts;
using ShopeeFlow.Interfaces.Integrations;

namespace ShopeeFlow.Integrations.Shopee;

public class ShopeeGraphQlClient : IShopeeGraphQlClient
{
    private const int MaxLoggedBodyLength = 500;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;
    private readonly IShopeeSignatureService _signatureService;
    private readonly ShopeeAffiliateSettings _settings;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<ShopeeGraphQlClient> _logger;

    public ShopeeGraphQlClient(
        HttpClient httpClient,
        IShopeeSignatureService signatureService,
        IOptions<ShopeeAffiliateSettings> settings,
        TimeProvider timeProvider,
        ILogger<ShopeeGraphQlClient> logger)
    {
        _httpClient = httpClient;
        _signatureService = signatureService;
        _settings = settings.Value;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<Result<TData>> ExecuteAsync<TData>(
        string graphQlQuery,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(graphQlQuery))
            return Result<TData>.Fail("GraphQL query is required.", HttpStatusCode.BadRequest);

        var configurationError = ValidateConfiguration();
        if (configurationError is not null)
            return Result<TData>.Fail(configurationError, HttpStatusCode.InternalServerError);

        var payload = JsonSerializer.Serialize(new { query = graphQlQuery });
        var timestamp = _timeProvider.GetUtcNow().ToUnixTimeSeconds();
        var authorization = _signatureService.BuildAuthorizationHeader(payload, timestamp);

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, _settings.BaseUrl);
        httpRequest.Headers.TryAddWithoutValidation("Authorization", authorization);
        httpRequest.Content = new StringContent(payload, Encoding.UTF8, "application/json");

        try
        {
            using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "Shopee GraphQL HTTP failure. StatusCode={StatusCode}, Body={Body}",
                    (int)response.StatusCode,
                    TruncateForLog(responseBody));

                return Result<TData>.Fail(
                    "Shopee API request failed.",
                    response.StatusCode);
            }

            var graphQlResponse = JsonSerializer.Deserialize<GraphQlResponse<TData>>(
                responseBody,
                SerializerOptions);

            if (graphQlResponse is null)
                return Result<TData>.Fail("Shopee returned an invalid JSON payload.", HttpStatusCode.BadGateway);

            if (graphQlResponse.Errors is { Count: > 0 })
                return BuildProviderError<TData>(graphQlResponse.Errors[0]);

            if (graphQlResponse.Data is null)
                return Result<TData>.Fail("Shopee returned an empty data payload.", HttpStatusCode.BadGateway);

            return Result<TData>.Ok(graphQlResponse.Data);
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError("Shopee GraphQL request timed out.");
            return Result<TData>.Fail("Shopee API request timed out.", HttpStatusCode.GatewayTimeout);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error calling Shopee GraphQL.");
            return Result<TData>.Fail("Unexpected error calling Shopee API.", HttpStatusCode.InternalServerError);
        }
    }

    private string? ValidateConfiguration()
    {
        if (string.IsNullOrWhiteSpace(_settings.AppId) || string.IsNullOrWhiteSpace(_settings.Secret))
            return "Shopee credentials are not configured.";

        if (string.IsNullOrWhiteSpace(_settings.BaseUrl))
            return "Shopee BaseUrl is not configured.";

        return null;
    }

    private Result<TData> BuildProviderError<TData>(GraphQlError firstError)
    {
        var providerErrorCode = firstError.Extensions?.Code ?? ShopeeErrorCodes.SystemError;
        var message = firstError.Extensions?.Message
                      ?? firstError.Message
                      ?? "Unknown Shopee GraphQL error.";

        _logger.LogWarning(
            "Shopee GraphQL business error. ProviderErrorCode={ProviderErrorCode}, Message={Message}",
            providerErrorCode,
            message);

        return Result<TData>.Fail(
            message,
            ShopeeStatusCodeMapper.ToHttpStatusCode(providerErrorCode),
            providerErrorCode);
    }

    private static string TruncateForLog(string body)
    {
        if (string.IsNullOrEmpty(body) || body.Length <= MaxLoggedBodyLength)
            return body;

        return body[..MaxLoggedBodyLength] + "...(truncated)";
    }
}
