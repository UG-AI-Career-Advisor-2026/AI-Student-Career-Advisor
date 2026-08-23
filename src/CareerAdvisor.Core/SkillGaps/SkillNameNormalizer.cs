using System.Text;

namespace CareerAdvisor.Core.SkillGaps;

/// <summary>
/// Normalizes skill names into invariant, whitespace-delimited tokens without
/// performing stemming, fuzzy matching, or arbitrary substring matching.
/// </summary>
public static class SkillNameNormalizer
{
    /// <summary>Normalizes casing, whitespace, and common punctuation.</summary>
    /// <param name="value">The skill name to normalize.</param>
    /// <returns>A normalized token phrase, or an empty string for blank input.</returns>
    public static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var builder = new StringBuilder(value.Length);

        foreach (var character in value.Trim().ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(character))
            {
                builder.Append(character);
                continue;
            }

            switch (character)
            {
                case '#':
                    AppendSeparated(builder, "sharp");
                    break;
                case '+':
                    AppendSeparated(builder, "plus");
                    break;
                case '&':
                    AppendSeparated(builder, "and");
                    break;
                default:
                    builder.Append(' ');
                    break;
            }
        }

        return string.Join(
            ' ',
            builder
                .ToString()
                .Split(
                    ' ',
                    StringSplitOptions.RemoveEmptyEntries |
                    StringSplitOptions.TrimEntries));
    }

    private static void AppendSeparated(
        StringBuilder builder,
        string replacement)
    {
        builder.Append(' ');
        builder.Append(replacement);
        builder.Append(' ');
    }
}
