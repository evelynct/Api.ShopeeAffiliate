namespace ShopeeFlow.Configurations;

public class AiSettings
{
    public const string SectionName = "Ai";
    public const string DefaultModel = "gemini-3.5-flash";

    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = DefaultModel;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(ApiKey);

    public string GetModelOrDefault()
    {
        return string.IsNullOrWhiteSpace(Model) ? DefaultModel : Model;
    }
}
