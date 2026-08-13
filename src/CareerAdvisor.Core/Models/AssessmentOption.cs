namespace CareerAdvisor.Core.Models;

public class AssessmentOption
{
    /// <summary>
    /// Unique identifier for the option.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Unique code for the option (e.g., "Q1_OPT_A", "Q1_OPT_B").
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// The text value of the option.
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// Optional description providing more context about the option.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Foreign key to the parent AssessmentQuestion.
    /// </summary>
    public Guid QuestionId { get; set; }
}
