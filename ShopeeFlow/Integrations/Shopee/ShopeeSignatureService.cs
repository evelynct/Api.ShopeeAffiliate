using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using ShopeeFlow.Configurations;
using ShopeeFlow.Interfaces.Integrations;

namespace ShopeeFlow.Integrations.Shopee;

public class ShopeeSignatureService : IShopeeSignatureService
{
    private readonly ShopeeAffiliateSettings _settings;

    public ShopeeSignatureService(IOptions<ShopeeAffiliateSettings> settings)
    {
        _settings = settings.Value;
    }

    public string BuildAuthorizationHeader(string payload, long timestampUnixSeconds)
    {
        var factor = $"{_settings.AppId}{timestampUnixSeconds}{payload}{_settings.Secret}";
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(factor));
        var signature = Convert.ToHexString(hashBytes).ToLowerInvariant();

        return $"SHA256 Credential={_settings.AppId}, Timestamp={timestampUnixSeconds}, Signature={signature}";
    }
}
