namespace ShopeeFlow.Configurations;

public class ShopeeAffiliateSettings
{
    public const string SectionName = "ShopeeAffiliate";
    public const int DefaultTimeoutSeconds = 30;

    public string BaseUrl { get; set; } = string.Empty;
    public string AppId { get; set; } = string.Empty;
    public string Secret { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = DefaultTimeoutSeconds;

    public int GetTimeoutSecondsOrDefault()
    {
        return TimeoutSeconds > 0 ? TimeoutSeconds : DefaultTimeoutSeconds;
    }

    public bool HasRequiredValues()
    {
        return !string.IsNullOrWhiteSpace(BaseUrl)
               && !string.IsNullOrWhiteSpace(AppId)
               && !string.IsNullOrWhiteSpace(Secret);
    }
}
