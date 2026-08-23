namespace CareerAdvisor.Core.Recommendations;

public static class RecommendationDisclaimer
{
    public const string Text =
        "Career recommendations are advisory and are based on the " +
        "profile, assessment responses and synthetic academic data. " +
        "They do not guarantee career success or employment.";

    /// <summary>
    /// Removes every exact ordinal occurrence of the canonical disclaimer
    /// from presentation text without changing the persisted source value.
    /// </summary>
    public static string RemoveExactOccurrences(string? reasoning)
    {
        if (string.IsNullOrEmpty(reasoning))
        {
            return reasoning ?? string.Empty;
        }

        return reasoning.Replace(
            Text,
            string.Empty,
            StringComparison.Ordinal);
    }
}
