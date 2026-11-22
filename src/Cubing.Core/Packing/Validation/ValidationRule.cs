namespace Cubing.Core.Packing.Validation;

/// <summary>One small, declarative check: the error <paramref name="Message"/> is reported for <paramref name="Field"/> when the rule is violated.</summary>
internal sealed record ValidationRule<T>(string Field, Func<T, bool> IsViolated, string Message)
{
    /// <summary>Runs all rules and returns the messages per field. Empty when everything is valid.</summary>
    public static Dictionary<string, string[]> Evaluate(IEnumerable<ValidationRule<T>> rules, T subject) =>
        rules.Where(r => r.IsViolated(subject))
            .GroupBy(r => r.Field)
            .ToDictionary(g => g.Key, g => g.Select(r => r.Message).ToArray());
}
