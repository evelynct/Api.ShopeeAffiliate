namespace ShopeeFlow.Configurations;

public class GreenApiSettings
{
    public const string SectionName = "GreenApi";

    public bool Enabled { get; set; }

    public string ApiUrl { get; set; } = "https://api.green-api.com";

    public string IdInstance { get; set; } = string.Empty;

    public string ApiTokenInstance { get; set; } = string.Empty;

    public string GroupChatId { get; set; } = string.Empty;

    public bool IsConfigured =>
        Enabled
        && !string.IsNullOrWhiteSpace(ApiUrl)
        && !string.IsNullOrWhiteSpace(IdInstance)
        && !string.IsNullOrWhiteSpace(ApiTokenInstance)
        && !string.IsNullOrWhiteSpace(GroupChatId);
}
