using Cubing.Core.Packing.Exceptions;
using Cubing.Core.Packing.Interfaces;
using Cubing.Core.Packing.Models;
using Cubing.Api.Contracts;
using Cubing.Core.Entities;
using Cubing.Core.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Cubing.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CubingController(
    ICubingService cubingService,
    ICubingCalculationRepository repository,
    IConfiguration configuration) : ControllerBase
{
    /// <summary>Calculates (and stores) the boxes needed to pack a quantity of cubic articles.</summary>
    [HttpPost]
    [ProducesResponseType<CubingApiResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(CubingApiRequest request, CancellationToken cancellationToken)
    {
        CubingResult result;
        try
        {
            result = cubingService.Cube(ToDomain(request, DefaultCustomerId));
        }
        catch (CubingValidationException ex)
        {
            foreach (var (field, messages) in ex.Errors)
                foreach (var message in messages)
                    ModelState.AddModelError(field, message);
            return ValidationProblem(ModelState);
        }

        var calculation = new CubingCalculation
        {
            Id = Guid.NewGuid(),
            CustomerId = result.CustomerId,
            ArticleName = result.ArticleName,
            BoxTypeName = result.BoxTypeName,
            TotalQuantity = result.TotalQuantity,
            Boxes = result.Boxes
                .Select(b => new CalculatedBox { LicensePlateNumber = b.LicensePlateNumber, NumberOfItems = b.NumberOfItems })
                .ToList(),
        };
        await repository.AddAsync(calculation, cancellationToken);

        return CreatedAtAction(nameof(Get), new { id = calculation.Id }, ToResponse(calculation));
    }

    /// <summary>Gets a stored cubing calculation.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<CubingApiResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var calculation = await repository.GetByIdAsync(id, cancellationToken);
        return calculation is null ? NotFound() : Ok(ToResponse(calculation));
    }

    private static CubingApiResponse ToResponse(CubingCalculation c) => new()
    {
        CalculationId = c.Id,
        CustomerId = c.CustomerId,
        ArticleName = c.ArticleName,
        BoxTypeName = c.BoxTypeName,
        TotalQuantity = c.TotalQuantity,
        TotalBoxes = c.Boxes.Count,
        Boxes = c.Boxes
            .OrderBy(b => b.Id)
            .Select(b => new BoxResponse { LicensePlateNumber = b.LicensePlateNumber, NumberOfItems = b.NumberOfItems })
            .ToList(),
    };

    private string DefaultCustomerId => configuration["Cubing:DefaultCustomerId"] ?? string.Empty;

    private static CubingRequest ToDomain(CubingApiRequest r, string defaultCustomerId) => new(
        string.IsNullOrWhiteSpace(r.CustomerId) ? defaultCustomerId : r.CustomerId.Trim(),
        r.Quantity,
        r.ArticleName ?? string.Empty,
        ToDomain(r.ArticleDimensions),
        r.BoxTypeName ?? string.Empty,
        ToDomain(r.BoxDimensions));

    // A missing dimensions object becomes 0x0x0, which the service rejects with a validation error.
    private static Dimensions ToDomain(DimensionsRequest? d) =>
        d is null ? new Dimensions(0, 0, 0) : new Dimensions(d.Length, d.Width, d.Height);
}
