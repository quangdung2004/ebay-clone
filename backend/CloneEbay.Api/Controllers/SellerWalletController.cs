using CloneEbay.Application.Payments;
using CloneEbay.Contracts;
using CloneEbay.Contracts.Payments;
using CloneEbay.Contracts.Products;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloneEbay.Api.Controllers;

[Route("api/seller/wallet")]
[Authorize]
public sealed class SellerWalletController : BaseController
{
    private readonly ISellerWalletService _svc;

    public SellerWalletController(ISellerWalletService svc)
    {
        _svc = svc;
    }

    [HttpGet]
    public async Task<ApiResponse<SellerWalletDto>> GetWallet(CancellationToken ct)
    {
        var result = await _svc.GetWalletAsync(CurrentUserId, ct);
        return Success(result, "Get seller wallet successfully", "SELLER_WALLET_GET_SUCCESS");
    }

    [HttpGet("settlements")]
    public async Task<ApiResponse<PagedResponse<SellerSettlementHistoryItemDto>>> GetSettlementHistory(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _svc.GetSettlementHistoryAsync(CurrentUserId, page, pageSize, ct);
        return Success(result, "Get seller settlement history successfully", "SELLER_SETTLEMENT_HISTORY_SUCCESS");
    }
}