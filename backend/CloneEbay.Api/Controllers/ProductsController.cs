using CloneEbay.Application.Products;
using CloneEbay.Contracts;
using CloneEbay.Contracts.Products;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloneEbay.Api.Controllers;

[Route("api/products")]
public class ProductsController : BaseController
{
    private readonly IProductService _svc;

    public ProductsController(IProductService svc)
    {
        _svc = svc;
    }

    [HttpGet]
    public async Task<ApiResponse<PagedResponse<ProductListItemDto>>> Get(
        [FromQuery] string? q,
        [FromQuery] int? categoryId,
        [FromQuery] decimal? minPrice,
        [FromQuery] decimal? maxPrice,
        [FromQuery] string? sort,
        [FromQuery] bool? isAuction,
        [FromQuery] string? status,
        [FromQuery] string? condition,
        [FromQuery] bool? inStock,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var data = await _svc.GetProductsAsync(
            q, categoryId, minPrice, maxPrice, sort,
            isAuction, status, condition, inStock,
            page, pageSize, ct);

        return Success(data, "Get products successfully", "PRODUCT_LIST_SUCCESS");
    }

    [HttpGet("{id:int}")]
    public async Task<ApiResponse<ProductDetailDto>> GetById([FromRoute] int id, CancellationToken ct)
        => Success(await _svc.GetByIdAsync(id, ct), "Get product successfully", "PRODUCT_DETAIL_SUCCESS");

    [Authorize]
    [HttpGet("my")]
    public async Task<ApiResponse<PagedResponse<ProductListItemDto>>> GetMyProducts(
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var data = await _svc.GetMyProductsAsync(CurrentUserId, status, page, pageSize, ct);
        return Success(data, "Get my products successfully", "MY_PRODUCT_LIST_SUCCESS");
    }

    [Authorize]
    [HttpPost]
    public async Task<ApiResponse<ProductDetailDto>> Create([FromBody] CreateProductRequest req, CancellationToken ct)
        => Success(await _svc.CreateAsync(CurrentUserId, req, ct), "Create product successfully", "PRODUCT_CREATE_SUCCESS");

    [Authorize]
    [HttpPut("{id:int}")]
    public async Task<ApiResponse<ProductDetailDto>> Update([FromRoute] int id, [FromBody] UpdateProductRequest req, CancellationToken ct)
        => Success(await _svc.UpdateAsync(CurrentUserId, id, req, ct), "Update product successfully", "PRODUCT_UPDATE_SUCCESS");

    [Authorize]
    [HttpPut("{id:int}/inventory")]
    public async Task<ApiResponse<object>> UpdateInventory([FromRoute] int id, [FromBody] UpdateInventoryRequest req, CancellationToken ct)
    {
        await _svc.UpdateInventoryAsync(CurrentUserId, id, req.quantity, ct);
        return Success("Update inventory successfully", "PRODUCT_INVENTORY_UPDATE_SUCCESS");
    }

    [Authorize]
    [HttpPatch("{id:int}/status")]
    public async Task<ApiResponse<ProductDetailDto>> UpdateStatus(
        [FromRoute] int id,
        [FromBody] UpdateProductStatusRequest req,
        CancellationToken ct)
    {
        var data = await _svc.UpdateStatusAsync(CurrentUserId, id, req.status, ct);
        return Success(data, "Update product status successfully", "PRODUCT_STATUS_UPDATE_SUCCESS");
    }

    [Authorize]
    [HttpDelete("{id:int}")]
    public async Task<ApiResponse<object>> Delete([FromRoute] int id, CancellationToken ct)
    {
        await _svc.DeleteAsync(CurrentUserId, id, ct);
        return Success("Delete product successfully", "PRODUCT_DELETE_SUCCESS");
    }

    [Authorize]
    [HttpPost("{id:int}/images")]
    [RequestSizeLimit(25 * 1024 * 1024)]
    public async Task<ApiResponse<ProductDetailDto>> UploadImages([FromRoute] int id, CancellationToken ct)
    {
        var files = Request.Form.Files;
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var data = await _svc.UploadImagesAsync(CurrentUserId, id, files, baseUrl, ct);
        return Success(data, "Upload images successfully", "PRODUCT_UPLOAD_IMAGES_SUCCESS");
    }

    [Authorize]
    [HttpDelete("{id:int}/images")]
    public async Task<ApiResponse<ProductDetailDto>> DeleteImage(
        [FromRoute] int id,
        [FromBody] DeleteProductImageRequest req,
        CancellationToken ct)
    {
        var data = await _svc.DeleteImageAsync(CurrentUserId, id, req.imageUrl, ct);
        return Success(data, "Delete product image successfully", "PRODUCT_DELETE_IMAGE_SUCCESS");
    }
}