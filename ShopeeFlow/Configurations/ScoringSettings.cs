namespace ShopeeFlow.Configurations;

public class ScoringSettings
{
    public const string SectionName = "Scoring";

    public int MinimumScore { get; set; } = 70;
    public decimal MinimumPrice { get; set; } = 50m;
    public decimal MinimumRating { get; set; } = 4.0m;
    public decimal MinimumCommissionRatePercent { get; set; } = 10m;
    public decimal MinimumCommissionValue { get; set; } = 10m;

    public List<int> AllowedCategories { get; set; } = [];
    public List<int> BlockedCategories { get; set; } = [];

    public bool HasRequiredValues() => AllowedCategories.Count > 0;
}
