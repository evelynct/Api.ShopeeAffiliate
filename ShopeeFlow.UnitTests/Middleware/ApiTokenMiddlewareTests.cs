using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using ShopeeFlow.Configurations;
using ShopeeFlow.Middleware;

namespace ShopeeFlow.UnitTests.Middleware;

public class ApiTokenMiddlewareTests
{
    #region Happy Path

    [Fact]
    public async Task InvokeAsync_WhenValidToken_CallsNextMiddleware()
    {
        // Arrange
        const string token = "valid-token";
        var nextCalled = false;
        var context = CreateContext("/ProductOffer", token);
        var middleware = new ApiTokenMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        // Act
        await middleware.InvokeAsync(context, CreateOptions(token));

        // Assert
        Assert.True(nextCalled);
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_WhenSwaggerPath_SkipsTokenValidation()
    {
        // Arrange
        var nextCalled = false;
        var context = CreateContext("/swagger/index.html", providedToken: null);
        var middleware = new ApiTokenMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        // Act
        await middleware.InvokeAsync(context, CreateOptions("any-token"));

        // Assert
        Assert.True(nextCalled);
    }

    #endregion

    #region Error Cases

    [Fact]
    public async Task InvokeAsync_WhenTokenMissing_ReturnsUnauthorized()
    {
        // Arrange
        var nextCalled = false;
        var context = CreateContext("/ProductOffer", providedToken: null);
        var middleware = new ApiTokenMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        // Act
        await middleware.InvokeAsync(context, CreateOptions("valid-token"));

        // Assert
        Assert.False(nextCalled);
        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_WhenTokenInvalid_ReturnsUnauthorized()
    {
        // Arrange
        var nextCalled = false;
        var context = CreateContext("/ProductOffer", "wrong-token");
        var middleware = new ApiTokenMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        // Act
        await middleware.InvokeAsync(context, CreateOptions("valid-token"));

        // Assert
        Assert.False(nextCalled);
        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_WhenAccessTokenNotConfigured_ReturnsInternalServerError()
    {
        // Arrange
        var nextCalled = false;
        var context = CreateContext("/ProductOffer", "any-token");
        var middleware = new ApiTokenMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        // Act
        await middleware.InvokeAsync(context, CreateOptions(""));

        // Assert
        Assert.False(nextCalled);
        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
    }

    #endregion

    private static DefaultHttpContext CreateContext(string path, string? providedToken)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        context.Response.Body = new MemoryStream();

        if (!string.IsNullOrWhiteSpace(providedToken))
            context.Request.Headers[ApiSecuritySettings.HeaderName] = providedToken;

        return context;
    }

    private static IOptions<ApiSecuritySettings> CreateOptions(string accessToken)
    {
        return Options.Create(new ApiSecuritySettings
        {
            AccessToken = accessToken
        });
    }
}
