namespace CareerAdvisor.Core.Models;

/// <summary>Represents one ordered step in a persisted learning roadmap.</summary>
public class RoadmapStep
{
    /// <summary>Gets or sets the roadmap-step identifier.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Gets or sets the owning roadmap identifier.</summary>
    public Guid LearningRoadmapId { get; set; }

    /// <summary>Gets or sets the one-based display order.</summary>
    public int Order { get; set; }

    /// <summary>Gets or sets the stable step title.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Gets or sets the stable academic guidance.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Gets or sets an optional resource link.</summary>
    public string ResourceLink { get; set; } = string.Empty;

    /// <summary>Gets or sets whether the student completed this step.</summary>
    public bool IsCompleted { get; set; } = false;

    /// <summary>Gets or sets the UTC completion time, when completed.</summary>
    public DateTime? CompletedAt { get; set; }
}
