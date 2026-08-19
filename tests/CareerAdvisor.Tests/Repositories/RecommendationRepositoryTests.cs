using CareerAdvisor.Core.Enums;
using CareerAdvisor.Core.Models;
using CareerAdvisor.Infrastructure.Data;
using CareerAdvisor.Infrastructure.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace CareerAdvisor.Tests.Repositories;

public class RecommendationRepositoryTests
{
    [Fact]
    public async Task AddAsync_SavesAndReopensSessionWithThreeCareers()
    {
        await using var connection =
            new SqliteConnection("Data Source=:memory:");

        await connection.OpenAsync();

        var options = CreateOptions(connection);
        var profile = CreateProfile();
        var careers = CreateCareers();

        await using (var writeContext =
                     new CareerAdvisorDbContext(options))
        {
            await writeContext.Database.EnsureCreatedAsync();

            writeContext.StudentProfiles.Add(profile);
            writeContext.CareerProfiles.AddRange(careers);
            await writeContext.SaveChangesAsync();

            var session = CreateSession(
                profile.Id,
                careers,
                DateTime.UtcNow);

            var repository =
                new RecommendationRepository(writeContext);

            await repository.AddAsync(session);
        }

        // A separate context simulates reopening the persisted data.
        await using var readContext =
            new CareerAdvisorDbContext(options);

        var readRepository =
            new RecommendationRepository(readContext);

        var savedSession =
            await readRepository.GetByIdAsync(
                readContext.RecommendationSessions
                    .Select(session => session.Id)
                    .Single());

        Assert.NotNull(savedSession);
        Assert.Equal(profile.Id, savedSession.StudentProfileId);
        Assert.Equal(3, savedSession.Recommendations.Count);

        Assert.All(
            savedSession.Recommendations,
            recommendation =>
            {
                Assert.Equal(
                    savedSession.Id,
                    recommendation.RecommendationSessionId);

                Assert.NotNull(recommendation.Career);
                Assert.NotEqual(
                    Guid.Empty,
                    recommendation.CareerProfileId);

                Assert.False(
                    string.IsNullOrWhiteSpace(
                        recommendation.Career!.Code));
            });

        Assert.Equal(
            careers.Select(career => career.Code).Order(),
            savedSession.Recommendations
                .Select(recommendation =>
                    recommendation.Career!.Code)
                .Order());
    }

    [Fact]
    public async Task GetByStudentProfileIdAsync_ReturnsOnlyRequestedStudentsSessions()
    {
        await using var connection =
            new SqliteConnection("Data Source=:memory:");

        await connection.OpenAsync();

        var options = CreateOptions(connection);
        var firstProfile = CreateProfile();
        var secondProfile = CreateProfile();
        secondProfile.Name = "Kojo Asante";

        var careers = CreateCareers();

        await using (var writeContext =
                     new CareerAdvisorDbContext(options))
        {
            await writeContext.Database.EnsureCreatedAsync();

            writeContext.StudentProfiles.AddRange(
                firstProfile,
                secondProfile);

            writeContext.CareerProfiles.AddRange(careers);
            await writeContext.SaveChangesAsync();

            var repository =
                new RecommendationRepository(writeContext);

            await repository.AddAsync(
                CreateSession(
                    firstProfile.Id,
                    careers,
                    DateTime.UtcNow.AddDays(-1)));

            await repository.AddAsync(
                CreateSession(
                    firstProfile.Id,
                    careers,
                    DateTime.UtcNow));

            await repository.AddAsync(
                CreateSession(
                    secondProfile.Id,
                    careers,
                    DateTime.UtcNow.AddHours(-1)));
        }

        await using var readContext =
            new CareerAdvisorDbContext(options);

        var readRepository =
            new RecommendationRepository(readContext);

        var sessions = (
            await readRepository.GetByStudentProfileIdAsync(
                firstProfile.Id))
            .ToList();

        Assert.Equal(2, sessions.Count);

        Assert.All(
            sessions,
            session =>
            {
                Assert.Equal(
                    firstProfile.Id,
                    session.StudentProfileId);

                Assert.Equal(3, session.Recommendations.Count);

                Assert.All(
                    session.Recommendations,
                    recommendation =>
                        Assert.NotNull(recommendation.Career));
            });

        Assert.True(
            sessions[0].GeneratedAt >=
            sessions[1].GeneratedAt);
    }

