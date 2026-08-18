using System.Text.Json;
using CareerAdvisor.Core.Recommendations;
using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Transforms;

namespace CareerAdvisor.Infrastructure.MachineLearning;

public sealed class CareerModelTrainer
{
    public const int DefaultSeed = 42;

    private const string LabelColumn = "Label";
    private const string FeaturesColumn = "Features";
    private const string ScoreColumn = "Score";
    private const string PredictedLabelColumn = "PredictedLabel";

    private static readonly string[] CategoricalColumns =
    [
        nameof(CareerTrainingInput.AcademicBackground),
        nameof(CareerTrainingInput.AcademicLevel),
        nameof(CareerTrainingInput.PreferredEnvironment),
        nameof(CareerTrainingInput.PreferredPace),
        nameof(CareerTrainingInput.StabilityPreference),
        nameof(CareerTrainingInput.CompensationPreference),
        nameof(CareerTrainingInput.IndustryPreference)
    ];

    private readonly MLContext _mlContext;

    public CareerModelTrainer(int seed = DefaultSeed)
    {
        Seed = seed;
        _mlContext = new MLContext(seed);
    }

    public int Seed { get; }

    public CareerModelTrainingResult Train(
        string datasetPath,
        string modelPath,
        string metadataPath,
        string datasetVersion = CareerModelMetadata.CurrentDatasetVersion)
    {
        ValidateRequiredPath(datasetPath, nameof(datasetPath));
        ValidateRequiredPath(modelPath, nameof(modelPath));
        ValidateRequiredPath(metadataPath, nameof(metadataPath));

        datasetPath = Path.GetFullPath(datasetPath);
        modelPath = Path.GetFullPath(modelPath);
        metadataPath = Path.GetFullPath(metadataPath);

        if (!File.Exists(datasetPath))
        {
            throw new FileNotFoundException(
                "The approved career training dataset was not found.",
                datasetPath);
        }

        ValidateHeader(datasetPath);

        var allData = _mlContext.Data.LoadFromTextFile<CareerTrainingInput>(
            datasetPath,
            hasHeader: true,
            separatorChar: ',',
            allowQuoting: true,
            trimWhitespace: true);

        var records = _mlContext.Data
            .CreateEnumerable<CareerTrainingInput>(
                allData,
                reuseRowObject: false)
            .ToList();

        ValidateRecords(records);

        var split = _mlContext.Data.TrainTestSplit(
            allData,
            testFraction: 0.20,
            seed: Seed);

        var trainingRecordCount = GetRecordCount(split.TrainSet);
        var testRecordCount = GetRecordCount(split.TestSet);

        var categoricalEncodingColumns = CategoricalColumns
            .Select(column => new InputOutputColumnPair(
                $"{column}Encoded",
                column))
            .ToArray();

        var featureColumns = RecommendationFeatureSchema.NumericColumns
            .Concat(categoricalEncodingColumns.Select(pair => pair.OutputColumnName))
            .ToArray();

        var pipeline = _mlContext.Transforms.Conversion
            .MapValueToKey(
                outputColumnName: LabelColumn,
                inputColumnName: nameof(CareerTrainingInput.CareerLabel),
                keyOrdinality:
                    ValueToKeyMappingEstimator.KeyOrdinality.ByValue)
            .Append(
                _mlContext.Transforms.Categorical.OneHotEncoding(
                    categoricalEncodingColumns))
            .Append(
                _mlContext.Transforms.Concatenate(
                    FeaturesColumn,
                    featureColumns))
            .Append(
                _mlContext.Transforms.NormalizeMinMax(
                    FeaturesColumn))
            .Append(
                _mlContext.MulticlassClassification.Trainers
                    .SdcaMaximumEntropy(
                        labelColumnName: LabelColumn,
                        featureColumnName: FeaturesColumn));

        var trainedModel = pipeline.Fit(split.TrainSet);
        var scoredTestData = trainedModel.Transform(split.TestSet);

        var metrics = _mlContext.MulticlassClassification.Evaluate(
            scoredTestData,
            labelColumnName: LabelColumn,
            scoreColumnName: ScoreColumn,
            predictedLabelColumnName: PredictedLabelColumn);

        ValidateMetrics(metrics);

        var scoreLabels = ReadScoreLabels(scoredTestData.Schema);
        ValidateScoreLabels(scoreLabels);

        var labelDecoder = _mlContext.Transforms.Conversion
            .MapKeyToValue(
                outputColumnName: PredictedLabelColumn,
                inputColumnName: PredictedLabelColumn)
            .Fit(scoredTestData);

        var modelWithDecodedLabel = trainedModel.Append(labelDecoder);

        CreateParentDirectory(modelPath);
        CreateParentDirectory(metadataPath);

        _mlContext.Model.Save(
            modelWithDecodedLabel,
            allData.Schema,
            modelPath);

        var metadata = new CareerModelMetadata
        {
            TrainingDateUtc = DateTime.UtcNow,
            DatasetVersion = datasetVersion,
            Trainer = "SdcaMaximumEntropy multiclass classification",
            RandomSeed = Seed,
            TotalRecordCount = records.Count,
            TrainingRecordCount = trainingRecordCount,
            TestRecordCount = testRecordCount,
            MicroAccuracy = metrics.MicroAccuracy,
            MacroAccuracy = metrics.MacroAccuracy,
            LogLoss = metrics.LogLoss,
            ScoreLabels = scoreLabels
        };

        var metadataJson = JsonSerializer.Serialize(
            metadata,
            new JsonSerializerOptions
            {
                WriteIndented = true
            });

        File.WriteAllText(metadataPath, metadataJson);

        return new CareerModelTrainingResult
        {
            ModelPath = modelPath,
            MetadataPath = metadataPath,
            Metadata = metadata
        };
    }

