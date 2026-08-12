namespace CareerAdvisor.Core.Models;

public class RoadmapStep
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public int Order { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ResourceLink { get; set; } = string.Empty;
    public bool IsCompleted { get; set; } = false;
}