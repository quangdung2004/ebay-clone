using CloneEbay.Application.Stores;
using CloneEbay.Contracts;
using CloneEbay.Contracts.Stores;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloneEbay.Api.Controllers;

[Authorize]
[Route("api/stores")]
public class StoresController : BaseController
{
    private readonly IStoreService _storeService;

    public StoresController(IStoreService storeService)
    {
        _storeService = storeService;
    }

    [HttpPost]
    public async Task<ApiResponse<StoreDto>> Create([FromBody] CreateStoreRequest req, CancellationToken ct)
        => Success(await _storeService.CreateAsync(CurrentUserId, req, ct), "Create store successfully", "STORE_CREATE_SUCCESS");

    [HttpGet("me")]
    public async Task<ApiResponse<StoreDto>> GetMyStore(CancellationToken ct)
        => Success(await _storeService.GetMyStoreAsync(CurrentUserId, ct), "Get store successfully", "STORE_ME_SUCCESS");

    [HttpPut("me")]
    public async Task<ApiResponse<StoreDto>> UpdateMyStore([FromBody] UpdateStoreRequest req, CancellationToken ct)
        => Success(await _storeService.UpdateMyStoreAsync(CurrentUserId, req, ct), "Update store successfully", "STORE_UPDATE_SUCCESS");
}