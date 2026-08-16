using CareerAdvisor.Core.Enums;
using CareerAdvisor.Core.Models;
using CareerAdvisor.Infrastructure.Data;
using CareerAdvisor.Infrastructure.Repositories;
using CareerAdvisor.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace CareerAdvisor.Tests.Integration;

public sealed class Sprint2IntegrationTests
{
    [Fact]
    public async Task CompleteSprint2Journey_PersistsExpectedData()
    {
        await using var database = await TestDatabase.CreateAsync();

        var profile = CreateProfile();

        await using (var createContext = database.CreateContext())
        {
            var repository =
                new StudentProfileRepository(createContext);

            await repository.AddAsync(profile);
        }

        await using (var updateContext = database.CreateContext())
        {
            var repository =
                new StudentProfileRepository(updateContext);

            var savedProfile =
                await repository.GetByIdAsync(profile.Id);

            Assert.NotNull(savedProfile);

            savedProfile.Name = "Updated Student";
            savedProfile.Programme = "Information Technology";
            savedProfile.Interests.Add("Cloud Computing");
            savedProfile.Skills[0].Proficiency =
                SkillProficiency.Expert;

            savedProfile.Skills.Add(new StudentSkill
            {
                SkillName = "Database Administration",
                Proficiency = SkillProficiency.Beginner
            });

            await repository.UpdateAsync(savedProfile);
        }

        await using (var readProfileContext =
                     database.CreateContext())
        {
            var repository =
                new StudentProfileRepository(readProfileContext);

            var updatedProfile =
                await repository.GetByIdAsync(profile.Id);

            Assert.NotNull(updatedProfile);
            Assert.Equal("Updated Student", updatedProfile.Name);
            Assert.Equal(
                "Information Technology",
                updatedProfile.Programme);
            Assert.Contains(
                "Cloud Computing",
                updatedProfile.Interests);
            Assert.Equal(2, updatedProfile.Skills.Count);
            Assert.Contains(
                updatedProfile.Skills,
                skill =>
                    skill.SkillName ==
                    "Database Administration");
        }

        var careerRepository =
            new JsonCareerRepository(GetCatalogPath());

        var careerService =
            new CareerService(careerRepository);

        var careers =
            (await careerService.GetAllCareersAsync()).ToList();

        Assert.Equal(8, careers.Count);
        Assert.Equal(
            8,
            careers.Select(career => career.Code)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count());

        Guid sessionId;
        int requiredQuestionCount;

        await using (var assessmentContext =
                     database.CreateContext())
        {
            var assessmentService =
                new AssessmentService(assessmentContext);

            var session =
                assessmentService.CreateAssessmentSession(
                    profile.Id);

            var requiredQuestions =
                assessmentService.GetAllQuestions()
                    .Where(question => question.IsRequired)
                    .ToList();

            Assert.NotEmpty(requiredQuestions);

            foreach (var question in requiredQuestions)
            {
                var responseResult =
                    assessmentService.SubmitResponse(
                        session,
                        question.Id,
                        question.Options[0].Id);

                Assert.True(
                    responseResult.IsValid,
                    string.Join(" ", responseResult.Errors));
            }

            var completionResult =
                assessmentService.CompleteAssessmentSession(
                    session);

            Assert.True(
                completionResult.IsValid,
                string.Join(" ", completionResult.Errors));

            sessionId = session.Id;
            requiredQuestionCount = requiredQuestions.Count;
        }

        await using (var readAssessmentContext =
                     database.CreateContext())
        {
            var assessmentService =
                new AssessmentService(readAssessmentContext);

            var savedSession =
                assessmentService.GetAssessmentSession(sessionId);

            Assert.NotNull(savedSession);
            Assert.Equal("Completed", savedSession.Status);
            Assert.NotNull(savedSession.CompletedAt);
            Assert.Equal(
                requiredQuestionCount,
                savedSession.Responses.Count);
        }
    }

    [Fact]
    public async Task IncompleteAssessment_IsRejectedAndRemainsInProgress()
    {
        await using var database = await TestDatabase.CreateAsync();

        var profile = CreateProfile();

        await using (var profileContext =
                     database.CreateContext())
        {
            var repository =
                new StudentProfileRepository(profileContext);

            await repository.AddAsync(profile);
        }

        Guid sessionId;

        await using (var assessmentContext =
                     database.CreateContext())
        {
            var assessmentService =
                new AssessmentService(assessmentContext);

            var session =
                assessmentService.CreateAssessmentSession(
                    profile.Id);

            var requiredQuestions =
                assessmentService.GetAllQuestions()
                    .Where(question => question.IsRequired)
                    .ToList();

            Assert.True(requiredQuestions.Count > 1);

            var firstQuestion = requiredQuestions[0];

            var responseResult =
                assessmentService.SubmitResponse(
                    session,
                    firstQuestion.Id,
                    firstQuestion.Options[0].Id);

            Assert.True(
                responseResult.IsValid,
                string.Join(" ", responseResult.Errors));

            var completionResult =
                assessmentService.CompleteAssessmentSession(
                    session);

            Assert.False(completionResult.IsValid);
            Assert.Contains(
                completionResult.Errors,
                error => error.Contains(
                    "not answered",
                    StringComparison.OrdinalIgnoreCase));

            sessionId = session.Id;
        }

        await using (var readContext = database.CreateContext())
        {
            var assessmentService =
                new AssessmentService(readContext);

            var savedSession =
                assessmentService.GetAssessmentSession(sessionId);

            Assert.NotNull(savedSession);
            Assert.Equal("InProgress", savedSession.Status);
            Assert.Null(savedSession.CompletedAt);
            Assert.Single(savedSession.Responses);
        }
    }

    private static StudentProfile CreateProfile()
    {
        return new StudentProfile
        {
            Name = "Integration Test Student",
            Programme = "Computer Science",
            AcademicLevel = AcademicLevel.Undergraduate,
            Interests =
            [
                "Artificial Intelligence"
            ],
            Skills =
            [
                new StudentSkill
                {
                    SkillName = "Problem Solving",
                    Proficiency =
                        SkillProficiency.Intermediate
                }
            ]
        };
    }

    private static string GetCatalogPath()
    {
        return Path.GetFullPath(
            Path.Combine(
                AppContext.BaseDirectory,
                "..", "..", "..", "..", "..",
                "data",
                "career-catalog.json"));
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
                $"careeriq-integration-{Guid.NewGuid():N}.db");

            var options =
                new DbContextOptionsBuilder<CareerAdvisorDbContext>()
                    .UseSqlite(
                        $"Data Source={databasePath}")
                    .Options;

            await using var context =
                new CareerAdvisorDbContext(options);

            await context.Database.MigrateAsync();

            return new TestDatabase(databasePath, options);
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