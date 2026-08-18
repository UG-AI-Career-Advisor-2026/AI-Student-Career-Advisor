namespace CareerAdvisor.Infrastructure.MachineLearning;

/// <summary>
/// Records how a CareerIQ recommendation model was trained and evaluated.
/// </summary>
public sealed class CareerModelMetadata
{
    public const string CurrentDatasetVersion = "1.0.0-synthetic-80";

    public DateTime TrainingDateUtc { get; init; }

    public string DatasetVersion { get; init; } = CurrentDatasetVersion;

    public string Trainer { get; init; } =
        "SdcaMaximumEntropy multiclass classification";

    public int RandomSeed { get; init; }

    public int TotalRecordCount { get; init; }

    public int TrainingRecordCount { get; init; }

    public int TestRecordCount { get; init; }

    public double MicroAccuracy { get; init; }

    public double MacroAccuracy { get; init; }

    public double LogLoss { get; init; }

    /// <summary>
    /// Maps each position in the prediction Score array to its career label.
    /// </summary>
    public List<string> ScoreLabels { get; init; } = [];
}