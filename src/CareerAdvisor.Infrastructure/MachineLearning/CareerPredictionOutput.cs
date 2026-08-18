using Microsoft.ML.Data;

namespace CareerAdvisor.Infrastructure.MachineLearning;

/// <summary>
/// Represents the raw output returned by the trained multiclass model.
/// Score positions are mapped to career labels in the model metadata.
/// </summary>
public sealed class CareerPredictionOutput
{
    [ColumnName("PredictedLabel")]
    public string PredictedCareerLabel { get; set; } = string.Empty;

    [ColumnName("Score")]
    public float[] Scores { get; set; } = [];
}