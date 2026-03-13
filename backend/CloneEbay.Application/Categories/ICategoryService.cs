using CloneEbay.Contracts.Categories;

namespace CloneEbay.Application.Categories;

public interface ICategoryService
{
    Task<IReadOnlyList<CategoryDto>> GetAllAsync(CancellationToken ct);
}