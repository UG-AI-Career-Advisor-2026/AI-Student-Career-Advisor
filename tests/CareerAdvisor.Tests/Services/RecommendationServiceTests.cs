using CareerAdvisor.Core.Enums;
using CareerAdvisor.Core.Interfaces;
using CareerAdvisor.Core.Models;
using CareerAdvisor.Core.Recommendations;
using CareerAdvisor.Core.Validators;
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
    public async Task GenerateWithOutcome_MismatchedProfileStopsBeforeDownstreamAccess()
    {
        var requestedId = Guid.NewGuid();
        var assessment = new DownstreamAssessmentService();
        var careers = new DownstreamCareerRepository();
        var recommendations = new DownstreamRecommendationRepository();
        var predictor = new DownstreamPredictor();
        var service = new RecommendationService(
            new MismatchedProfileRepository(requestedId),
            assessment,
            careers,
            recommendations,
            new RecommendationInputBuilder(),
            predictor);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.GenerateRecommendationsWithOutcomeAsync(requestedId));

        Assert.Equal(
            "The requested student profile could not be found.",
            exception.Message);
        Assert.Equal(0, assessment.Calls);
        Assert.Equal(0, careers.Calls);
        Assert.Equal(0, recommendations.Calls);
        Assert.Equal(0, predictor.Calls);
    }

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
    public async Task GenerateWithOutcome_IdenticalLatestSessionIsReused()
    {
        await using var database = await TestDatabase.CreateAsync();
        var service = CreateService(database, CreateValidScores());

        var first = await service.GenerateRecommendationsWithOutcomeAsync(
            database.StudentProfileId);
        var second = await service.GenerateRecommendationsWithOutcomeAsync(
            database.StudentProfileId);

        Assert.True(first.WasNewlyPersisted);
        Assert.False(second.WasNewlyPersisted);
        Assert.Equal(first.Session.Id, second.Session.Id);
        Assert.Equal(first.Session.GeneratedAt, second.Session.GeneratedAt);
        Assert.Equal(
            first.Session.Recommendations.Select(RecommendationValue),
            second.Session.Recommendations.Select(RecommendationValue));
        Assert.Equal(
            1,
            await database.Context.RecommendationSessions.CountAsync());
        Assert.Equal(
            3,
            await database.Context.CareerRecommendations.CountAsync());
    }

    [Fact]
    public async Task GenerateWithOutcome_ChangedScorePersistsNewSession()
    {
        await using var database = await TestDatabase.CreateAsync();
        var firstService = CreateService(database, CreateValidScores());
        var first = await firstService.GenerateRecommendationsWithOutcomeAsync(
            database.StudentProfileId);

        var changedScores = CreateValidScores().ToList();
        changedScores[6] = changedScores[6] with { Score = 0.41f };
        var secondService = CreateService(database, changedScores);
        var second = await secondService.GenerateRecommendationsWithOutcomeAsync(
            database.StudentProfileId);

        Assert.True(first.WasNewlyPersisted);
        Assert.True(second.WasNewlyPersisted);
        Assert.NotEqual(first.Session.Id, second.Session.Id);
        Assert.Equal(
            2,
            await database.Context.RecommendationSessions.CountAsync());
        Assert.Equal(
            6,
            await database.Context.CareerRecommendations.CountAsync());
    }

    [Fact]
    public async Task GenerateWithOutcome_ChangedReasoningPersistsNewSession()
    {
        await using var database = await TestDatabase.CreateAsync();
        var service = CreateService(database, CreateValidScores());
        var first = await service.GenerateRecommendationsWithOutcomeAsync(
            database.StudentProfileId);
        var storedRecommendation = await database.Context.CareerRecommendations
            .SingleAsync(recommendation =>
                recommendation.RecommendationSessionId == first.Session.Id &&
                recommendation.CareerProfileId ==
                first.Session.Recommendations[0].CareerProfileId);
        storedRecommendation.Reasoning += " Updated saved explanation.";
        await database.Context.SaveChangesAsync();

        var second = await service.GenerateRecommendationsWithOutcomeAsync(
            database.StudentProfileId);

        Assert.True(second.WasNewlyPersisted);
        Assert.NotEqual(first.Session.Id, second.Session.Id);
        Assert.Equal(
            2,
            await database.Context.RecommendationSessions.CountAsync());
    }

    [Fact]
    public async Task GenerateWithOutcome_ChangedCareerPersistsNewSession()
    {
        await using var database = await TestDatabase.CreateAsync();
        var first = await CreateService(database, CreateValidScores())
            .GenerateRecommendationsWithOutcomeAsync(database.StudentProfileId);
        var changedScores = CreateValidScores().ToList();
        changedScores[7] = changedScores[7] with { Score = 0.30f };

        var second = await CreateService(database, changedScores)
            .GenerateRecommendationsWithOutcomeAsync(database.StudentProfileId);

        Assert.True(second.WasNewlyPersisted);
        Assert.NotEqual(first.Session.Id, second.Session.Id);
        Assert.NotEqual(
            first.Session.Recommendations
                .Select(item => item.CareerProfileId)
                .Order(),
            second.Session.Recommendations
                .Select(item => item.CareerProfileId)
                .Order());
        Assert.Equal(2, await database.Context.RecommendationSessions.CountAsync());
    }

    [Fact]
    public async Task GenerateWithOutcome_EqualScoresReuseDespiteReversedPersistedOrder()
    {
        await using var database = await TestDatabase.CreateAsync();
        var equalScores = CreateValidScores().ToList();
        equalScores[3] = equalScores[3] with { Score = 0.40f };
        var first = await CreateService(database, equalScores)
            .GenerateRecommendationsWithOutcomeAsync(database.StudentProfileId);
        var reversedRepository = new ReversingRecommendationRepository(
            new RecommendationRepository(database.Context));
        var secondService = new RecommendationService(
            new StudentProfileRepository(database.Context),
            new AssessmentService(database.Context),
            database.CareerRepository,
            reversedRepository,
            new RecommendationInputBuilder(),
            new FixedCareerModelPredictor(equalScores));

        var second = await secondService
            .GenerateRecommendationsWithOutcomeAsync(database.StudentProfileId);

        Assert.False(second.WasNewlyPersisted);
        Assert.Equal(first.Session.Id, second.Session.Id);
        Assert.Equal(
            first.Session.Recommendations.Select(item => item.CareerProfileId),
            second.Session.Recommendations.Select(item => item.CareerProfileId));
        Assert.Equal(1, await database.Context.RecommendationSessions.CountAsync());
    }

    [Theory]
    [InlineData(true, "")]
    [InlineData(true, "   ")]
    [InlineData(false, "")]
    [InlineData(false, "   ")]
    public async Task GenerateWithOutcome_BlankCareerDetailsRejectWithoutWrite(
        bool changeTitle,
        string invalidValue)
    {
        await using var database = await TestDatabase.CreateAsync();
        var service = CreateService(database, CreateValidScores());
        var first = await service.GenerateRecommendationsWithOutcomeAsync(
            database.StudentProfileId);
        var careerId = first.Session.Recommendations[0].CareerProfileId;
        var career = await database.Context.CareerProfiles
            .SingleAsync(item => item.Id == careerId);
        if (changeTitle)
        {
            career.Title = invalidValue;
        }
        else
        {
            career.Description = invalidValue;
        }
        await database.Context.SaveChangesAsync();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.GenerateRecommendationsWithOutcomeAsync(
                database.StudentProfileId));

        Assert.Equal(
            "The latest saved recommendation session is invalid. " +
            "No new recommendations were saved.",
            exception.Message);
        Assert.Equal(1, await database.Context.RecommendationSessions.CountAsync());
        Assert.Equal(3, await database.Context.CareerRecommendations.CountAsync());
    }

    [Fact]
    public async Task GenerateWithOutcome_MalformedNewestSessionRejectsWithoutWrite()
    {
        await using var database = await TestDatabase.CreateAsync();
        var service = CreateService(database, CreateValidScores());
        var first = await service.GenerateRecommendationsWithOutcomeAsync(
            database.StudentProfileId);

        var storedRecommendation = await database.Context.CareerRecommendations
            .SingleAsync(recommendation =>
                recommendation.RecommendationSessionId == first.Session.Id &&
                recommendation.CareerProfileId ==
                first.Session.Recommendations[0].CareerProfileId);
        storedRecommendation.Reasoning = " ";
        await database.Context.SaveChangesAsync();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.GenerateRecommendationsWithOutcomeAsync(
                database.StudentProfileId));

        Assert.Equal(
            "The latest saved recommendation session is invalid. " +
            "No new recommendations were saved.",
            exception.Message);
        Assert.Equal(
            1,
            await database.Context.RecommendationSessions.CountAsync());
        Assert.Equal(
            3,
            await database.Context.CareerRecommendations.CountAsync());
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

    private static object RecommendationValue(
        CareerRecommendation recommendation)
    {
        return new
        {
            recommendation.CareerProfileId,
            recommendation.MatchScore,
            recommendation.Reasoning
        };
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

    private sealed class ReversingRecommendationRepository(
        IRecommendationRepository inner) : IRecommendationRepository
    {
        public Task<RecommendationSession?> GetByIdAsync(Guid id) =>
            inner.GetByIdAsync(id);

        public Task<IEnumerable<RecommendationSession>> GetAllAsync() =>
            inner.GetAllAsync();

        public async Task<IEnumerable<RecommendationSession>>
            GetByStudentProfileIdAsync(Guid studentProfileId)
        {
            var sessions = (await inner.GetByStudentProfileIdAsync(
                    studentProfileId))
                .ToList();

            foreach (var session in sessions)
            {
                session.Recommendations.Reverse();
            }

            return sessions;
        }

        public Task AddAsync(RecommendationSession entity) =>
            inner.AddAsync(entity);

        public Task UpdateAsync(RecommendationSession entity) =>
            inner.UpdateAsync(entity);

        public Task DeleteAsync(Guid id) => inner.DeleteAsync(id);
    }

    private sealed class MismatchedProfileRepository(Guid requestedId)
        : IStudentProfileRepository
    {
        public Task<StudentProfile?> GetByIdAsync(Guid id) =>
            Task.FromResult<StudentProfile?>(new StudentProfile
            {
                Id = id == requestedId ? Guid.NewGuid() : requestedId
            });

        public Task<IEnumerable<StudentProfile>> GetAllAsync() =>
            throw new InvalidOperationException("Unexpected profile access.");

        public Task AddAsync(StudentProfile entity) =>
            throw new InvalidOperationException("Unexpected profile write.");

        public Task UpdateAsync(StudentProfile entity) =>
            throw new InvalidOperationException("Unexpected profile write.");

        public Task DeleteAsync(Guid id) =>
            throw new InvalidOperationException("Unexpected profile write.");
    }

    private sealed class DownstreamAssessmentService : IAssessmentService
    {
        public int Calls { get; private set; }

        public Task<AssessmentSession?> GetLatestCompletedAssessmentAsync(
            Guid studentProfileId)
        {
            Calls++;
            throw new InvalidOperationException("Assessment must not be read.");
        }

        public AssessmentSession CreateAssessmentSession(Guid studentProfileId) =>
            throw new NotSupportedException();
        public Guid? GetAvailableStudentProfileId() => throw new NotSupportedException();
        public List<AssessmentQuestion> GetAllQuestions() => throw new NotSupportedException();
        public AssessmentQuestion? GetQuestion(Guid questionId) => throw new NotSupportedException();
        public AssessmentSession? GetAssessmentSession(Guid sessionId) => throw new NotSupportedException();
        public ValidationResult SubmitResponse(AssessmentSession session, Guid questionId, Guid optionId) =>
            throw new NotSupportedException();
        public ValidationResult CompleteAssessmentSession(AssessmentSession session) =>
            throw new NotSupportedException();
        public List<AssessmentResponse> GetSessionResponses(Guid sessionId) =>
            throw new NotSupportedException();
    }

    private sealed class DownstreamCareerRepository : ICareerRepository
    {
        public int Calls { get; private set; }
        private T Unexpected<T>()
        {
            Calls++;
            throw new InvalidOperationException("Career repository must not be read.");
        }
        public Task<CareerProfile?> GetByCodeAsync(string code) => Unexpected<Task<CareerProfile?>>();
        public Task<CareerProfile?> GetByIdAsync(Guid id) => Unexpected<Task<CareerProfile?>>();
        public Task<IEnumerable<CareerProfile>> GetAllAsync() => Unexpected<Task<IEnumerable<CareerProfile>>>();
        public Task AddAsync(CareerProfile entity) => Unexpected<Task>();
        public Task UpdateAsync(CareerProfile entity) => Unexpected<Task>();
        public Task DeleteAsync(Guid id) => Unexpected<Task>();
    }

    private sealed class DownstreamRecommendationRepository
        : IRecommendationRepository
    {
        public int Calls { get; private set; }
        private T Unexpected<T>()
        {
            Calls++;
            throw new InvalidOperationException("Recommendation repository must not be accessed.");
        }
        public Task<RecommendationSession?> GetByIdAsync(Guid id) => Unexpected<Task<RecommendationSession?>>();
        public Task<IEnumerable<RecommendationSession>> GetAllAsync() => Unexpected<Task<IEnumerable<RecommendationSession>>>();
        public Task<IEnumerable<RecommendationSession>> GetByStudentProfileIdAsync(Guid studentProfileId) => Unexpected<Task<IEnumerable<RecommendationSession>>>();
        public Task AddAsync(RecommendationSession entity) => Unexpected<Task>();
        public Task UpdateAsync(RecommendationSession entity) => Unexpected<Task>();
        public Task DeleteAsync(Guid id) => Unexpected<Task>();
    }

    private sealed class DownstreamPredictor : ICareerModelPredictor
    {
        public int Calls { get; private set; }
        public IReadOnlyList<CareerModelScore> Predict(CareerTrainingInput input)
        {
            Calls++;
            throw new InvalidOperationException("Predictor must not run.");
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
