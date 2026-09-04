namespace ShopeeFlow.DTOs.Common;

public class PagedResponseDto<T>
{
    public List<T> Data { get; set; } = [];

    public int PageNumber { get; set; }

    public int PageSize { get; set; }

    public int TotalRecords { get; set; }

    public int TotalPages => PageSize > 0
        ? (int)Math.Ceiling((double)TotalRecords / PageSize)
        : 0;
}
