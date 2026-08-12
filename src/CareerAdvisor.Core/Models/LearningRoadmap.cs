namespace CareerAdvisor.Core.Models;

public class LearningRoadmap
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid StudentProfileId { get; set; }
    public Guid CareerProfileId { get; set; }
    public List<RoadmapStep> Steps { get; set; } = new();
}