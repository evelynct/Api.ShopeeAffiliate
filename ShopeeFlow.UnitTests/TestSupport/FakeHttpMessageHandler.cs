namespace ShopeeFlow.UnitTests.TestSupport;

public sealed class FakeHttpMessageHandler : HttpMessageHandler
{
    public Func<HttpRequestMessage, HttpResponseMessage>? ResponseFactory { get; set; }
    public HttpRequestMessage? LastRequest { get; private set; }
    public string? LastRequestBody { get; private set; }
    public int SendCount { get; private set; }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        SendCount++;
        LastRequest = request;
        LastRequestBody = request.Content is null
            ? null
            : await request.Content.ReadAsStringAsync(cancellationToken);

        if (ResponseFactory is null)
            throw new InvalidOperationException("ResponseFactory was not configured.");

        return ResponseFactory(request);
    }
}
