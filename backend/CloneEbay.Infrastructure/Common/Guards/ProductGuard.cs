using CloneEbay.Domain.Entities;
using CloneEbay.Domain.Exceptions;
using CloneEbay.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CloneEbay.Infrastructure.Common.Guards;

/// <summary>
/// Reusable guards for product ownership checks.
/// Eliminates the repeated "load product → check null → check owner" pattern in ProductService.
/// </summary>
public static class ProductGuard
{
    /// <summary>
    /// Loads a non-deleted product and verifies ownership.
    /// Throws NotFoundException/ForbiddenException as appropriate.
    /// </summary>
    public static async Task<Product> LoadOwnedProductAsync(
        CloneEbayDbContext db,
        int sellerId,
        int productId,
        CancellationToken ct,
        Func<IQueryable<Product>, IQueryable<Product>>? includes = null)
    {
        IQueryable<Product> query = db.Product;

        if (includes != null)
            query = includes(query);

        var product = await query.FirstOrDefaultAsync(
            x => x.id == productId && x.isDeleted != true, ct);

        if (product == null)
            throw new NotFoundException("Product not found", "PRODUCT_NOT_FOUND");

        if (product.sellerId != sellerId)
            throw new ForbiddenException("You are not the owner", "PRODUCT_FORBIDDEN");

        return product;
    }

    /// <summary>
    /// Ensures product exists (non-deleted) without ownership check.
    /// </summary>
    public static async Task<Product> LoadProductAsync(
        CloneEbayDbContext db,
        int productId,
        CancellationToken ct,
        Func<IQueryable<Product>, IQueryable<Product>>? includes = null)
    {
        IQueryable<Product> query = db.Product;

        if (includes != null)
            query = includes(query);

        var product = await query.FirstOrDefaultAsync(
            x => x.id == productId && x.isDeleted != true, ct);

        if (product == null)
            throw new NotFoundException("Product not found", "PRODUCT_NOT_FOUND");

        return product;
    }
}
