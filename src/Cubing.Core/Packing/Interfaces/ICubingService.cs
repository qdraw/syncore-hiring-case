using Cubing.Core.Packing.Exceptions;
using Cubing.Core.Packing.Models;

namespace Cubing.Core.Packing.Interfaces;

public interface ICubingService
{
    /// <exception cref="CubingValidationException">The request is invalid or the article does not fit the box.</exception>
    CubingResult Cube(CubingRequest request);
}
