using CloneEbay.Contracts;
using CloneEbay.Contracts.Addresses;
using CloneEbay.Domain.Entities;
using CloneEbay.Domain.Exceptions;
using CloneEbay.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CloneEbay.Api.Controllers;

[Authorize]
[Route("api/addresses")]
public class AddressesController : BaseController
{
    private readonly CloneEbayDbContext _db;

    public AddressesController(CloneEbayDbContext db)
    {
        _db = db;
    }

    [HttpGet("my")]
    public async Task<ApiResponse<IReadOnlyList<AddressDto>>> GetMyAddresses(CancellationToken ct)
    {
        var items = await _db.Address
            .AsNoTracking()
            .Where(x => x.userId == CurrentUserId)
            .OrderByDescending(x => x.isDefault == true)
            .ThenByDescending(x => x.id)
            .Select(x => new AddressDto(
                x.id,
                x.fullName,
                x.phone,
                x.street,
                x.city,
                x.state,
                x.country,
                x.isDefault == true
            ))
            .ToListAsync(ct);

        return Success<IReadOnlyList<AddressDto>>(items, "Get addresses successfully", "ADDRESS_LIST_SUCCESS");
    }

    [HttpPost]
    public async Task<ApiResponse<AddressDto>> Create([FromBody] CreateAddressRequest req, CancellationToken ct)
    {
        if (req.isDefault)
        {
            var defaults = await _db.Address
                .Where(x => x.userId == CurrentUserId && x.isDefault == true)
                .ToListAsync(ct);

            foreach (var row in defaults) row.isDefault = false;
        }

        var entity = new Address
        {
            userId = CurrentUserId,
            fullName = req.fullName.Trim(),
            phone = req.phone.Trim(),
            street = req.street.Trim(),
            city = req.city.Trim(),
            state = req.state.Trim(),
            country = req.country.Trim(),
            isDefault = req.isDefault
        };

        _db.Address.Add(entity);
        await _db.SaveChangesAsync(ct);

        return Success(
            new AddressDto(
                entity.id,
                entity.fullName,
                entity.phone,
                entity.street,
                entity.city,
                entity.state,
                entity.country,
                entity.isDefault == true
            ),
            "Create address successfully",
            "ADDRESS_CREATE_SUCCESS"
        );
    }

    [HttpPut("{id:int}")]
    public async Task<ApiResponse<AddressDto>> Update([FromRoute] int id, [FromBody] UpdateAddressRequest req, CancellationToken ct)
    {
        var entity = await _db.Address.FirstOrDefaultAsync(x => x.id == id && x.userId == CurrentUserId, ct);
        if (entity == null)
            throw new NotFoundException("Address not found", "ADDRESS_NOT_FOUND");

        if (req.isDefault)
        {
            var defaults = await _db.Address
                .Where(x => x.userId == CurrentUserId && x.id != id && x.isDefault == true)
                .ToListAsync(ct);

            foreach (var row in defaults) row.isDefault = false;
        }

        entity.fullName = req.fullName.Trim();
        entity.phone = req.phone.Trim();
        entity.street = req.street.Trim();
        entity.city = req.city.Trim();
        entity.state = req.state.Trim();
        entity.country = req.country.Trim();
        entity.isDefault = req.isDefault;

        await _db.SaveChangesAsync(ct);

        return Success(
            new AddressDto(
                entity.id,
                entity.fullName,
                entity.phone,
                entity.street,
                entity.city,
                entity.state,
                entity.country,
                entity.isDefault == true
            ),
            "Update address successfully",
            "ADDRESS_UPDATE_SUCCESS"
        );
    }
}
