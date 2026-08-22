using CareerAdvisor.Core.Enums;
using CareerAdvisor.Core.Interfaces;
using CareerAdvisor.Core.Models;
using CareerAdvisor.Core.Recommendations;
using CareerAdvisor.Infrastructure.Data;
using CareerAdvisor.Infrastructure.MachineLearning;
using CareerAdvisor.Infrastructure.Repositories;
using CareerAdvisor.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace CareerAdvisor.Tests.Integration;

public sealed class Sprint3IntegrationTests
{
    [Fact]
    public async Task CompleteSprint3Journey_UsesRealModelAndPersistsTopThree()
    {
        await using var database =
            await TestDatabase.CreateAsync();

        var profile = await SaveProfileAsync(database);

        var completedAssessment =
            await CompleteAssessmentAsync(
                database,
                profile.Id);

        RecommendationSession generatedSession;

        await using (var generationContext =
                     database.CreateContext())
        {
            using var predictor = CreateRealModelPredictor();

            var service = CreateRecommendationService(
                generationContext,
                predictor);

            generatedSession =
                await service.GenerateRecommendationsAsync(
                    profile.Id);
        }

        Assert.NotEqual(Guid.Empty, completedAssessment.Id);
        Assert.Equal("Completed", completedAssessment.Status);
        Assert.NotNull(completedAssessment.CompletedAt);

        Assert.Equal(
            3,
            generatedSession.Recommendations.Count);

        Assert.Equal(
            3,
            generatedSession.Recommendations
                .Select(recommendation =>
                    recommendation.CareerProfileId)
                .Distinct()
                .Count());

        Assert.Equal(
            3,
            generatedSession.Recommendations
                .Select(recommendation =>
                    recommendation.Career!.Code)
                .Distinct(StringComparer.Ordinal)
                .Count());

        Assert.All(
            generatedSession.Recommendations,
            recommendation =>
            {
                Assert.NotNull(recommendation.Career);

                var career = recommendation.Career!;

                Assert.True(
                    RecommendationFeatureSchema
                        .CareerLabelsByCode
                        .TryGetValue(
                            career.Code,
                            out var expectedLabel));

                Assert.Equal(expectedLabel, career.Title);

                Assert.True(
                    double.IsFinite(
                        recommendation.MatchScore));

                Assert.InRange(
                    recommendation.MatchScore,
                    0,
                    1);

                Assert.False(
                    string.IsNullOrWhiteSpace(
                        recommendation.Reasoning));

                Assert.Contains(
                    career.Title,
                    recommendation.Reasoning,
                    StringComparison.Ordinal);

                Assert.Contains(
                    RecommendationDisclaimer.Text,
                    recommendation.Reasoning,
                    StringComparison.Ordinal);
            });

        var generatedScores = generatedSession
            .Recommendations
            .Select(recommendation =>
                recommendation.MatchScore)
            .ToArray();

        Assert.Equal(
            generatedScores
                .OrderByDescending(score => score)
                .ToArray(),
            generatedScores);

        await using var reopenContext =
            database.CreateContext();

        var repository =
            new RecommendationRepository(reopenContext);

        var reopenedSession =
            await repository.GetByIdAsync(
                generatedSession.Id);

        Assert.NotNull(reopenedSession);
        Assert.Equal(profile.Id, reopenedSession.StudentProfileId);
        Assert.Equal(3, reopenedSession.Recommendations.Count);

        Assert.Equal(
            generatedSession.Recommendations
                .Select(recommendation =>
                    recommendation.CareerProfileId)
                .Order()
                .ToArray(),
            reopenedSession.Recommendations
                .Select(recommendation =>
                    recommendation.CareerProfileId)
                .Order()
                .ToArray());

        Assert.All(
            reopenedSession.Recommendations,
            recommendation =>
            {
                Assert.NotNull(recommendation.Career);

                Assert.True(
                    double.IsFinite(
                        recommendation.MatchScore));

                Assert.False(
                    string.IsNullOrWhiteSpace(
                        recommendation.Reasoning));
            });

        var history =
            (await repository
                .GetByStudentProfileIdAsync(profile.Id))
            .ToList();

        var savedHistorySession = Assert.Single(history);

        Assert.Equal(
            generatedSession.Id,
            savedHistorySession.Id);

        Assert.Equal(
            3,
            savedHistorySession.Recommendations.Count);
    }

