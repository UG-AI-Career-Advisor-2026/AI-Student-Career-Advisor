namespace CareerAdvisor.Core.Models;

public class CareerProfile
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Code { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public List<string> RequiredSkills { get; set; } = new();

    public List<string> RecommendedCertifications { get; set; } = new();

    public List<string> SuggestedLearningTopics { get; set; } = new();
}