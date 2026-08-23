using CareerAdvisor.Core.Models;

namespace CareerAdvisor.Core.SkillGaps;

/// <summary>
/// Selects one deterministic saved-skill match for one required skill using
/// canonical phrases and explicitly approved aliases only.
/// </summary>
public static class SkillNameMatcher
{
    /// <summary>
    /// Finds the highest-proficiency recognized saved skill for one requirement.
    /// The operation is pure and does not load or change application state.
    /// </summary>
    /// <param name="requiredSkillName">
    /// The original career-catalogue required-skill name.
    /// </param>
    /// <param name="studentSkills">The saved-profile skills to compare.</param>
    /// <returns>The selected saved skill, or <see langword="null"/>.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when the required skill name is blank.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="studentSkills"/> is null.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when a matching saved skill has an undefined proficiency.
    /// </exception>
    public static StudentSkill? FindBestMatch(
        string? requiredSkillName,
        IEnumerable<StudentSkill?>? studentSkills)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(requiredSkillName);
        ArgumentNullException.ThrowIfNull(studentSkills);

        var normalizedCanonical =
            SkillNameNormalizer.Normalize(requiredSkillName);

        var acceptedPhrases = GetAcceptedPhrases(
            requiredSkillName,
            normalizedCanonical);

        var candidates = studentSkills
            .Select(skill => CreateCandidate(
                skill,
                normalizedCanonical,
                acceptedPhrases))
            .Where(candidate => candidate is not null)
            .Select(candidate => candidate!)
            .GroupBy(
                candidate => candidate.NormalizedSkillName,
                StringComparer.Ordinal)
            .Select(group => OrderCandidates(group).First())
            .ToList();

        return OrderCandidates(candidates).FirstOrDefault()?.Skill;
    }

    /// <summary>
    /// Determines whether a normalized phrase occurs as consecutive whole tokens
    /// in normalized skill text.
    /// </summary>
    /// <param name="normalizedSkillName">Normalized saved-skill text.</param>
    /// <param name="normalizedPhrase">Normalized approved phrase text.</param>
    /// <returns><see langword="true"/> when the whole-token phrase occurs.</returns>
    public static bool ContainsConsecutiveWholeTokenPhrase(
        string normalizedSkillName,
        string normalizedPhrase)
    {
        if (string.IsNullOrWhiteSpace(normalizedSkillName) ||
            string.IsNullOrWhiteSpace(normalizedPhrase))
        {
            return false;
        }

        var skillTokens = normalizedSkillName.Split(' ');
        var phraseTokens = normalizedPhrase.Split(' ');

        if (phraseTokens.Length > skillTokens.Length)
        {
            return false;
        }

        for (var start = 0;
             start <= skillTokens.Length - phraseTokens.Length;
             start++)
        {
            var matches = true;

            for (var offset = 0; offset < phraseTokens.Length; offset++)
            {
                if (!string.Equals(
                        skillTokens[start + offset],
                        phraseTokens[offset],
                        StringComparison.Ordinal))
                {
                    matches = false;
                    break;
                }
            }

            if (matches)
            {
                return true;
            }
        }

        return false;
    }

    private static IReadOnlyList<string> GetAcceptedPhrases(
        string requiredSkillName,
        string normalizedCanonical)
    {
        var phrases = new List<string> { normalizedCanonical };

        if (SkillAliasCatalog.AliasesByNormalizedRequiredSkill.TryGetValue(
                normalizedCanonical,
                out var aliases))
        {
            phrases.AddRange(aliases.Select(SkillNameNormalizer.Normalize));
        }

        return phrases
            .Where(phrase => phrase.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .ToList();
    }

    private static MatchCandidate? CreateCandidate(
        StudentSkill? skill,
        string normalizedCanonical,
        IReadOnlyList<string> acceptedPhrases)
    {
        if (skill is null)
        {
            throw new ArgumentException(
                "The saved-skill collection cannot contain null entries.",
                "studentSkills");
        }

        var normalizedSkillName =
            SkillNameNormalizer.Normalize(skill.SkillName);

        if (normalizedSkillName.Length == 0)
        {
            return null;
        }

        var matchedPhrase = acceptedPhrases
            .Where(phrase => ContainsConsecutiveWholeTokenPhrase(
                normalizedSkillName,
                phrase))
            .OrderByDescending(CountTokens)
            .ThenBy(phrase => phrase, StringComparer.Ordinal)
            .FirstOrDefault();

        if (matchedPhrase is null)
        {
            return null;
        }

        if (!Enum.IsDefined(skill.Proficiency))
        {
            throw new ArgumentOutOfRangeException(
                nameof(skill),
                skill.Proficiency,
                "The matching saved skill has an invalid proficiency.");
        }

        return new MatchCandidate(
            skill,
            normalizedSkillName,
            string.Equals(
                normalizedSkillName,
                normalizedCanonical,
                StringComparison.Ordinal),
            CountTokens(matchedPhrase));
    }

    private static int CountTokens(string normalizedPhrase)
    {
        return normalizedPhrase.Split(' ').Length;
    }

    private static IOrderedEnumerable<MatchCandidate> OrderCandidates(
        IEnumerable<MatchCandidate> candidates)
    {
        return candidates
            .OrderByDescending(candidate =>
                (int)candidate.Skill.Proficiency)
            .ThenByDescending(candidate => candidate.IsExactCanonicalMatch)
            .ThenByDescending(candidate => candidate.MatchedPhraseTokenCount)
            .ThenBy(
                candidate => candidate.NormalizedSkillName,
                StringComparer.Ordinal)
            .ThenBy(
                candidate => candidate.Skill.SkillName,
                StringComparer.Ordinal)
            .ThenBy(candidate => candidate.Skill.Id);
    }

    private sealed record MatchCandidate(
        StudentSkill Skill,
        string NormalizedSkillName,
        bool IsExactCanonicalMatch,
        int MatchedPhraseTokenCount);
}
