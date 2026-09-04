using System.ComponentModel.DataAnnotations;

namespace ShopeeFlow.DTOs.PublishedProduct;

public class SearchPublishedProductsRequest
{
    [Range(1, int.MaxValue)]
    public int PageNumber { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 20;

    /// <summary>Filter by CreatedAt day in Brasilia (dd/MM/yyyy or yyyy-MM-dd). Default: today.</summary>
    public string? Date { get; set; }

    /// <summary>Filter by posting status. Omit to return both.</summary>
    public bool? IsPosted { get; set; }
}
