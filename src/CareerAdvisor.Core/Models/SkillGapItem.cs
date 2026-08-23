using CareerAdvisor.Core.Enums;

namespace CareerAdvisor.Core.Models;

/// <summary>
/// Represents the comparison result for one original career-catalogue skill.
/// </summary>
public class SkillGapItem
{
    /// <summary>
    /// Gets or sets the original career-catalogue skill name used for display.
    /// </summary>
    public string RequiredSkillName { get; set; } = string.Empty;

    /// <summary>Gets or sets the comparison classification.</summary>
    public SkillGapClassification Classification { get; set; }

    /// <summary>
    /// Gets or sets the original saved-profile skill name selected as the match.
    /// </summary>
    public string? MatchedStudentSkillName { get; set; }

    /// <summary>
    /// Gets or sets the proficiency of the selected saved-profile skill.
    /// </summary>
    public SkillProficiency? CurrentProficiency { get; set; }
}
