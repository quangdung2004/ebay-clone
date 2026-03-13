using CloneEbay.Application.Bids;
using CloneEbay.Contracts;
using CloneEbay.Contracts.Bids;
using CloneEbay.Contracts.Products;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloneEbay.Api.Controllers;

[Route("api/products/{productId:int}/bids")]
public class BidsController : BaseController
{
    private readonly IBidService _svc;

    public BidsController(IBidService svc)
    {
        _svc = svc;
    }

    [HttpGet]
    public async Task<ApiResponse<PagedResponse<BidHistoryItemDto>>> GetBidHistory(
        [FromRoute] int productId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var data = await _svc.GetBidHistoryAsync(productId, page, pageSize, ct);
        return Success(data, "Get bid history successfully", "BID_HISTORY_SUCCESS");
    }

    [Authorize]
    [HttpPost]
    public async Task<ApiResponse<PlaceBidResultDto>> PlaceBid(
        [FromRoute] int productId,
        [FromBody] PlaceBidRequest req,
        CancellationToken ct)
    {
        var data = await _svc.PlaceBidAsync(CurrentUserId, productId, req, ct);
        return Success(data, "Place bid successfully", "BID_PLACE_SUCCESS");
    }
}