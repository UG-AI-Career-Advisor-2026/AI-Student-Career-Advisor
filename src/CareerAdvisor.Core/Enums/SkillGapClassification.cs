namespace CareerAdvisor.Core.Enums;

/// <summary>
/// Describes how a saved student skill compares with an academic MVP
/// baseline for a career-catalogue requirement.
/// </summary>
public enum SkillGapClassification
{
    /// <summary>No recognized saved-profile skill matches the requirement.</summary>
    Missing,

    /// <summary>The recognized saved skill is at Beginner proficiency.</summary>
    NeedsDevelopment,

    /// <summary>
    /// The recognized saved skill is at Intermediate proficiency or higher.
    /// </summary>
    Matched
}
