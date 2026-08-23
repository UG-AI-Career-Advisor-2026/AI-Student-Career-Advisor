using CareerAdvisor.Core.Enums;
using CareerAdvisor.Core.Interfaces;
using CareerAdvisor.Core.Models;
using CareerAdvisor.Infrastructure.Data;
using CareerAdvisor.Infrastructure.Repositories;
using CareerAdvisor.Infrastructure.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace CareerAdvisor.Tests.Services;

public sealed class RecommendationHistoryServiceTests
{
    [Fact]
    public async Task GetHistoryAsync_ExistingProfileWithNoSessions_ReturnsEmpty()
    {
        await using var database = await TestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var service = CreateService(context);

        var history = await service.GetHistoryAsync(
            database.FirstProfileId);

        Assert.Empty(history);
    }

    [Fact]
    public async Task GetHistoryAsync_ReturnsOnlySelectedProfileNewestFirst()
    {
        await using var database = await TestDatabase.CreateAsync();

        var oldest = CreateSession(
            database.FirstProfileId,
            database.Careers,
            DateTime.UtcNow.AddDays(-2));

        var newest = CreateSession(
            database.FirstProfileId,
            database.Careers,
            DateTime.UtcNow);

        var otherProfile = CreateSession(
            database.SecondProfileId,
            database.Careers,
            DateTime.UtcNow.AddDays(1));

        await database.SaveSessionsAsync(oldest, otherProfile, newest);

        await using var context = database.CreateContext();
        var service = CreateService(context);

        var history = (await service.GetHistoryAsync(
                database.FirstProfileId))
            .ToList();

        Assert.Equal([newest.Id, oldest.Id], history.Select(x => x.Id));
        Assert.All(history, session =>
            Assert.Equal(database.FirstProfileId, session.StudentProfileId));
        Assert.DoesNotContain(history, session => session.Id == otherProfile.Id);
    }

    [Fact]
    public async Task GetHistoryAsync_ReturnsThreeSavedRecommendationsWithUnchangedValues()
    {
        await using var database = await TestDatabase.CreateAsync();
        var session = CreateSession(
            database.FirstProfileId,
            database.Careers,
            DateTime.UtcNow);

        var expected = session.Recommendations.ToDictionary(
            recommendation => recommendation.Id,
            recommendation => new
            {
                recommendation.MatchScore,
                recommendation.Reasoning,
                recommendation.CareerProfileId
            });

        await database.SaveSessionsAsync(session);

        await using var context = database.CreateContext();
        var service = CreateService(context);
        var saved = Assert.Single(await service.GetHistoryAsync(
            database.FirstProfileId));

        Assert.Equal(3, saved.Recommendations.Count);

        Assert.All(saved.Recommendations, recommendation =>
        {
            var original = expected[recommendation.Id];
            Assert.Equal(original.MatchScore, recommendation.MatchScore);
            Assert.Equal(original.Reasoning, recommendation.Reasoning);
            Assert.Equal(
                original.CareerProfileId,
                recommendation.CareerProfileId);
        });
    }

    [Fact]
    public async Task GetSessionAsync_OwnedSessionReopensWithCareerDetailsAndSavedValues()
    {
        await using var database = await TestDatabase.CreateAsync();
        var session = CreateSession(
            database.FirstProfileId,
            database.Careers,
            DateTime.UtcNow);

        var expected = session.Recommendations.ToDictionary(
            recommendation => recommendation.Id,
            recommendation => new
            {
                recommendation.MatchScore,
                recommendation.Reasoning,
                recommendation.CareerProfileId
            });

        await database.SaveSessionsAsync(session);

        await using var context = database.CreateContext();
        var service = CreateService(context);

        var reopened = await service.GetSessionAsync(
            database.FirstProfileId,
            session.Id);

        Assert.NotNull(reopened);
        Assert.Equal(session.Id, reopened.Id);
        Assert.Equal(3, reopened.Recommendations.Count);

        Assert.All(reopened.Recommendations, recommendation =>
        {
            var original = expected[recommendation.Id];
            Assert.Equal(original.MatchScore, recommendation.MatchScore);
            Assert.Equal(original.Reasoning, recommendation.Reasoning);
            Assert.Equal(
                original.CareerProfileId,
                recommendation.CareerProfileId);
            Assert.NotNull(recommendation.Career);
            Assert.False(string.IsNullOrWhiteSpace(
                recommendation.Career!.Title));
            Assert.False(string.IsNullOrWhiteSpace(
                recommendation.Career.Description));
        });
    }

