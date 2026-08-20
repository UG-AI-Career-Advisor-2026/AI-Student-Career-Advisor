using System.Text.Json;
using CareerAdvisor.Core.Recommendations;
using CareerAdvisor.Infrastructure.MachineLearning;

namespace CareerAdvisor.Tests.MachineLearning;

public sealed class CareerModelPredictorTests
{
    [Fact]
    public void Predict_ReturnsEightFiniteCorrectlyLabelledScores()
    {
        var repositoryRoot = FindRepositoryRoot();

        var modelPath = Path.Combine(
            repositoryRoot,
            "data",
            "models",
            "career-recommendation-model.zip");

        var metadataPath = Path.Combine(
            repositoryRoot,
            "data",
            "models",
            "career-recommendation-model.metadata.json");

        using var predictor = new CareerModelPredictor(
            modelPath,
            metadataPath);

        var scores = predictor.Predict(CreateValidInput());

        Assert.Equal(8, scores.Count);

        Assert.Equal(
            8,
            scores
                .Select(score => score.CareerLabel)
                .Distinct(StringComparer.Ordinal)
                .Count());

        Assert.All(
            scores,
            score =>
            {
                Assert.False(
                    string.IsNullOrWhiteSpace(
                        score.CareerLabel));

                Assert.True(
                    float.IsFinite(score.Score),
                    $"Expected a finite score for " +
                    $"{score.CareerLabel}, but received " +
                    $"{score.Score}.");
            });

        var expectedLabels = RecommendationFeatureSchema
            .CareerLabelsByCode
            .Values
            .ToHashSet(StringComparer.Ordinal);

        Assert.True(
            expectedLabels.SetEquals(
                scores.Select(score =>
                    score.CareerLabel)));
    }

    [Fact]
    public void Predict_PreservesMetadataScoreOrder()
    {
        var repositoryRoot = FindRepositoryRoot();

        var modelPath = Path.Combine(
            repositoryRoot,
            "data",
            "models",
            "career-recommendation-model.zip");

        var metadataPath = Path.Combine(
            repositoryRoot,
            "data",
            "models",
            "career-recommendation-model.metadata.json");

        var metadata = JsonSerializer.Deserialize<CareerModelMetadata>(
            File.ReadAllText(metadataPath));

        Assert.NotNull(metadata);

        using var predictor = new CareerModelPredictor(
            modelPath,
            metadataPath);

        var scores = predictor.Predict(CreateValidInput());

        Assert.Equal(
            metadata.ScoreLabels,
            scores.Select(score =>
                score.CareerLabel));
    }

    [Fact]
    public void Constructor_RejectsMissingModel()
    {
        var repositoryRoot = FindRepositoryRoot();

        var metadataPath = Path.Combine(
            repositoryRoot,
            "data",
            "models",
            "career-recommendation-model.metadata.json");

        var missingModelPath = Path.Combine(
            Path.GetTempPath(),
            $"missing-model-{Guid.NewGuid()}.zip");

        var exception = Assert.Throws<FileNotFoundException>(
            () => new CareerModelPredictor(
                missingModelPath,
                metadataPath));

        Assert.Contains(
            "trained recommendation model",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Constructor_RejectsIncompleteMetadataLabels()
    {
        var repositoryRoot = FindRepositoryRoot();

        var modelPath = Path.Combine(
            repositoryRoot,
            "data",
            "models",
            "career-recommendation-model.zip");

        var temporaryMetadataPath = Path.Combine(
            Path.GetTempPath(),
            $"careeriq-invalid-metadata-{Guid.NewGuid()}.json");

        try
        {
            var invalidMetadata = new CareerModelMetadata
            {
                RandomSeed = CareerModelTrainer.DefaultSeed,
                ScoreLabels =
                [
                    "Software Developer"
                ]
            };

            File.WriteAllText(
                temporaryMetadataPath,
                JsonSerializer.Serialize(invalidMetadata));

            var exception = Assert.Throws<InvalidDataException>(
                () => new CareerModelPredictor(
                    modelPath,
                    temporaryMetadataPath));

            Assert.Contains(
                "eight approved career labels",
                exception.Message,
                StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            if (File.Exists(temporaryMetadataPath))
            {
                File.Delete(temporaryMetadataPath);
            }
        }
    }

    private static CareerTrainingInput CreateValidInput()
    {
        return new CareerTrainingInput
        {
            AcademicBackground = "ComputerScience",
            AcademicLevel = "Undergraduate",
            ProgrammingSkill = 5,
            DataSkill = 3,
            CybersecuritySkill = 2,
            CloudSkill = 3,
            NetworkingSkill = 2,
            DatabaseSkill = 3,
            DesignSkill = 2,
            AISkill = 4,
            TechnologyInterest = 5,
            DataInterest = 4,
            DesignInterest = 2,
            LeadershipInterest = 3,
            SocialImpactInterest = 2,
            ProgrammingSelfAssessment = 5,
            CommunicationSelfAssessment = 4,
            ProblemSolvingSelfAssessment = 5,
            CollaborationSelfAssessment = 4,
            LearningAgility = 5,
            PreferredEnvironment = "RemoteHybrid",
            PreferredPace = "Fast",
            StabilityPreference = "Growth",
            CompensationPreference = "SalaryEquity",
            IndustryPreference = "Technology"
        };
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(
            AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(
                    Path.Combine(
                        directory.FullName,
                        "CareerAdvisor.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException(
            "Could not locate the repository root.");
    }
}