using CloneEbay.Application.Categories;
using CloneEbay.Contracts.Categories;
using CloneEbay.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CloneEbay.Infrastructure.Categories;

public sealed class CategoryService : ICategoryService
{
    private readonly CloneEbayDbContext _db;
    public CategoryService(CloneEbayDbContext db) => _db = db;

    public async Task<IReadOnlyList<CategoryDto>> GetAllAsync(CancellationToken ct)
    {
        var cats = await _db.Category
            .AsNoTracking()
            .OrderBy(c => c.name)
            .ToListAsync(ct);

        return cats.Select(c => new CategoryDto(c.id, c.name ?? "")).ToList();
    }
}