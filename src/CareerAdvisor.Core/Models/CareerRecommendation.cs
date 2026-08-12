namespace CareerAdvisor.Core.Models;

public class CareerRecommendation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CareerProfileId { get; set; }
    public CareerProfile? Career { get; set; }
    public double MatchScore { get; set; } // e.g., 0.85 for 85% match
    public string Reasoning { get; set; } = string.Empty;
}