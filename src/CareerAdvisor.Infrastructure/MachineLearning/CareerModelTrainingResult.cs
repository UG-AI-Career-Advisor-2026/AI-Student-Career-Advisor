namespace CareerAdvisor.Infrastructure.MachineLearning;

/// <summary>
/// Identifies the artifacts produced by a successful model-training run.
/// </summary>
public sealed class CareerModelTrainingResult
{
    public string ModelPath { get; init; } = string.Empty;

    public string MetadataPath { get; init; } = string.Empty;

    public CareerModelMetadata Metadata { get; init; } = new();
}