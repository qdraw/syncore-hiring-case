using Cubing.Api.Contracts;
using Cubing.Api.Mapping;
using Cubing.Core.Entities;
using Cubing.Core.Packing.Exceptions;
using Cubing.Core.Packing.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Cubing.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CubingController(ICubingCalculationService calculations) : ControllerBase
{
    /// <summary>Calculates (and stores) the boxes needed to pack a quantity of cubic articles.</summary>
    [HttpPost]
    [ProducesResponseType<CubingApiResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(CubingApiRequest request, CancellationToken cancellationToken)
    {
        CubingCalculation calculation;
        try
        {
            calculation = await calculations.CalculateAndStoreAsync(CubingApiMapper.ToDomain(request), cancellationToken);
        }
        catch (CubingValidationException ex)
        {
            foreach (var (field, messages) in ex.Errors)
                foreach (var message in messages)
                    ModelState.AddModelError(field, message);
            return ValidationProblem(ModelState);
        }

        return CreatedAtAction(nameof(Get), new { id = calculation.Id }, CubingApiMapper.ToResponse(calculation));
    }

    /// <summary>Gets a stored cubing calculation.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<CubingApiResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var calculation = await calculations.GetAsync(id, cancellationToken);
        return calculation is null ? NotFound() : Ok(CubingApiMapper.ToResponse(calculation));
    }
}
