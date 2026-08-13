namespace CareerAdvisor.Core.Models;

public class AssessmentQuestion
{
    /// <summary>
    /// Unique identifier for the question.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Unique code for the question (e.g., "Q1_INT", "Q2_SKILL").
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// The question text.
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Category of the question: "Interests", "Skills", or "WorkPreferences".
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Indicates whether this question is required.
    /// </summary>
    public bool IsRequired { get; set; } = true;

    /// <summary>
    /// Display order of the question in the assessment.
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// Available options for this question.
    /// </summary>
    public List<AssessmentOption> Options { get; set; } = new();

    /// <summary>
    /// Timestamp when the question was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Timestamp when the question was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
