namespace ShopeeFlow.Configurations;

public class ApiSecuritySettings
{
    public const string SectionName = "ApiSecurity";
    public const string HeaderName = "X-Api-Key";

    public string AccessToken { get; set; } = string.Empty;

    public bool HasRequiredValues() => !string.IsNullOrWhiteSpace(AccessToken);
}
