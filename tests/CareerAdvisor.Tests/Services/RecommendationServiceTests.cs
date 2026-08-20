using CareerAdvisor.Core.Enums;
using CareerAdvisor.Core.Interfaces;
using CareerAdvisor.Core.Models;
using CareerAdvisor.Core.Recommendations;
using CareerAdvisor.Infrastructure.Data;
using CareerAdvisor.Infrastructure.MachineLearning;
using CareerAdvisor.Infrastructure.Repositories;
using CareerAdvisor.Infrastructure.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace CareerAdvisor.Tests.Services;

public sealed class RecommendationServiceTests
{
    [Fact]
    public async Task GenerateRecommendationsAsync_ReturnsAndPersistsTopThree()
    {
        await using var database =
            await TestDatabase.CreateAsync();

        var service = CreateService(
            database,
            CreateValidScores());

        var result =
            await service.GenerateRecommendationsAsync(
                database.StudentProfileId);

        Assert.Equal(3, result.Recommendations.Count);

        Assert.Equal(
            ["SD-001", "DA-002", "CS-003"],
            result.Recommendations
                .Select(recommendation =>
                    recommendation.Career!.Code)
                .ToArray());

        Assert.True(
            result.Recommendations[0].MatchScore >
            result.Recommendations[1].MatchScore);

        Assert.True(
            result.Recommendations[1].MatchScore >
            result.Recommendations[2].MatchScore);

        Assert.InRange(
            Math.Abs(
                result.Recommendations[0].MatchScore - 0.40),
            0,
            0.0001);

        Assert.InRange(
            Math.Abs(
                result.Recommendations[1].MatchScore - 0.25),
            0,
            0.0001);

        Assert.InRange(
            Math.Abs(
                result.Recommendations[2].MatchScore - 0.15),
            0,
            0.0001);

        Assert.All(
            result.Recommendations,
            recommendation =>
            {
                Assert.NotNull(recommendation.Career);

                Assert.Contains(
                    recommendation.Career!.Title,
                    recommendation.Reasoning,
                    StringComparison.Ordinal);

                Assert.Contains(
                    "/5",
                    recommendation.Reasoning,
                    StringComparison.Ordinal);

                Assert.Contains(
                    RecommendationDisclaimer.Text,
                    recommendation.Reasoning,
                    StringComparison.Ordinal);
            });

        await using var readContext =
            new CareerAdvisorDbContext(database.Options);

        var readRepository =
            new RecommendationRepository(readContext);

        var persisted =
            await readRepository.GetByIdAsync(result.Id);

        Assert.NotNull(persisted);
        Assert.Equal(3, persisted.Recommendations.Count);

        Assert.Equal(
            3,
            persisted.Recommendations
                .Select(recommendation =>
                    recommendation.CareerProfileId)
                .Distinct()
                .Count());

        Assert.All(
            persisted.Recommendations,
            recommendation =>
            {
                Assert.NotNull(recommendation.Career);

                Assert.False(
                    string.IsNullOrWhiteSpace(
                        recommendation.Reasoning));
            });
    }

    [Fact]
    public async Task GenerateRecommendationsAsync_RejectsMissingProfile()
    {
        await using var database =
            await TestDatabase.CreateAsync();

        var service = CreateService(
            database,
            CreateValidScores());

        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => service.GenerateRecommendationsAsync(
                    Guid.NewGuid()));

        Assert.Contains(
            "could not be found",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);

