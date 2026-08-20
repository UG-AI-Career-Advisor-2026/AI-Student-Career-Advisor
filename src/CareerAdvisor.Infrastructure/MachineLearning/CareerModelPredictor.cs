using System.Text.Json;
using CareerAdvisor.Core.Recommendations;
using Microsoft.ML;

namespace CareerAdvisor.Infrastructure.MachineLearning;

/// <summary>
/// Loads the saved CareerIQ model and pairs each prediction score
/// with the corresponding label recorded in the model metadata.
/// </summary>
public sealed class CareerModelPredictor :
    ICareerModelPredictor,
    IDisposable
{
    private readonly CareerModelMetadata _metadata;
    private readonly PredictionEngine<
        CareerTrainingInput,
        CareerPredictionOutput> _predictionEngine;

    private readonly object _predictionLock = new();
    private bool _disposed;

    public CareerModelPredictor(
        string modelPath,
        string metadataPath)
    {
        modelPath = ValidateExistingPath(
            modelPath,
            nameof(modelPath),
            "The trained recommendation model was not found.");

        metadataPath = ValidateExistingPath(
            metadataPath,
            nameof(metadataPath),
            "The recommendation model metadata was not found.");

        _metadata = LoadMetadata(metadataPath);
        ValidateMetadata(_metadata);

        var mlContext = new MLContext(
            seed: _metadata.RandomSeed);

        var model = mlContext.Model.Load(
            modelPath,
            out _);

        _predictionEngine = mlContext.Model
            .CreatePredictionEngine<
                CareerTrainingInput,
                CareerPredictionOutput>(model);
    }

    public IReadOnlyList<CareerModelScore> Predict(
        CareerTrainingInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        ObjectDisposedException.ThrowIf(_disposed, this);

        CareerPredictionOutput prediction;

        lock (_predictionLock)
        {
            prediction = _predictionEngine.Predict(input);
        }

        ValidatePrediction(prediction);

        return _metadata.ScoreLabels
            .Select((label, index) =>
                new CareerModelScore(
                    label,
                    prediction.Scores[index]))
            .ToList();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _predictionEngine.Dispose();
        _disposed = true;
    }

    private static CareerModelMetadata LoadMetadata(
        string metadataPath)
    {
        try
        {
            var json = File.ReadAllText(metadataPath);

            return JsonSerializer.Deserialize<CareerModelMetadata>(
                       json,
                       new JsonSerializerOptions
                       {
                           PropertyNameCaseInsensitive = true
                       })
                   ?? throw new InvalidDataException(
                       "The recommendation model metadata is empty.");
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException(
                "The recommendation model metadata is invalid.",
                exception);
        }
    }

    private static void ValidateMetadata(
        CareerModelMetadata metadata)
    {
        var expectedLabels = RecommendationFeatureSchema
            .CareerLabelsByCode
            .Values
            .ToHashSet(StringComparer.Ordinal);

        var actualLabels = metadata.ScoreLabels
            .ToHashSet(StringComparer.Ordinal);

        if (metadata.ScoreLabels.Count != expectedLabels.Count ||
            actualLabels.Count != metadata.ScoreLabels.Count ||
            !expectedLabels.SetEquals(actualLabels))
        {
            throw new InvalidDataException(
                "The recommendation model metadata must contain " +
                "exactly the eight approved career labels.");
        }

        if (metadata.RandomSeed != CareerModelTrainer.DefaultSeed)
        {
            throw new InvalidDataException(
                "The recommendation model metadata contains an " +
                "unexpected training seed.");
        }
    }

    private void ValidatePrediction(
        CareerPredictionOutput prediction)
    {
        if (prediction.Scores is null ||
            prediction.Scores.Length !=
            _metadata.ScoreLabels.Count)
        {
            throw new InvalidOperationException(
                "The recommendation model did not return exactly " +
                "eight career scores.");
        }

        if (prediction.Scores.Any(score =>
                !float.IsFinite(score)))
        {
            throw new InvalidOperationException(
                "The recommendation model returned a non-finite score.");
        }

        if (string.IsNullOrWhiteSpace(
                prediction.PredictedCareerLabel) ||
            !_metadata.ScoreLabels.Contains(
                prediction.PredictedCareerLabel,
                StringComparer.Ordinal))
        {
            throw new InvalidOperationException(
                "The recommendation model returned an unknown " +
                "predicted career label.");
        }
    }

    private static string ValidateExistingPath(
        string path,
        string parameterName,
        string missingFileMessage)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException(
                "A file path is required.",
                parameterName);
        }

        var fullPath = Path.GetFullPath(path);

        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException(
                missingFileMessage,
                fullPath);
        }

        return fullPath;
    }
}