using CloneEbay.Application.Auctions;
using CloneEbay.Contracts;
using CloneEbay.Contracts.Auctions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloneEbay.Api.Controllers;

[Route("api/products/{productId:int}/auction")]
public class AuctionController : BaseController
{
    private readonly IAuctionService _svc;

    public AuctionController(IAuctionService svc)
    {
        _svc = svc;
    }

    [HttpGet("winner")]
    public async Task<ApiResponse<AuctionWinnerDto>> GetWinner(
        [FromRoute] int productId,
        CancellationToken ct)
    {
        var data = await _svc.GetAuctionWinnerAsync(productId, ct);
        return Success(data, "Get auction winner successfully", "AUCTION_WINNER_SUCCESS");
    }

    [Authorize]
    [HttpPost("close")]
    public async Task<ApiResponse<CloseAuctionResultDto>> CloseAuction(
        [FromRoute] int productId,
        CancellationToken ct)
    {
        var data = await _svc.CloseAuctionAsync(CurrentUserId, productId, ct);
        return Success(data, "Close auction successfully", "AUCTION_CLOSE_SUCCESS");
    }
}