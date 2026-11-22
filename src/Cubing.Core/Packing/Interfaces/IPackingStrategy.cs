using Cubing.Core.Packing.Models;

namespace Cubing.Core.Packing.Interfaces;

/// <summary>
/// Extension point for customer-specific packing rules: which articles are accepted and how many fit into one box.
/// </summary>
public interface IPackingStrategy
{
    /// <summary>Returns an error message when this customer's rules do not allow the article, otherwise null.</summary>
    string? ValidateArticle(Dimensions article);

    /// <summary>Maximum number of articles that fit in a single box; 0 when the article does not fit.</summary>
    long GetMaxArticlesPerBox(Dimensions article, Dimensions box);
}