        Assert.Empty(
            await database.Context
                .RecommendationSessions
                .ToListAsync());
    }

    [Fact]
    public async Task GenerateRecommendationsAsync_RejectsIncompleteAssessment()
    {
        await using var database =
            await TestDatabase.CreateAsync(
                includeAllResponses: false);

        var service = CreateService(
            database,
            CreateValidScores());

        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => service.GenerateRecommendationsAsync(
                    database.StudentProfileId));

        Assert.Contains(
            "all 15 questions",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);

        Assert.Empty(
            await database.Context
                .RecommendationSessions
                .ToListAsync());
    }

    [Fact]
    public async Task GenerateRecommendationsAsync_RejectsDuplicateModelLabels()
    {
        await using var database =
            await TestDatabase.CreateAsync();

        var invalidScores = CreateValidScores()
            .ToList();

        invalidScores[1] = new CareerModelScore(
            invalidScores[0].CareerLabel,
            invalidScores[1].Score);

        var service = CreateService(
            database,
            invalidScores);

        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => service.GenerateRecommendationsAsync(
                    database.StudentProfileId));

        Assert.Contains(
            "exactly one score",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);

        Assert.Empty(
            await database.Context
                .RecommendationSessions
                .ToListAsync());
    }

    [Fact]
    public async Task GenerateRecommendationsAsync_UsesRealSavedModel()
    {
        await using var database =
            await TestDatabase.CreateAsync();

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

        var service = new RecommendationService(
            new StudentProfileRepository(database.Context),
            new AssessmentService(database.Context),
            database.CareerRepository,
            new RecommendationRepository(database.Context),
            new RecommendationInputBuilder(),
            predictor);

        var result =
            await service.GenerateRecommendationsAsync(
                database.StudentProfileId);

        Assert.Equal(3, result.Recommendations.Count);

        Assert.Equal(
            3,
            result.Recommendations
                .Select(recommendation =>
                    recommendation.CareerProfileId)
                .Distinct()
                .Count());

        Assert.All(
            result.Recommendations,
            recommendation =>
            {
                Assert.NotNull(recommendation.Career);

                Assert.True(
                    double.IsFinite(
                        recommendation.MatchScore));

                Assert.InRange(
                    recommendation.MatchScore,
                    0,
                    1);

                Assert.Contains(
                    RecommendationDisclaimer.Text,
                    recommendation.Reasoning,
                    StringComparison.Ordinal);
            });

        var scores = result.Recommendations
            .Select(recommendation =>
                recommendation.MatchScore)
            .ToArray();

        Assert.Equal(
            scores.OrderByDescending(score => score),
            scores);

        Assert.Equal(
            1,
            await database.Context
                .RecommendationSessions
                .CountAsync());

        Assert.Equal(
            3,
            await database.Context
                .CareerRecommendations
                .CountAsync());
    }

    private static RecommendationService CreateService(
        TestDatabase database,
        IReadOnlyList<CareerModelScore> scores)
    {
        return new RecommendationService(
            new StudentProfileRepository(database.Context),
            new AssessmentService(database.Context),
            database.CareerRepository,
            new RecommendationRepository(database.Context),
            new RecommendationInputBuilder(),
            new FixedCareerModelPredictor(scores));
    }

    private static IReadOnlyList<CareerModelScore>
        CreateValidScores()
    {
        return
        [
            new CareerModelScore(
                "AI/ML Engineer",
                0.05f),

            new CareerModelScore(
                "Cloud Engineer",
                0.04f),

            new CareerModelScore(
                "Cybersecurity Analyst",
                0.15f),

            new CareerModelScore(
                "Data Analyst",
                0.25f),

            new CareerModelScore(
                "Database Administrator",
                0.03f),

            new CareerModelScore(
                "Network Administrator",
                0.02f),

            new CareerModelScore(
                "Software Developer",
                0.40f),

            new CareerModelScore(
                "UI/UX Designer",
                0.06f)
        ];
    }

    private sealed class FixedCareerModelPredictor :
        ICareerModelPredictor
    {
        private readonly IReadOnlyList<CareerModelScore> _scores;

        public FixedCareerModelPredictor(
            IReadOnlyList<CareerModelScore> scores)
        {
            _scores = scores;
        }

        public IReadOnlyList<CareerModelScore> Predict(
            CareerTrainingInput input)
        {
            ArgumentNullException.ThrowIfNull(input);
            return _scores;
        }
    }

    private sealed class TestDatabase : IAsyncDisposable
    {
        private TestDatabase(
            SqliteConnection connection,
            DbContextOptions<CareerAdvisorDbContext> options,
            CareerAdvisorDbContext context,
            Guid studentProfileId,
            ICareerRepository careerRepository)
        {
            Connection = connection;
            Options = options;
            Context = context;
            StudentProfileId = studentProfileId;
            CareerRepository = careerRepository;
        }

        private SqliteConnection Connection { get; }

        public DbContextOptions<CareerAdvisorDbContext> Options
        {
            get;
        }

        public CareerAdvisorDbContext Context { get; }

        public Guid StudentProfileId { get; }

        public ICareerRepository CareerRepository { get; }

        public static async Task<TestDatabase> CreateAsync(
            bool includeAllResponses = true)
        {
            var connection =
                new SqliteConnection("Data Source=:memory:");

            await connection.OpenAsync();

            var options =
                new DbContextOptionsBuilder<CareerAdvisorDbContext>()
                    .UseSqlite(connection)
                    .Options;

            var context = new CareerAdvisorDbContext(options);
            await context.Database.MigrateAsync();

            var repositoryRoot = FindRepositoryRoot();

            var catalogPath = Path.Combine(
                repositoryRoot,
                "data",
                "career-catalog.json");

            var careerRepository =
                new JsonCareerRepository(catalogPath);

            var careers = (
                await careerRepository.GetAllAsync())
                .ToList();

            var profile = new StudentProfile
            {
                Id = Guid.NewGuid(),
                Name = "Ama Mensah",
                Programme = "Computer Science",
                AcademicLevel = AcademicLevel.Undergraduate,
                Interests =
                [
                    "Technology",
                    "Data Analysis"
                ],
                Skills =
                [
                    new StudentSkill
                    {
                        SkillName = "C# Programming",
                        Proficiency =
                            SkillProficiency.Advanced
                    },
                    new StudentSkill
                    {
                        SkillName = "Data Analysis",
                        Proficiency =
                            SkillProficiency.Intermediate
                    }
                ]
            };

            var questions =
                AssessmentQuestionBank.GetAllQuestions();

            var responses = questions
                .Select(question =>
                    new AssessmentResponse
                    {
                        QuestionId = question.Id,
                        OptionId = question.Options[0].Id
                    })
                .ToList();

            if (!includeAllResponses)
            {
                responses.RemoveAt(responses.Count - 1);
            }

            var assessment = new AssessmentSession
            {
                Id = Guid.NewGuid(),
                StudentProfileId = profile.Id,
                Status = "Completed",
                CompletedAt = DateTime.UtcNow,
                Responses = responses
            };

            context.StudentProfiles.Add(profile);
            context.CareerProfiles.AddRange(careers);
            context.AssessmentSessions.Add(assessment);

            await context.SaveChangesAsync();

            return new TestDatabase(
                connection,
                options,
                context,
                profile.Id,
                careerRepository);
        }

        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            await Connection.DisposeAsync();
        }
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