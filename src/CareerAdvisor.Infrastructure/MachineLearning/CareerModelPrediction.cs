namespace CareerAdvisor.Infrastructure.MachineLearning;

/// <summary>
/// Pairs one raw model score with its authoritative career label.
/// </summary>
public sealed record CareerModelScore(
    string CareerLabel,
    float Score);

/// <summary>
/// Produces validated career scores from a trained ML.NET model.
/// </summary>
public interface ICareerModelPredictor
{
    IReadOnlyList<CareerModelScore> Predict(
        CareerTrainingInput input);
}