    [Fact]
    public async Task GetSessionAsync_UnknownSession_ReturnsNull()
    {
        await using var database = await TestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var service = CreateService(context);

        var result = await service.GetSessionAsync(
            database.FirstProfileId,
            Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task GetSessionAsync_OtherProfilesSession_ReturnsNull()
    {
        await using var database = await TestDatabase.CreateAsync();
        var session = CreateSession(
            database.SecondProfileId,
            database.Careers,
            DateTime.UtcNow);

        await database.SaveSessionsAsync(session);

        await using var context = database.CreateContext();
        var service = CreateService(context);

        var result = await service.GetSessionAsync(
            database.FirstProfileId,
            session.Id);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetHistoryAsync_UnknownProfile_IsRejected()
    {
        await using var database = await TestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var service = CreateService(context);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.GetHistoryAsync(Guid.NewGuid()));

        Assert.Contains(
            "could not be found",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetSessionAsync_UnknownProfile_IsRejectedBeforeSessionLookup()
    {
        await using var database = await TestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var service = CreateService(context);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.GetSessionAsync(
                Guid.NewGuid(),
                Guid.NewGuid()));

        Assert.Contains(
            "could not be found",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetHistoryAsync_EmptyProfileId_IsRejected()
    {
        await using var database = await TestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var service = CreateService(context);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.GetHistoryAsync(Guid.Empty));
    }

    [Fact]
    public async Task GetSessionAsync_EmptyProfileId_IsRejected()
    {
        await using var database = await TestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var service = CreateService(context);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.GetSessionAsync(Guid.Empty, Guid.NewGuid()));
    }

    [Fact]
    public async Task GetSessionAsync_EmptySessionId_IsRejected()
    {
        await using var database = await TestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var service = CreateService(context);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.GetSessionAsync(
                database.FirstProfileId,
                Guid.Empty));
    }

    [Fact]
    public async Task GetSessionAsync_IncompleteSession_IsRejected()
    {
        await using var database = await TestDatabase.CreateAsync();
        var session = CreateSession(
            database.FirstProfileId,
            database.Careers,
            DateTime.UtcNow);

        session.Recommendations.RemoveAt(2);
        await database.SaveSessionsAsync(session);

        var exception = await ReopenExpectingInvalidSessionAsync(
            database,
            session.Id);

        Assert.Contains("exactly three", exception.Message);
    }

    [Fact]
    public async Task GetSessionAsync_DuplicateCareers_IsRejected()
    {
        await using var database = await TestDatabase.CreateAsync();
        var session = CreateSession(
            database.FirstProfileId,
            database.Careers,
            DateTime.UtcNow);

        session.Recommendations[2].CareerProfileId =
            session.Recommendations[0].CareerProfileId;

        await database.SaveSessionsAsync(session);

        var exception = await ReopenExpectingInvalidSessionAsync(
            database,
            session.Id);

        Assert.Contains("duplicate careers", exception.Message);
    }

    [Fact]
    public async Task GetSessionAsync_MissingCareerDetails_IsRejected()
    {
        await using var database = await TestDatabase.CreateAsync();
        var session = CreateSession(
            database.FirstProfileId,
            database.Careers,
            DateTime.UtcNow);

        await database.SaveSessionsAsync(session);

        await using (var writeContext = database.CreateContext())
        {
            var career = await writeContext.CareerProfiles
                .SingleAsync(item =>
                    item.Id == session.Recommendations[0].CareerProfileId);

            career.Description = string.Empty;
            await writeContext.SaveChangesAsync();
        }

        var exception = await ReopenExpectingInvalidSessionAsync(
            database,
            session.Id);

        Assert.Contains("career details", exception.Message);
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(1.01)]
    [InlineData(double.PositiveInfinity)]
    public async Task GetSessionAsync_InvalidSavedScore_IsRejected(
        double invalidScore)
    {
        await using var database = await TestDatabase.CreateAsync();
        var session = CreateSession(
            database.FirstProfileId,
            database.Careers,
            DateTime.UtcNow);

        session.Recommendations[0].MatchScore = invalidScore;
        await database.SaveSessionsAsync(session);

        var exception = await ReopenExpectingInvalidSessionAsync(
            database,
            session.Id);

        Assert.Contains("match scores", exception.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetSessionAsync_BlankSavedReasoning_IsRejected(
        string reasoning)
    {
        await using var database = await TestDatabase.CreateAsync();
        var session = CreateSession(
            database.FirstProfileId,
            database.Careers,
            DateTime.UtcNow);

        session.Recommendations[0].Reasoning = reasoning;
        await database.SaveSessionsAsync(session);

        var exception = await ReopenExpectingInvalidSessionAsync(
            database,
            session.Id);

        Assert.Contains("explanations", exception.Message);
    }

    [Fact]
    public async Task GetSessionAsync_EqualScores_UsesTitleThenCareerIdOrdering()
    {
        await using var database = await TestDatabase.CreateAsync(
            careerTitles: ["Zulu Career", "Alpha Career", "Alpha Career"]);

        var session = CreateSession(
            database.FirstProfileId,
            database.Careers,
            DateTime.UtcNow,
            scores: [0.5, 0.5, 0.5]);

        await database.SaveSessionsAsync(session);

        await using var context = database.CreateContext();
        var service = CreateService(context);
        var reopened = await service.GetSessionAsync(
            database.FirstProfileId,
            session.Id);

        Assert.NotNull(reopened);

        var expected = database.Careers
            .OrderBy(career => career.Title, StringComparer.Ordinal)
            .ThenBy(career => career.Id)
            .Select(career => career.Id)
            .ToArray();

        Assert.Equal(
            expected,
            reopened.Recommendations
                .Select(recommendation => recommendation.CareerProfileId)
                .ToArray());
    }

    [Fact]
    public async Task EqualScores_ForwardAndReverseOrder_ProduceSameRanking()
    {
        var profile = CreateProfileForRepositoryDouble();
        var careers = CreateCareersForDeterministicOrdering();
        var forwardHistorySession = CreateLoadedSession(profile.Id, careers);
        var reverseHistorySession = CreateLoadedSession(
            profile.Id,
            careers.Reverse().ToList());
        var forwardDetailSession = CreateLoadedSession(profile.Id, careers);
        var reverseDetailSession = CreateLoadedSession(
            profile.Id,
            careers.Reverse().ToList());

        var expected = careers
            .OrderBy(career => career.Title, StringComparer.Ordinal)
            .ThenBy(career => career.Id)
            .Select(career => career.Id)
            .ToArray();

        var forwardHistoryService = CreateService(
            profile,
            forwardHistorySession);
        var reverseHistoryService = CreateService(
            profile,
            reverseHistorySession);
        var forwardDetailService = CreateService(
            profile,
            forwardDetailSession);
        var reverseDetailService = CreateService(
            profile,
            reverseDetailSession);

        var forwardHistory = Assert.Single(
            await forwardHistoryService.GetHistoryAsync(profile.Id));
        var reverseHistory = Assert.Single(
            await reverseHistoryService.GetHistoryAsync(profile.Id));
        var forwardSession = await forwardDetailService.GetSessionAsync(
            profile.Id,
            forwardDetailSession.Id);
        var reverseSession = await reverseDetailService.GetSessionAsync(
            profile.Id,
            reverseDetailSession.Id);

        Assert.NotNull(forwardSession);
        Assert.NotNull(reverseSession);
        Assert.Equal(expected, CareerIds(forwardHistory));
        Assert.Equal(expected, CareerIds(reverseHistory));
        Assert.Equal(expected, CareerIds(forwardSession));
        Assert.Equal(expected, CareerIds(reverseSession));
    }

    [Fact]
    public async Task GetSessionAsync_NullRecommendationCollection_IsSanitized()
    {
        var profile = CreateProfileForRepositoryDouble();
        var session = new RecommendationSession
        {
            StudentProfileId = profile.Id,
            Recommendations = null!
        };
        var service = CreateService(profile, session);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.GetSessionAsync(profile.Id, session.Id));

        Assert.StartsWith(
            "The saved recommendation session cannot be displayed",
            exception.Message);
        Assert.DoesNotContain("null", exception.Message,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetHistoryAsync_NullSessionEntry_IsSanitized()
    {
        var profile = CreateProfileForRepositoryDouble();
        var service = CreateService(profile, null);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => (await service.GetHistoryAsync(profile.Id)).ToList());

        Assert.StartsWith(
            "The saved recommendation session cannot be displayed",
            exception.Message);
        Assert.DoesNotContain("null", exception.Message,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetHistoryAsync_NullRecommendationItem_IsSanitized()
    {
        var profile = CreateProfileForRepositoryDouble();
        var careers = CreateCareersForDeterministicOrdering();
        var session = CreateLoadedSession(profile.Id, careers);
        session.Recommendations[1] = null!;
        var service = CreateService(profile, session);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => (await service.GetHistoryAsync(profile.Id)).ToList());

        Assert.StartsWith(
            "The saved recommendation session cannot be displayed",
            exception.Message);
        Assert.DoesNotContain("reference", exception.Message,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Constructor_RequiresOnlyProfileAndRecommendationRepositories()
    {
        var constructor = Assert.Single(
            typeof(RecommendationHistoryService).GetConstructors());

        Assert.Equal(
            [
                typeof(IStudentProfileRepository),
                typeof(IRecommendationRepository)
            ],
            constructor.GetParameters()
                .Select(parameter => parameter.ParameterType)
                .ToArray());
    }

    private static RecommendationHistoryService CreateService(
        CareerAdvisorDbContext context)
    {
        return new RecommendationHistoryService(
            new StudentProfileRepository(context),
            new RecommendationRepository(context));
    }

    private static RecommendationHistoryService CreateService(
        StudentProfile profile,
        RecommendationSession? session)
    {
        return new RecommendationHistoryService(
            new ProfileRepositoryDouble(profile),
            new RecommendationRepositoryDouble(session));
    }

    private static StudentProfile CreateProfileForRepositoryDouble()
    {
        return new StudentProfile
        {
            Id = Guid.NewGuid(),
            Name = "Test Student",
            Programme = "Computer Science",
            AcademicLevel = AcademicLevel.Undergraduate
        };
    }

    private static IReadOnlyList<CareerProfile>
        CreateCareersForDeterministicOrdering()
    {
        return
        [
            new CareerProfile
            {
                Id = Guid.Parse("10000000-0000-0000-0000-000000000003"),
                Title = "Zulu Career",
                Description = "Zulu description."
            },
            new CareerProfile
            {
                Id = Guid.Parse("10000000-0000-0000-0000-000000000002"),
                Title = "Alpha Career",
                Description = "Alpha description two."
            },
            new CareerProfile
            {
                Id = Guid.Parse("10000000-0000-0000-0000-000000000001"),
                Title = "Alpha Career",
                Description = "Alpha description one."
            }
        ];
    }

    private static RecommendationSession CreateLoadedSession(
        Guid profileId,
        IReadOnlyList<CareerProfile> careers)
    {
        return new RecommendationSession
        {
            Id = Guid.NewGuid(),
            StudentProfileId = profileId,
            Recommendations = careers.Select(career =>
                new CareerRecommendation
                {
                    CareerProfileId = career.Id,
                    Career = career,
                    MatchScore = 0.5,
                    Reasoning = $"Saved explanation for {career.Title}."
                }).ToList()
        };
    }

    private static Guid[] CareerIds(RecommendationSession session)
    {
        return session.Recommendations
            .Select(recommendation => recommendation.CareerProfileId)
            .ToArray();
    }

    private static async Task<InvalidOperationException>
        ReopenExpectingInvalidSessionAsync(
            TestDatabase database,
            Guid sessionId)
    {
        await using var context = database.CreateContext();
        var service = CreateService(context);

        return await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.GetSessionAsync(
                database.FirstProfileId,
                sessionId));
    }

    private static RecommendationSession CreateSession(
        Guid studentProfileId,
        IReadOnlyList<CareerProfile> careers,
        DateTime generatedAt,
        IReadOnlyList<double>? scores = null)
    {
        scores ??= [0.92, 0.84, 0.76];

        return new RecommendationSession
        {
            Id = Guid.NewGuid(),
            StudentProfileId = studentProfileId,
            GeneratedAt = generatedAt,
            Recommendations = careers
                .Select((career, index) => new CareerRecommendation
                {
                    Id = Guid.NewGuid(),
                    CareerProfileId = career.Id,
                    MatchScore = scores[index],
                    Reasoning = $"Saved explanation for {career.Title}."
                })
                .ToList()
        };
    }

    private sealed class ProfileRepositoryDouble(StudentProfile profile)
        : IStudentProfileRepository
    {
        public Task<StudentProfile?> GetByIdAsync(Guid id) =>
            Task.FromResult(id == profile.Id ? profile : null);

        public Task<IEnumerable<StudentProfile>> GetAllAsync() =>
            Task.FromResult<IEnumerable<StudentProfile>>([profile]);

        public Task AddAsync(StudentProfile entity) =>
            Task.CompletedTask;

        public Task UpdateAsync(StudentProfile entity) =>
            Task.CompletedTask;

        public Task DeleteAsync(Guid id) =>
            Task.CompletedTask;
    }

    private sealed class RecommendationRepositoryDouble(
        RecommendationSession? session)
        : IRecommendationRepository
    {
        public Task<RecommendationSession?> GetByIdAsync(Guid id) =>
            Task.FromResult(session is not null && id == session.Id
                ? session
                : null);

        public Task<IEnumerable<RecommendationSession>> GetAllAsync() =>
            Task.FromResult<IEnumerable<RecommendationSession>>([session!]);

        public Task<IEnumerable<RecommendationSession>>
            GetByStudentProfileIdAsync(Guid studentProfileId) =>
            Task.FromResult<IEnumerable<RecommendationSession>>(
                session is null ||
                session.StudentProfileId == studentProfileId
                    ? [session!]
                    : []);

        public Task AddAsync(RecommendationSession entity) =>
            Task.CompletedTask;

        public Task UpdateAsync(RecommendationSession entity) =>
            Task.CompletedTask;

        public Task DeleteAsync(Guid id) =>
            Task.CompletedTask;
    }

    private sealed class TestDatabase : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;

        private TestDatabase(
            SqliteConnection connection,
            DbContextOptions<CareerAdvisorDbContext> options,
            Guid firstProfileId,
            Guid secondProfileId,
            IReadOnlyList<CareerProfile> careers)
        {
            _connection = connection;
            Options = options;
            FirstProfileId = firstProfileId;
            SecondProfileId = secondProfileId;
            Careers = careers;
        }

        public DbContextOptions<CareerAdvisorDbContext> Options { get; }
        public Guid FirstProfileId { get; }
        public Guid SecondProfileId { get; }
        public IReadOnlyList<CareerProfile> Careers { get; }

        public static async Task<TestDatabase> CreateAsync(
            IReadOnlyList<string>? careerTitles = null)
        {
            careerTitles ??=
                ["Software Developer", "Data Analyst", "Cloud Engineer"];

            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();

            var options =
                new DbContextOptionsBuilder<CareerAdvisorDbContext>()
                    .UseSqlite(connection)
                    .Options;

            var firstProfile = CreateProfile("Ama Mensah");
            var secondProfile = CreateProfile("Kojo Asante");

            var careers = careerTitles
                .Select((title, index) => new CareerProfile
                {
                    Id = Guid.Parse(
                        $"10000000-0000-0000-0000-{index + 1:000000000000}"),
                    Code = $"TEST-{index + 1:000}",
                    Title = title,
                    Description = $"{title} persisted description."
                })
                .ToList();

            await using (var context =
                         new CareerAdvisorDbContext(options))
            {
                await context.Database.EnsureCreatedAsync();
                context.StudentProfiles.AddRange(firstProfile, secondProfile);
                context.CareerProfiles.AddRange(careers);
                await context.SaveChangesAsync();
            }

            return new TestDatabase(
                connection,
                options,
                firstProfile.Id,
                secondProfile.Id,
                careers);
        }

        public CareerAdvisorDbContext CreateContext()
        {
            return new CareerAdvisorDbContext(Options);
        }

        public async Task SaveSessionsAsync(
            params RecommendationSession[] sessions)
        {
            await using var context = CreateContext();
            var repository = new RecommendationRepository(context);

            foreach (var session in sessions)
            {
                await repository.AddAsync(session);
            }
        }

        public async ValueTask DisposeAsync()
        {
            await _connection.DisposeAsync();
        }

        private static StudentProfile CreateProfile(string name)
        {
            return new StudentProfile
            {
                Id = Guid.NewGuid(),
                Name = name,
                Programme = "Computer Science",
                AcademicLevel = AcademicLevel.Undergraduate,
                Interests = ["Technology"],
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }
    }
}
