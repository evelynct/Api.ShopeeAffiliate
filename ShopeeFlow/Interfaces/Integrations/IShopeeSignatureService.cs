namespace ShopeeFlow.Interfaces.Integrations;

public interface IShopeeSignatureService
{
    string BuildAuthorizationHeader(string payload, long timestampUnixSeconds);
}