    [Fact]
    public async Task GenerateRecommendations_RejectsMissingProfile()
    {
        await using var database =
            await TestDatabase.CreateAsync();

        await using var context = database.CreateContext();

        var service = CreateRecommendationService(
            context,
            new UnexpectedModelPredictor());

        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => service.GenerateRecommendationsAsync(
                    Guid.NewGuid()));

        Assert.Contains(
            "could not be found",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);

        Assert.Empty(
            await context.RecommendationSessions
                .ToListAsync());

        Assert.Empty(
            await context.CareerRecommendations
                .ToListAsync());
    }

    [Fact]
    public async Task GenerateRecommendations_RejectsIncompleteAssessment()
    {
        await using var database =
            await TestDatabase.CreateAsync();

        var profile = await SaveProfileAsync(database);

        Guid assessmentSessionId;

        await using (var assessmentContext =
                     database.CreateContext())
        {
            var assessmentService =
                new AssessmentService(assessmentContext);

            var session =
                assessmentService.CreateAssessmentSession(
                    profile.Id);

            var firstRequiredQuestion =
                assessmentService.GetAllQuestions()
                    .First(question => question.IsRequired);

            var responseResult =
                assessmentService.SubmitResponse(
                    session,
                    firstRequiredQuestion.Id,
                    firstRequiredQuestion.Options[0].Id);

            Assert.True(
                responseResult.IsValid,
                string.Join(
                    " ",
                    responseResult.Errors));

            assessmentSessionId = session.Id;
        }

        await using var generationContext =
            database.CreateContext();

        var service = CreateRecommendationService(
            generationContext,
            new UnexpectedModelPredictor());

        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => service.GenerateRecommendationsAsync(
                    profile.Id));

        Assert.Contains(
            "completed career assessment",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);

        var savedAssessment =
            await generationContext.AssessmentSessions
                .AsNoTracking()
                .Include(session => session.Responses)
                .SingleAsync(session =>
                    session.Id == assessmentSessionId);

        Assert.Equal("InProgress", savedAssessment.Status);
        Assert.Null(savedAssessment.CompletedAt);
        Assert.Single(savedAssessment.Responses);

        Assert.Empty(
            await generationContext
                .RecommendationSessions
                .ToListAsync());

        Assert.Empty(
            await generationContext
                .CareerRecommendations
                .ToListAsync());
    }

    [Fact]
    public async Task ModelFailure_DoesNotPersistFallbackRecommendations()
    {
        await using var database =
            await TestDatabase.CreateAsync();

        var profile = await SaveProfileAsync(database);

        await CompleteAssessmentAsync(
            database,
            profile.Id);

        await using var generationContext =
            database.CreateContext();

        var service = CreateRecommendationService(
            generationContext,
            new FailingModelPredictor());

        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => service.GenerateRecommendationsAsync(
                    profile.Id));

        Assert.Contains(
            "simulated model failure",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);

        Assert.Empty(
            await generationContext
                .RecommendationSessions
                .ToListAsync());

        Assert.Empty(
            await generationContext
                .CareerRecommendations
                .ToListAsync());
    }

    private static async Task<StudentProfile> SaveProfileAsync(
        TestDatabase database)
    {
        var profile = CreateProfile();

        await using var context = database.CreateContext();

        var repository =
            new StudentProfileRepository(context);

        await repository.AddAsync(profile);

        return profile;
    }

    private static async Task<AssessmentSession>
        CompleteAssessmentAsync(
            TestDatabase database,
            Guid studentProfileId)
    {
        await using var context = database.CreateContext();

        var service = new AssessmentService(context);

        var session =
            service.CreateAssessmentSession(
                studentProfileId);

        var requiredQuestions =
            service.GetAllQuestions()
                .Where(question => question.IsRequired)
                .ToList();

        Assert.Equal(15, requiredQuestions.Count);

        foreach (var question in requiredQuestions)
        {
            var responseResult =
                service.SubmitResponse(
                    session,
                    question.Id,
                    question.Options[0].Id);

            Assert.True(
                responseResult.IsValid,
                string.Join(
                    " ",
                    responseResult.Errors));
        }

        var completionResult =
            service.CompleteAssessmentSession(session);

        Assert.True(
            completionResult.IsValid,
            string.Join(
                " ",
                completionResult.Errors));

        Assert.Equal("Completed", session.Status);
        Assert.NotNull(session.CompletedAt);

        return session;
    }

    private static RecommendationService
        CreateRecommendationService(
            CareerAdvisorDbContext context,
            ICareerModelPredictor predictor)
    {
        return new RecommendationService(
            new StudentProfileRepository(context),
            new AssessmentService(context),
            new JsonCareerRepository(GetCatalogPath()),
            new RecommendationRepository(context),
            new RecommendationInputBuilder(),
            predictor);
    }

    private static CareerModelPredictor
        CreateRealModelPredictor()
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

        return new CareerModelPredictor(
            modelPath,
            metadataPath);
    }

    private static StudentProfile CreateProfile()
    {
        return new StudentProfile
        {
            Name = "Sprint 3 Integration Student",
            Programme = "Computer Science",
            AcademicLevel = AcademicLevel.Undergraduate,
            Interests =
            [
                "Artificial Intelligence",
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
                },
                new StudentSkill
                {
                    SkillName = "Machine Learning",
                    Proficiency =
                        SkillProficiency.Beginner
                }
            ]
        };
    }

    private static string GetCatalogPath()
    {
        return Path.Combine(
            FindRepositoryRoot(),
            "data",
            "career-catalog.json");
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

    private sealed class UnexpectedModelPredictor :
        ICareerModelPredictor
    {
        public IReadOnlyList<CareerModelScore> Predict(
            CareerTrainingInput input)
        {
            throw new InvalidOperationException(
                "The model must not run when prerequisites are missing.");
        }
    }

    private sealed class FailingModelPredictor :
        ICareerModelPredictor
    {
        public IReadOnlyList<CareerModelScore> Predict(
            CareerTrainingInput input)
        {
            ArgumentNullException.ThrowIfNull(input);

            throw new InvalidOperationException(
                "Simulated model failure.");
        }
    }

    private sealed class TestDatabase : IAsyncDisposable
    {
        private TestDatabase(
            string databasePath,
            DbContextOptions<CareerAdvisorDbContext> options)
        {
            DatabasePath = databasePath;
            Options = options;
        }

        private string DatabasePath { get; }

        private DbContextOptions<CareerAdvisorDbContext> Options
        {
            get;
        }

        public CareerAdvisorDbContext CreateContext()
        {
            return new CareerAdvisorDbContext(Options);
        }

        public static async Task<TestDatabase> CreateAsync()
        {
            var databasePath = Path.Combine(
                Path.GetTempPath(),
                $"careeriq-sprint3-{Guid.NewGuid():N}.db");

            var options =
                new DbContextOptionsBuilder<CareerAdvisorDbContext>()
                    .UseSqlite(
                        $"Data Source={databasePath}")
                    .Options;

            await using var context =
                new CareerAdvisorDbContext(options);

            await context.Database.MigrateAsync();

            var careerRepository =
                new JsonCareerRepository(GetCatalogPath());

            var careers =
                (await careerRepository.GetAllAsync())
                .ToList();

            context.CareerProfiles.AddRange(careers);
            await context.SaveChangesAsync();

            return new TestDatabase(
                databasePath,
                options);
        }

        public ValueTask DisposeAsync()
        {
            DeleteIfPresent(DatabasePath);
            DeleteIfPresent($"{DatabasePath}-shm");
            DeleteIfPresent($"{DatabasePath}-wal");

            return ValueTask.CompletedTask;
        }

        private static void DeleteIfPresent(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
}