namespace CareerAdvisor.Core.Models;

public class AssessmentResponse
{
    /// <summary>
    /// Unique identifier for the response.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Reference to the assessment session.
    /// </summary>
    public Guid AssessmentSessionId { get; set; }

    /// <summary>
    /// Reference to the question being answered.
    /// </summary>
    public Guid QuestionId { get; set; }

    /// <summary>
    /// Reference to the selected option.
    /// </summary>
    public Guid OptionId { get; set; }

    /// <summary>
    /// Timestamp when the response was recorded.
    /// </summary>
    public DateTime RespondedAt { get; set; } = DateTime.UtcNow;
}
