namespace CareerAdvisor.Core.Models;

public class CareerRecommendation
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid RecommendationSessionId { get; set; }

    public Guid CareerProfileId { get; set; }

    public CareerProfile? Career { get; set; }

    public double MatchScore { get; set; }

    public string Reasoning { get; set; } = string.Empty;
}