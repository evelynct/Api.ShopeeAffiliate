using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using ShopeeFlow.Configurations;
using ShopeeFlow.Integrations.Shopee;

namespace ShopeeFlow.UnitTests.Integrations;

public class ShopeeSignatureServiceTests
{
    #region Happy Path

    [Fact]
    public void BuildAuthorizationHeader_WhenValidInputs_ReturnsExpectedSignatureFromOfficialExample()
    {
        // Arrange
        // Official Shopee docs example:
        // AppId=123456, Secret=demo, Timestamp=1577836800
        var settings = Options.Create(new ShopeeAffiliateSettings
        {
            AppId = "123456",
            Secret = "demo",
            BaseUrl = "https://open-api.affiliate.shopee.com.br/graphql"
        });
        var service = new ShopeeSignatureService(settings);
        const string payload = "{\"query\":\"{\\nbrandOffer{\\n    nodes{\\n        commissionRate\\n        offerName\\n    }\\n}\\n}\"}";
        const long timestamp = 1577836800;
        const string expectedSignature = "dc88d72feea70c80c52c3399751a7d34966763f51a7f056aa070a5e9df645412";

        // Act
        var authorization = service.BuildAuthorizationHeader(payload, timestamp);

        // Assert
        Assert.Equal(
            $"SHA256 Credential=123456, Timestamp={timestamp}, Signature={expectedSignature}",
            authorization);
    }

    [Fact]
    public void BuildAuthorizationHeader_WhenCustomPayload_MatchesManualSha256Calculation()
    {
        // Arrange
        const string appId = "18333990384";
        const string secret = "unit-test-secret";
        const long timestamp = 1700000000;
        const string payload = "{\"query\":\"{ productOfferV2(limit: 1) { nodes { itemId } } }\"}";

        var settings = Options.Create(new ShopeeAffiliateSettings
        {
            AppId = appId,
            Secret = secret
        });
        var service = new ShopeeSignatureService(settings);
        var expectedSignature = ComputeSha256Hex($"{appId}{timestamp}{payload}{secret}");

        // Act
        var authorization = service.BuildAuthorizationHeader(payload, timestamp);

        // Assert
        Assert.Equal(
            $"SHA256 Credential={appId}, Timestamp={timestamp}, Signature={expectedSignature}",
            authorization);
    }

    #endregion

    #region Error / Edge Cases

    [Fact]
    public void BuildAuthorizationHeader_WhenOnlyPayloadChanges_ChangesOnlySignatureSegment()
    {
        // Arrange
        var settings = Options.Create(new ShopeeAffiliateSettings
        {
            AppId = "123456",
            Secret = "demo"
        });
        var service = new ShopeeSignatureService(settings);
        const long timestamp = 1577836800;

        // Act
        var first = service.BuildAuthorizationHeader("{\"query\":\"a\"}", timestamp);
        var second = service.BuildAuthorizationHeader("{\"query\":\"b\"}", timestamp);

        // Assert
        Assert.NotEqual(
            first.Split("Signature=")[1],
            second.Split("Signature=")[1]);
        Assert.StartsWith("SHA256 Credential=123456, Timestamp=1577836800, Signature=", first);
        Assert.StartsWith("SHA256 Credential=123456, Timestamp=1577836800, Signature=", second);
    }

    #endregion

    private static string ComputeSha256Hex(string value)
    {
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}
