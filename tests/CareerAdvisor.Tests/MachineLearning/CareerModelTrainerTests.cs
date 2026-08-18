using System.Text.Json;
using CareerAdvisor.Core.Recommendations;
using CareerAdvisor.Infrastructure.MachineLearning;
using Microsoft.ML;

namespace CareerAdvisor.Tests.MachineLearning;

public sealed class CareerModelTrainerTests
{
    [Fact]
    public void Train_SavesLoadableModelWithEightFiniteScores()
    {
        var repositoryRoot = FindRepositoryRoot();
        var datasetPath = Path.Combine(
            repositoryRoot,
            "data",
            "training",
            "sample-career-training-data.csv");

        var outputDirectory = Path.Combine(
            Path.GetTempPath(),
            $"careeriq-model-test-{Guid.NewGuid()}");

        Directory.CreateDirectory(outputDirectory);

        try
        {
            var modelPath = Path.Combine(
                outputDirectory,
                "career-recommendation-model.zip");

            var metadataPath = Path.Combine(
                outputDirectory,
                "career-recommendation-model.metadata.json");

            var trainer = new CareerModelTrainer();

            var result = trainer.Train(
                datasetPath,
                modelPath,
                metadataPath);

            Assert.True(File.Exists(result.ModelPath));
            Assert.True(File.Exists(result.MetadataPath));

            Assert.Equal(80, result.Metadata.TotalRecordCount);
            Assert.Equal(CareerModelTrainer.DefaultSeed, result.Metadata.RandomSeed);
            Assert.Equal(8, result.Metadata.ScoreLabels.Count);

            var expectedLabels = RecommendationFeatureSchema
                .CareerLabelsByCode
                .Values
                .OrderBy(label => label, StringComparer.Ordinal)
                .ToArray();

            Assert.Equal(expectedLabels, result.Metadata.ScoreLabels);

            var mlContext = new MLContext(
                seed: CareerModelTrainer.DefaultSeed);

            var model = mlContext.Model.Load(
                result.ModelPath,
                out _);

            var predictionEngine = mlContext.Model
                .CreatePredictionEngine<
                    CareerTrainingInput,
                    CareerPredictionOutput>(model);

            var prediction = predictionEngine.Predict(
                CreateValidInput());

            Assert.NotNull(prediction.Scores);
            Assert.Equal(8, prediction.Scores.Length);

            Assert.All(
                prediction.Scores,
                score => Assert.True(
                    float.IsFinite(score),
                    $"Expected a finite score but received {score}."));

            Assert.Contains(
                prediction.PredictedCareerLabel,
                result.Metadata.ScoreLabels);
        }
        finally
        {
            Directory.Delete(
                outputDirectory,
                recursive: true);
        }
    }

    [Fact]
    public void Train_WritesMetadataThatCanBeReloaded()
    {
        var repositoryRoot = FindRepositoryRoot();
        var datasetPath = Path.Combine(
            repositoryRoot,
            "data",
            "training",
            "sample-career-training-data.csv");

        var outputDirectory = Path.Combine(
            Path.GetTempPath(),
            $"careeriq-metadata-test-{Guid.NewGuid()}");

        Directory.CreateDirectory(outputDirectory);

        try
        {
            var modelPath = Path.Combine(
                outputDirectory,
                "model.zip");

            var metadataPath = Path.Combine(
                outputDirectory,
                "model.metadata.json");

            var result = new CareerModelTrainer().Train(
                datasetPath,
                modelPath,
                metadataPath);

            var json = File.ReadAllText(metadataPath);

            var savedMetadata =
                JsonSerializer.Deserialize<CareerModelMetadata>(json);

            Assert.NotNull(savedMetadata);
            Assert.Equal(
                result.Metadata.DatasetVersion,
                savedMetadata.DatasetVersion);
            Assert.Equal(
                result.Metadata.Trainer,
                savedMetadata.Trainer);
            Assert.Equal(
                CareerModelTrainer.DefaultSeed,
                savedMetadata.RandomSeed);
            Assert.Equal(
                result.Metadata.ScoreLabels,
                savedMetadata.ScoreLabels);
            Assert.Equal(8, savedMetadata.ScoreLabels.Count);
            Assert.True(double.IsFinite(savedMetadata.MicroAccuracy));
            Assert.True(double.IsFinite(savedMetadata.MacroAccuracy));
            Assert.True(double.IsFinite(savedMetadata.LogLoss));
        }
        finally
        {
            Directory.Delete(
                outputDirectory,
                recursive: true);
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