    [Fact]
    public async Task AddAsync_RejectsRecommendationForUnknownCareer()
    {
        await using var connection =
            new SqliteConnection("Data Source=:memory:");

        await connection.OpenAsync();

        var options = CreateOptions(connection);
        var profile = CreateProfile();

        await using var context =
            new CareerAdvisorDbContext(options);

        await context.Database.EnsureCreatedAsync();

        context.StudentProfiles.Add(profile);
        await context.SaveChangesAsync();

        var session = new RecommendationSession
        {
            StudentProfileId = profile.Id,
            Recommendations =
            [
                new CareerRecommendation
                {
                    CareerProfileId = Guid.NewGuid(),
                    MatchScore = 0.90,
                    Reasoning = "Unknown career reference."
                }
            ]
        };

        var repository =
            new RecommendationRepository(context);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => repository.AddAsync(session));

        Assert.Empty(
            await context.RecommendationSessions.ToListAsync());

        Assert.Empty(
            await context.CareerRecommendations.ToListAsync());
    }

    [Fact]
    public async Task DeleteAsync_CascadesToCareerRecommendations()
    {
        await using var connection =
            new SqliteConnection("Data Source=:memory:");

        await connection.OpenAsync();

        var options = CreateOptions(connection);
        var profile = CreateProfile();
        var careers = CreateCareers();
        var session = CreateSession(
            profile.Id,
            careers,
            DateTime.UtcNow);

        await using (var writeContext =
                     new CareerAdvisorDbContext(options))
        {
            await writeContext.Database.EnsureCreatedAsync();

            writeContext.StudentProfiles.Add(profile);
            writeContext.CareerProfiles.AddRange(careers);
            await writeContext.SaveChangesAsync();

            var repository =
                new RecommendationRepository(writeContext);

            await repository.AddAsync(session);
        }

        await using (var deleteContext =
                     new CareerAdvisorDbContext(options))
        {
            var repository =
                new RecommendationRepository(deleteContext);

            await repository.DeleteAsync(session.Id);
        }

        await using var readContext =
            new CareerAdvisorDbContext(options);

        Assert.Empty(
            await readContext.RecommendationSessions.ToListAsync());

        Assert.Empty(
            await readContext.CareerRecommendations.ToListAsync());

        // Deleting a session must not delete catalogue careers.
        Assert.Equal(
            3,
            await readContext.CareerProfiles.CountAsync());
    }

    private static DbContextOptions<CareerAdvisorDbContext>
        CreateOptions(SqliteConnection connection)
    {
        return new DbContextOptionsBuilder<CareerAdvisorDbContext>()
            .UseSqlite(connection)
            .Options;
    }

    private static StudentProfile CreateProfile()
    {
        return new StudentProfile
        {
            Id = Guid.NewGuid(),
            Name = "Ama Mensah",
            Programme = "Computer Science",
            AcademicLevel = AcademicLevel.Undergraduate,
            Interests = ["Software Development"],
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    private static List<CareerProfile> CreateCareers()
    {
        return
        [
            CreateCareer(
                "10000000-0000-0000-0000-000000000001",
                "SD-001",
                "Software Developer"),

            CreateCareer(
                "10000000-0000-0000-0000-000000000002",
                "DA-002",
                "Data Analyst"),

            CreateCareer(
                "10000000-0000-0000-0000-000000000003",
                "CS-003",
                "Cybersecurity Analyst")
        ];
    }

    private static CareerProfile CreateCareer(
        string id,
        string code,
        string title)
    {
        return new CareerProfile
        {
            Id = Guid.Parse(id),
            Code = code,
            Title = title,
            Description = $"{title} description."
        };
    }

    private static RecommendationSession CreateSession(
        Guid studentProfileId,
        IReadOnlyList<CareerProfile> careers,
        DateTime generatedAt)
    {
        return new RecommendationSession
        {
            Id = Guid.NewGuid(),
            StudentProfileId = studentProfileId,
            GeneratedAt = generatedAt,
            Recommendations =
            [
                CreateRecommendation(
                    careers[0],
                    0.92,
                    "Strong programming alignment."),

                CreateRecommendation(
                    careers[1],
                    0.84,
                    "Strong analytical alignment."),

                CreateRecommendation(
                    careers[2],
                    0.76,
                    "Strong security alignment.")
            ]
        };
    }

    private static CareerRecommendation CreateRecommendation(
        CareerProfile career,
        double score,
        string reasoning)
    {
        return new CareerRecommendation
        {
            Id = Guid.NewGuid(),
            CareerProfileId = career.Id,
            MatchScore = score,
            Reasoning = reasoning
        };
    }
}