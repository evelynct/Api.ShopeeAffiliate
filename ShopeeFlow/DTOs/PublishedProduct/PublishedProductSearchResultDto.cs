using ShopeeFlow.DTOs.Common;

namespace ShopeeFlow.DTOs.PublishedProduct;

public class PublishedProductSearchResultDto
{
    public DateOnly Date { get; set; }

    public int PostedCount { get; set; }

    public int PendingCount { get; set; }

    public PagedResponseDto<ReadPublishedProductDto> Page { get; set; } = new();
}
