using CloneEbay.Contracts.Products;
using Microsoft.AspNetCore.Http;

namespace CloneEbay.Application.Products;

public interface IProductService
{
    Task<PagedResponse<ProductListItemDto>> GetProductsAsync(
        string? q,
        int? categoryId,
        decimal? minPrice,
        decimal? maxPrice,
        string? sort,
        bool? isAuction,
        string? status,
        string? condition,
        bool? inStock,
        int page,
        int pageSize,
        CancellationToken ct);

    Task<ProductDetailDto> GetByIdAsync(int id, CancellationToken ct);

    Task<ProductDetailDto> CreateAsync(int sellerId, CreateProductRequest req, CancellationToken ct);

    Task<ProductDetailDto> UpdateAsync(int sellerId, int productId, UpdateProductRequest req, CancellationToken ct);

    Task UpdateInventoryAsync(int sellerId, int productId, int quantity, CancellationToken ct);

    Task<ProductDetailDto> UploadImagesAsync(
        int sellerId,
        int productId,
        IFormFileCollection files,
        string baseUrl,
        CancellationToken ct);

    Task<ProductDetailDto> DeleteImageAsync(
        int sellerId,
        int productId,
        string imageUrl,
        CancellationToken ct);

    Task<ProductDetailDto> UpdateStatusAsync(
        int sellerId,
        int productId,
        string status,
        CancellationToken ct);

    Task DeleteAsync(int sellerId, int productId, CancellationToken ct);

    Task<PagedResponse<ProductListItemDto>> GetMyProductsAsync(
        int sellerId,
        string? status,
        int page,
        int pageSize,
        CancellationToken ct);
}