namespace CareerAdvisor.Core.Models;

public class AssessmentSession
{
    /// <summary>
    /// Unique identifier for the assessment session.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Reference to the student profile completing the assessment.
    /// </summary>
    public Guid StudentProfileId { get; set; }

    /// <summary>
    /// Status of the assessment: "InProgress", "Completed", "Abandoned".
    /// </summary>
    public string Status { get; set; } = "InProgress";

    /// <summary>
    /// Timestamp when the assessment session was started.
    /// </summary>
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Timestamp when the assessment session was completed (null if not completed).
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Collection of responses provided during this assessment session.
    /// </summary>
    public List<AssessmentResponse> Responses { get; set; } = new();
}
