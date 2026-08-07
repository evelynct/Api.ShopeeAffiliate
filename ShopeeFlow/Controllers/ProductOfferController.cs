using Microsoft.AspNetCore.Mvc;
using ShopeeFlow.DTOs.Common;
using ShopeeFlow.DTOs.Shopee;
using ShopeeFlow.Interfaces.Services;

namespace ShopeeFlow.Controllers;

[ApiController]
[Route("[controller]")]
[Produces("application/json")]
public class ProductOfferController : ControllerBase
{
    private readonly IProductOfferService _productOfferService;
    private readonly ILogger<ProductOfferController> _logger;

    public ProductOfferController(
        IProductOfferService productOfferService,
        ILogger<ProductOfferController> logger)
    {
        _productOfferService = productOfferService;
        _logger = logger;
    }

    /// <summary>Search product offers (Shopee productOfferV2).</summary>
    /// <remarks>
    /// Tip: SortType=5 (CommissionDesc) + IsAmsOffer=true usually returns stronger commissions.
    /// AMS = Affiliate Marketing Solution (seller campaigns with affiliate payout).
    /// </remarks>
    [HttpGet]
    [ProducesResponseType(typeof(BaseResponse<ProductOfferListResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Search(
        [FromQuery] SearchProductOffersRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _productOfferService.SearchAsync(request, cancellationToken);
            if (result.IsFailed)
            {
                return StatusCode(result.StatusCode, new BaseResponse
                {
                    ErrorDescription = result.Error
                });
            }

            return Ok(new BaseResponse<ProductOfferListResponseDto>
            {
                Data = result.Value
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ProductOfferController -> Search]: {Message}", ex.Message);
            return StatusCode(StatusCodes.Status500InternalServerError, new BaseResponse
            {
                ErrorDescription = "Unexpected error while searching product offers."
            });
        }
    }
}
