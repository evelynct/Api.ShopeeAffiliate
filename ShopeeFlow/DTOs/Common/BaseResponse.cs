namespace ShopeeFlow.DTOs.Common;

public class BaseResponse
{
    public string? ErrorDescription { get; set; }
}

public class BaseResponse<T> : BaseResponse
{
    public T? Data { get; set; }
}