    private int GetRecordCount(IDataView data)
    {
        return _mlContext.Data
            .CreateEnumerable<CareerTrainingInput>(
                data,
                reuseRowObject: false)
            .Count();
    }

    private static void ValidateHeader(string datasetPath)
    {
        var headerLine = File.ReadLines(datasetPath).FirstOrDefault();

        if (string.IsNullOrWhiteSpace(headerLine))
        {
            throw new InvalidDataException(
                "The training dataset is empty.");
        }

        var actualColumns = headerLine
            .Split(',')
            .Select(column => column.Trim())
            .ToArray();

        if (!actualColumns.SequenceEqual(
                RecommendationFeatureSchema.RequiredColumns,
                StringComparer.Ordinal))
        {
            throw new InvalidDataException(
                "The training dataset columns do not match the approved recommendation feature schema.");
        }
    }

    private static void ValidateRecords(
        IReadOnlyCollection<CareerTrainingInput> records)
    {
        var requiredMinimum =
            RecommendationFeatureSchema.MinimumRecordsPerCareer *
            RecommendationFeatureSchema.CareerLabelsByCode.Count;

        if (records.Count < requiredMinimum)
        {
            throw new InvalidDataException(
                $"The training dataset contains {records.Count} records; at least {requiredMinimum} are required.");
        }

        var expectedLabels = RecommendationFeatureSchema
            .CareerLabelsByCode
            .Values
            .ToHashSet(StringComparer.Ordinal);

        var labelCounts = new Dictionary<string, int>(
            StringComparer.Ordinal);

        var rowNumber = 1;

        foreach (var record in records)
        {
            rowNumber++;

            ValidateRequiredText(
                record.AcademicBackground,
                nameof(record.AcademicBackground),
                rowNumber);

            ValidateCategoricalValue(
                nameof(record.AcademicLevel),
                record.AcademicLevel,
                rowNumber);

            ValidateCategoricalValue(
                nameof(record.PreferredEnvironment),
                record.PreferredEnvironment,
                rowNumber);

            ValidateCategoricalValue(
                nameof(record.PreferredPace),
                record.PreferredPace,
                rowNumber);

            ValidateCategoricalValue(
                nameof(record.StabilityPreference),
                record.StabilityPreference,
                rowNumber);

            ValidateCategoricalValue(
                nameof(record.CompensationPreference),
                record.CompensationPreference,
                rowNumber);

            ValidateCategoricalValue(
                nameof(record.IndustryPreference),
                record.IndustryPreference,
                rowNumber);

            foreach (var numericValue in GetNumericValues(record))
            {
                ValidateNumericValue(
                    numericValue.Column,
                    numericValue.Value,
                    rowNumber);
            }

            ValidateRequiredText(
                record.CareerLabel,
                nameof(record.CareerLabel),
                rowNumber);

            if (!expectedLabels.Contains(record.CareerLabel))
            {
                throw new InvalidDataException(
                    $"Row {rowNumber} contains unrecognized career label '{record.CareerLabel}'.");
            }

            labelCounts.TryGetValue(
                record.CareerLabel,
                out var currentCount);

            labelCounts[record.CareerLabel] = currentCount + 1;
        }

        if (!expectedLabels.SetEquals(labelCounts.Keys))
        {
            throw new InvalidDataException(
                "The training dataset does not contain all eight approved career labels.");
        }

        if (labelCounts.Values.Any(
                count => count <
                    RecommendationFeatureSchema.MinimumRecordsPerCareer))
        {
            throw new InvalidDataException(
                "Every career must have at least ten training records.");
        }

        if (labelCounts.Values.Distinct().Count() != 1)
        {
            throw new InvalidDataException(
                "The training dataset must have equal representation for every career.");
        }
    }

