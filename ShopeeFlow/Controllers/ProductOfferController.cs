using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using ShopeeFlow.Configurations;
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
    private readonly CollectSettings _collectSettings;
    private readonly ILogger<ProductOfferController> _logger;

    public ProductOfferController(
        IProductOfferService productOfferService,
        IOptions<CollectSettings> collectSettings,
        ILogger<ProductOfferController> logger)
    {
        _productOfferService = productOfferService;
        _collectSettings = collectSettings.Value;
        _logger = logger;
    }

    /// <summary>Collect qualified product offers into the SQLite queue (paginates until daily limit or no more pages).</summary>
    /// <remarks>
    /// Omit query params to use Collect settings from appsettings (Casa + commission sort).
    /// Limit controls page size per Shopee request (default 50); the endpoint keeps paging until the daily quota is filled.
    /// </remarks>
    [HttpGet]
    [ProducesResponseType(typeof(BaseResponse<ScheduledCollectResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Search(
        [FromQuery] SearchProductOffersRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var collectRequest = _collectSettings.MergeWithDefaults(request);
            var result = await _productOfferService.CollectAllPagesAsync(collectRequest, cancellationToken);
            if (result.IsFailed)
            {
                return StatusCode(result.StatusCode, new BaseResponse
                {
                    ErrorDescription = result.Error
                });
            }

            return Ok(new BaseResponse<ScheduledCollectResultDto>
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
