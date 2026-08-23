using CareerAdvisor.Core.Enums;

namespace CareerAdvisor.Core.SkillGaps;

/// <summary>
/// Defines the deterministic academic MVP classification rules for skill gaps.
/// </summary>
public static class SkillGapRules
{
    /// <summary>
    /// Gets the academic MVP comparison baseline. It is not a professional
    /// readiness, employability, or certification standard.
    /// </summary>
    public static SkillProficiency BaselineProficiency =>
        SkillProficiency.Intermediate;

    /// <summary>
    /// Classifies a missing or recognized saved-skill proficiency.
    /// </summary>
    /// <param name="proficiency">
    /// The matched saved-skill proficiency, or <see langword="null"/> when no
    /// recognized saved skill exists.
    /// </param>
    /// <returns>The deterministic skill-gap classification.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="proficiency"/> is not a defined value.
    /// </exception>
    public static SkillGapClassification Classify(
        SkillProficiency? proficiency)
    {
        if (proficiency is null)
        {
            return SkillGapClassification.Missing;
        }

        if (!Enum.IsDefined(proficiency.Value))
        {
            throw new ArgumentOutOfRangeException(
                nameof(proficiency),
                proficiency,
                "The skill proficiency is not recognized.");
        }

        return proficiency == SkillProficiency.Beginner
            ? SkillGapClassification.NeedsDevelopment
            : SkillGapClassification.Matched;
    }
}
