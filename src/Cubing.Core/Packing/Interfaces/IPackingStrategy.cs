using Cubing.Core.Packing.Models;

namespace Cubing.Core.Packing.Interfaces;

/// <summary>
/// Extension point for customer-specific packing rules: how many articles fit into one box.
/// </summary>
public interface IPackingStrategy
{
    /// <summary>Maximum number of articles that fit in a single box; 0 when the article does not fit.</summary>
    long GetMaxArticlesPerBox(Dimensions article, Dimensions box);
}