    private static IEnumerable<(string Column, float Value)>
        GetNumericValues(CareerTrainingInput record)
    {
        yield return (
            nameof(record.ProgrammingSkill),
            record.ProgrammingSkill);

        yield return (
            nameof(record.DataSkill),
            record.DataSkill);

        yield return (
            nameof(record.CybersecuritySkill),
            record.CybersecuritySkill);

        yield return (
            nameof(record.CloudSkill),
            record.CloudSkill);

        yield return (
            nameof(record.NetworkingSkill),
            record.NetworkingSkill);

        yield return (
            nameof(record.DatabaseSkill),
            record.DatabaseSkill);

        yield return (
            nameof(record.DesignSkill),
            record.DesignSkill);

        yield return (
            nameof(record.AISkill),
            record.AISkill);

        yield return (
            nameof(record.TechnologyInterest),
            record.TechnologyInterest);

        yield return (
            nameof(record.DataInterest),
            record.DataInterest);

        yield return (
            nameof(record.DesignInterest),
            record.DesignInterest);

        yield return (
            nameof(record.LeadershipInterest),
            record.LeadershipInterest);

        yield return (
            nameof(record.SocialImpactInterest),
            record.SocialImpactInterest);

        yield return (
            nameof(record.ProgrammingSelfAssessment),
            record.ProgrammingSelfAssessment);

        yield return (
            nameof(record.CommunicationSelfAssessment),
            record.CommunicationSelfAssessment);

        yield return (
            nameof(record.ProblemSolvingSelfAssessment),
            record.ProblemSolvingSelfAssessment);

        yield return (
            nameof(record.CollaborationSelfAssessment),
            record.CollaborationSelfAssessment);

        yield return (
            nameof(record.LearningAgility),
            record.LearningAgility);
    }

    private static void ValidateNumericValue(
        string column,
        float value,
        int rowNumber)
    {
        if (!float.IsFinite(value) ||
            value < RecommendationFeatureSchema.MinimumNumericValue ||
            value > RecommendationFeatureSchema.MaximumNumericValue)
        {
            throw new InvalidDataException(
                $"Row {rowNumber}, column '{column}' must contain a finite value between 1 and 5.");
        }
    }

    private static void ValidateCategoricalValue(
        string column,
        string value,
        int rowNumber)
    {
        ValidateRequiredText(value, column, rowNumber);

        var allowedValues =
            RecommendationFeatureSchema
                .AllowedCategoricalValuesByColumn[column];

        if (!allowedValues.Contains(value, StringComparer.Ordinal))
        {
            throw new InvalidDataException(
                $"Row {rowNumber}, column '{column}' contains unrecognized value '{value}'.");
        }
    }

    private static void ValidateRequiredText(
        string value,
        string column,
        int rowNumber)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidDataException(
                $"Row {rowNumber}, column '{column}' cannot be empty.");
        }
    }

    private static List<string> ReadScoreLabels(
        DataViewSchema schema)
    {
        var scoreColumn = schema[ScoreColumn];

        VBuffer<ReadOnlyMemory<char>> slotNames = default;

        scoreColumn.Annotations.GetValue(
            "SlotNames",
            ref slotNames);

        return slotNames
            .DenseValues()
            .Select(value => value.ToString())
            .ToList();
    }

    private static void ValidateScoreLabels(
        IReadOnlyCollection<string> scoreLabels)
    {
        var expectedLabels = RecommendationFeatureSchema
            .CareerLabelsByCode
            .Values
            .ToHashSet(StringComparer.Ordinal);

        if (scoreLabels.Count != expectedLabels.Count ||
            !expectedLabels.SetEquals(scoreLabels))
        {
            throw new InvalidOperationException(
                "The trained model score-vector mapping does not contain exactly the eight approved careers.");
        }
    }

    private static void ValidateMetrics(
        MulticlassClassificationMetrics metrics)
    {
        if (!double.IsFinite(metrics.MicroAccuracy) ||
            !double.IsFinite(metrics.MacroAccuracy) ||
            !double.IsFinite(metrics.LogLoss))
        {
            throw new InvalidOperationException(
                "Model evaluation produced a non-finite metric.");
        }
    }

    private static void ValidateRequiredPath(
        string path,
        string parameterName)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException(
                "A file path is required.",
                parameterName);
        }
    }

    private static void CreateParentDirectory(string path)
    {
        var directory = Path.GetDirectoryName(path);

        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }
}