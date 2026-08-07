using System.Net;

namespace ShopeeFlow.DTOs.Common;

public class Result
{
    public bool IsSuccess { get; protected init; }
    public bool IsFailed => !IsSuccess;
    public string? Error { get; protected init; }

    /// <summary>
    /// HTTP status to return from the API when the result failed.
    /// </summary>
    public int StatusCode { get; protected init; }

    /// <summary>
    /// Optional provider-specific code (e.g. Shopee GraphQL extensions.code).
    /// </summary>
    public int? ProviderErrorCode { get; protected init; }

    public static Result Ok() => new()
    {
        IsSuccess = true,
        StatusCode = (int)HttpStatusCode.OK
    };

    public static Result Fail(string error, HttpStatusCode statusCode, int? providerErrorCode = null) => new()
    {
        IsSuccess = false,
        Error = error,
        StatusCode = (int)statusCode,
        ProviderErrorCode = providerErrorCode
    };

    public static Result Fail(string error, int statusCode, int? providerErrorCode = null) => new()
    {
        IsSuccess = false,
        Error = error,
        StatusCode = statusCode,
        ProviderErrorCode = providerErrorCode
    };
}

public class Result<T> : Result
{
    public T? Value { get; private init; }

    public static Result<T> Ok(T value) => new()
    {
        IsSuccess = true,
        Value = value,
        StatusCode = (int)HttpStatusCode.OK
    };

    public static new Result<T> Fail(string error, HttpStatusCode statusCode, int? providerErrorCode = null) => new()
    {
        IsSuccess = false,
        Error = error,
        StatusCode = (int)statusCode,
        ProviderErrorCode = providerErrorCode
    };

    public static new Result<T> Fail(string error, int statusCode, int? providerErrorCode = null) => new()
    {
        IsSuccess = false,
        Error = error,
        StatusCode = statusCode,
        ProviderErrorCode = providerErrorCode
    };

    public static Result<T> FailFrom(Result failedResult) => new()
    {
        IsSuccess = false,
        Error = failedResult.Error,
        StatusCode = failedResult.StatusCode,
        ProviderErrorCode = failedResult.ProviderErrorCode
    };
}
