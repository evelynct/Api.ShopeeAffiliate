namespace ShopeeFlow.Integrations.Shopee;

/// <summary>
/// Official Shopee Affiliate Open API error codes (GraphQL extensions.code).
/// </summary>
public static class ShopeeErrorCodes
{
    public const int SystemError = 10000;
    public const int RequestParsingError = 10010;
    public const int AuthenticationError = 10020;
    public const int RateLimitExceeded = 10030;
    public const int BusinessProcessingError = 11000;
}
