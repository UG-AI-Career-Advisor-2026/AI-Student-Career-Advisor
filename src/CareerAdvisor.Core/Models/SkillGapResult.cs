using CareerAdvisor.Core.Enums;

namespace CareerAdvisor.Core.Models;

/// <summary>
/// Represents a student's skill comparison against one career-catalogue entry.
/// </summary>
public class SkillGapResult
{
    /// <summary>Gets or sets the saved student profile identifier.</summary>
    public Guid StudentProfileId { get; set; }

    /// <summary>Gets or sets the career profile identifier.</summary>
    public Guid CareerProfileId { get; set; }

    /// <summary>Gets or sets the stable career-catalogue code.</summary>
    public string CareerCode { get; set; } = string.Empty;

    /// <summary>Gets or sets the career title used for display.</summary>
    public string CareerTitle { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the academic MVP comparison baseline. This baseline is not
    /// evidence of professional readiness, employability, or certification.
    /// </summary>
    public SkillProficiency BaselineProficiency { get; set; } =
        SkillProficiency.Intermediate;

    /// <summary>Gets or sets one comparison item per required skill.</summary>
    public List<SkillGapItem> Items { get; set; } = new();

    /// <summary>
    /// Gets the derived number of required skills with no recognized match.
    /// </summary>
    public int MissingCount => CountItems(SkillGapClassification.Missing);

    /// <summary>
    /// Gets the derived number of matched Beginner skills requiring development.
    /// </summary>
    public int NeedsDevelopmentCount =>
        CountItems(SkillGapClassification.NeedsDevelopment);

    /// <summary>
    /// Gets the derived number of skills meeting the academic MVP baseline.
    /// </summary>
    public int MatchedCount => CountItems(SkillGapClassification.Matched);

    private int CountItems(SkillGapClassification classification)
    {
        return Items?.Count(item =>
            item is not null && item.Classification == classification) ?? 0;
    }
}
