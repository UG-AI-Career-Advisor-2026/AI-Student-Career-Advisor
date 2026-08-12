namespace CareerAdvisor.Core.Models;

public class RecommendationSession
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid StudentProfileId { get; set; }
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public List<CareerRecommendation> Recommendations { get; set; } = new();
}