namespace ShopeeFlow.UnitTests.TestSupport;

public sealed class SettableTimeProvider : TimeProvider
{
    public SettableTimeProvider(DateTimeOffset utcNow)
    {
        UtcNow = utcNow;
    }

    public DateTimeOffset UtcNow { get; set; }

    public override DateTimeOffset GetUtcNow() => UtcNow;
}
