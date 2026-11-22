using Cubing.Core.Packing.Models;

namespace Cubing.Core.Packing.Interfaces;

public interface ICustomerProfileResolver
{
    /// <summary>Returns the profile registered for the customer, or null when the customer is unknown.</summary>
    CustomerProfile? Resolve(string customerId);
}
