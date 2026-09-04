using Microsoft.AspNetCore.Mvc;
using ShopeeFlow.DTOs.Common;
using ShopeeFlow.DTOs.PublishedProduct;
using ShopeeFlow.Interfaces.Services;

namespace ShopeeFlow.Controllers;

[ApiController]
[Route("[controller]")]
[Produces("application/json")]
public class PublishedProductController : ControllerBase
{
    private readonly IPublishedProductService _publishedProductService;
    private readonly ILogger<PublishedProductController> _logger;

    public PublishedProductController(
        IPublishedProductService publishedProductService,
        ILogger<PublishedProductController> logger)
    {
        _publishedProductService = publishedProductService;
        _logger = logger;
    }

    /// <summary>List products stored in the SQLite queue (paginated).</summary>
    /// <remarks>
    /// Date defaults to today (Brasilia). Formats: dd/MM/yyyy or yyyy-MM-dd.
    /// IsPosted=true/false filters the page; PostedCount/PendingCount always reflect the full day.
    /// </remarks>
    [HttpGet]
    [ProducesResponseType(typeof(BaseResponse<PublishedProductSearchResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Search(
        [FromQuery] SearchPublishedProductsRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _publishedProductService.SearchAsync(request, cancellationToken);
            if (result.IsFailed)
            {
                return StatusCode(result.StatusCode, new BaseResponse
                {
                    ErrorDescription = result.Error
                });
            }

            return Ok(new BaseResponse<PublishedProductSearchResultDto>
            {
                Data = result.Value
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[PublishedProductController -> Search]: {Message}", ex.Message);
            return StatusCode(StatusCodes.Status500InternalServerError, new BaseResponse
            {
                ErrorDescription = "Unexpected error while listing published products."
            });
        }
    }
}
