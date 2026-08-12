using System.Collections.Generic;

namespace CareerAdvisor.Core.Models;

/// <summary>
/// Represents a technology career path with its associated skills,
/// certifications, and learning topics.
/// </summary>
public class Career
{
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public List<string> RequiredSkills { get; set; } = new();

    public List<string> RecommendedCertifications { get; set; } = new();

    public List<string> SuggestedLearningTopics { get; set; } = new();
}