using CloneEbay.Application.Categories;
using CloneEbay.Contracts;
using CloneEbay.Contracts.Categories;
using Microsoft.AspNetCore.Mvc;

namespace CloneEbay.Api.Controllers;

[Route("api/categories")]
public class CategoriesController : BaseController
{
    private readonly ICategoryService _svc;

    public CategoriesController(ICategoryService svc)
    {
        _svc = svc;
    }

    [HttpGet]
    public async Task<ApiResponse<IReadOnlyList<CategoryDto>>> GetAll(CancellationToken ct)
        => Success(await _svc.GetAllAsync(ct), "Get categories successfully", "CATEGORY_LIST_SUCCESS");
}