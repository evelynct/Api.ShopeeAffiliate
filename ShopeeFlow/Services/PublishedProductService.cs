using System.Net;
using ShopeeFlow.DTOs.Common;
using ShopeeFlow.DTOs.PublishedProduct;
using ShopeeFlow.Helpers;
using ShopeeFlow.Interfaces.Data;
using ShopeeFlow.Interfaces.Services;
using ShopeeFlow.Models;

namespace ShopeeFlow.Services;

public class PublishedProductService : IPublishedProductService
{
    private readonly IPublishedProductDAO _publishedProductDAO;
    private readonly TimeProvider _timeProvider;

    public PublishedProductService(
        IPublishedProductDAO publishedProductDAO,
        TimeProvider timeProvider)
    {
        _publishedProductDAO = publishedProductDAO;
        _timeProvider = timeProvider;
    }

    public async Task<Result<PublishedProductSearchResultDto>> SearchAsync(
        SearchPublishedProductsRequest request,
        CancellationToken cancellationToken = default)
    {
        var filterDateResult = ResolveFilterDate(request.Date);
        if (filterDateResult.IsFailed)
            return Result<PublishedProductSearchResultDto>.FailFrom(filterDateResult);

        var filterDate = filterDateResult.Value!;
        var (startUnix, endUnix) = BrasiliaTimeZone.GetLocalDayBoundsUnix(filterDate);

        var filter = new PublishedProductSearchFilter
        {
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            CreatedFromUnix = startUnix,
            CreatedToUnix = endUnix,
            IsPosted = request.IsPosted
        };

        var searchResult = await _publishedProductDAO.SearchAsync(filter, cancellationToken);

        return Result<PublishedProductSearchResultDto>.Ok(new PublishedProductSearchResultDto
        {
            Date = filterDate,
            PostedCount = searchResult.PostedCount,
            PendingCount = searchResult.PendingCount,
            Page = new PagedResponseDto<ReadPublishedProductDto>
            {
                Data = searchResult.Items.Select(ToReadDto).ToList(),
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                TotalRecords = searchResult.TotalRecords
            }
        });
    }

    private DateOnly GetTodayInBrasilia()
    {
        var localNow = TimeZoneInfo.ConvertTime(_timeProvider.GetUtcNow(), BrasiliaTimeZone.Resolve());
        return DateOnly.FromDateTime(localNow.DateTime);
    }

    private Result<DateOnly> ResolveFilterDate(string? rawDate)
    {
        if (string.IsNullOrWhiteSpace(rawDate))
            return Result<DateOnly>.Ok(GetTodayInBrasilia());

        if (LocalDateParser.TryParseFilterDate(rawDate, out var parsed))
            return Result<DateOnly>.Ok(parsed);

        return Result<DateOnly>.Fail(
            "Date must use dd/MM/yyyy (ex.: 04/09/2026) or yyyy-MM-dd (ex.: 2026-09-04).",
            HttpStatusCode.BadRequest);
    }

    private static ReadPublishedProductDto ToReadDto(PublishedProduct product)
    {
        return new ReadPublishedProductDto
        {
            Id = product.Id,
            ItemId = product.ItemId,
            IsPosted = product.IsPosted,
            CreatedAt = product.CreatedAt,
            PostedAt = product.PostedAt,
            ProductName = product.ProductName,
            ImageUrl = product.ImageUrl,
            OfferLink = product.OfferLink,
            Price = product.Price,
            Commission = product.Commission,
            CommissionRate = product.CommissionRate,
            Score = product.Score
        };
    }
}
