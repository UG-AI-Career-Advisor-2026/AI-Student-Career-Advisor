namespace CareerAdvisor.Core.Models;

/// <summary>
/// Represents a persisted learning roadmap for one saved profile and career.
/// </summary>
public class LearningRoadmap
{
    /// <summary>Gets or sets the roadmap identifier.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Gets or sets the owning saved profile identifier.</summary>
    public Guid StudentProfileId { get; set; }

    /// <summary>Gets or sets the selected supported career identifier.</summary>
    public Guid CareerProfileId { get; set; }

    /// <summary>Gets or sets the UTC time when the roadmap was created.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Gets or sets the ordered persisted roadmap steps.</summary>
    public List<RoadmapStep> Steps { get; set; } = new();
}
