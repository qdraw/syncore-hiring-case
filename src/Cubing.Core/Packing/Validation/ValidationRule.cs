namespace Cubing.Core.Packing.Validation;

/// <summary>A check on <typeparamref name="T"/>: when <paramref name="IsViolated"/> returns true, <paramref name="Message"/> is reported for <paramref name="Field"/>.</summary>
internal sealed record ValidationRule<T>(string Field, Func<T, bool> IsViolated, string Message)
{
    /// <summary>Returns the error messages per field. Empty when the subject is valid.</summary>
    public static Dictionary<string, string[]> Evaluate(IEnumerable<ValidationRule<T>> rules, T subject)
    {
        var violated = rules.Where(rule => rule.IsViolated(subject));

        return violated
            .GroupBy(rule => rule.Field)
            .ToDictionary(group => group.Key, group => group.Select(rule => rule.Message).ToArray());
    }
}
