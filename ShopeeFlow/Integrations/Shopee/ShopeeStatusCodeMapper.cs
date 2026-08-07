using System.Net;

namespace ShopeeFlow.Integrations.Shopee;

public static class ShopeeStatusCodeMapper
{
    public static HttpStatusCode ToHttpStatusCode(int shopeeErrorCode)
    {
        return shopeeErrorCode switch
        {
            ShopeeErrorCodes.RequestParsingError => HttpStatusCode.BadRequest,
            ShopeeErrorCodes.AuthenticationError => HttpStatusCode.Unauthorized,
            ShopeeErrorCodes.RateLimitExceeded => HttpStatusCode.TooManyRequests,
            ShopeeErrorCodes.BusinessProcessingError => HttpStatusCode.UnprocessableEntity,
            ShopeeErrorCodes.SystemError => HttpStatusCode.InternalServerError,
            _ => HttpStatusCode.InternalServerError
        };
    }
}
