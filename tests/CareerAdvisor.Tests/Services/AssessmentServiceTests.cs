using CareerAdvisor.Core.Enums;
using CareerAdvisor.Core.Models;
using CareerAdvisor.Infrastructure.Data;
using CareerAdvisor.Infrastructure.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace CareerAdvisor.Tests.Services;

public class AssessmentServiceTests
{
    [Fact]
    public async Task CreateAssessmentSession_PersistsAndCanBeReopened()
    {
        await using var database = await TestDatabase.CreateAsync();
        var service = new AssessmentService(database.Context);

        Assert.Equal(
            database.StudentProfileId,
            service.GetAvailableStudentProfileId());

        var session = service.CreateAssessmentSession(
            database.StudentProfileId);

        await using var readContext =
            new CareerAdvisorDbContext(database.Options);

        var readService = new AssessmentService(readContext);
        var savedSession =
            readService.GetAssessmentSession(session.Id);

        Assert.NotNull(savedSession);
        Assert.Equal(
            database.StudentProfileId,
            savedSession.StudentProfileId);
        Assert.Equal("InProgress", savedSession.Status);
        Assert.Empty(savedSession.Responses);
    }

    [Fact]
    public async Task CreateAssessmentSession_RejectsUnknownProfile()
    {
        await using var database = await TestDatabase.CreateAsync();
        var service = new AssessmentService(database.Context);

        var exception = Assert.Throws<InvalidOperationException>(
            () => service.CreateAssessmentSession(Guid.NewGuid()));

        Assert.Contains(
            "valid student profile",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SubmitResponse_UpdatesAnswerWithoutDuplicate()
    {
        await using var database = await TestDatabase.CreateAsync();
        var service = new AssessmentService(database.Context);

        var session = service.CreateAssessmentSession(
            database.StudentProfileId);

        var question = service.GetAllQuestions().First();

        Assert.True(question.Options.Count >= 2);

        var firstResult = service.SubmitResponse(
            session,
            question.Id,
            question.Options[0].Id);

        var updateResult = service.SubmitResponse(
            session,
            question.Id,
            question.Options[1].Id);

        Assert.True(
            firstResult.IsValid,
            string.Join(" ", firstResult.Errors));

        Assert.True(
            updateResult.IsValid,
            string.Join(" ", updateResult.Errors));

        await using var readContext =
            new CareerAdvisorDbContext(database.Options);

        var responses = await readContext.AssessmentResponses
            .Where(response =>
                response.AssessmentSessionId == session.Id)
            .ToListAsync();

        var savedResponse = Assert.Single(responses);

        Assert.Equal(
            question.Options[1].Id,
            savedResponse.OptionId);
    }

    [Fact]
    public async Task CompleteAssessmentSession_RejectsMissingAnswers()
    {
        await using var database = await TestDatabase.CreateAsync();
        var service = new AssessmentService(database.Context);

        var session = service.CreateAssessmentSession(
            database.StudentProfileId);

        var requiredQuestions = service.GetAllQuestions()
            .Where(question => question.IsRequired)
            .ToList();

        Assert.True(requiredQuestions.Count > 1);

        var answeredQuestion = requiredQuestions[0];

        var responseResult = service.SubmitResponse(
            session,
            answeredQuestion.Id,
            answeredQuestion.Options[0].Id);

        Assert.True(
            responseResult.IsValid,
            string.Join(" ", responseResult.Errors));

        var completionResult =
            service.CompleteAssessmentSession(session);

        Assert.False(completionResult.IsValid);
        Assert.Contains(
            completionResult.Errors,
            error => error.Contains(
                "not answered",
                StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task CompleteAssessmentSession_PersistsCompletion()
    {
        await using var database = await TestDatabase.CreateAsync();
        var service = new AssessmentService(database.Context);

        var session = service.CreateAssessmentSession(
            database.StudentProfileId);

        var requiredQuestions = service.GetAllQuestions()
            .Where(question => question.IsRequired)
            .ToList();

        foreach (var question in requiredQuestions)
        {
            var responseResult = service.SubmitResponse(
                session,
                question.Id,
                question.Options[0].Id);

            Assert.True(
                responseResult.IsValid,
                string.Join(" ", responseResult.Errors));
        }

        var completionResult =
            service.CompleteAssessmentSession(session);

        Assert.True(
            completionResult.IsValid,
            string.Join(" ", completionResult.Errors));

        await using var readContext =
            new CareerAdvisorDbContext(database.Options);

        var readService = new AssessmentService(readContext);
        var savedSession =
            readService.GetAssessmentSession(session.Id);

        Assert.NotNull(savedSession);
        Assert.Equal("Completed", savedSession.Status);
        Assert.NotNull(savedSession.CompletedAt);
        Assert.Equal(
            requiredQuestions.Count,
            savedSession.Responses.Count);
    }
        [Fact]
    public async Task GetLatestCompletedAssessmentAsync_ReturnsLatestCompletedSession()
    {
        await using var database = await TestDatabase.CreateAsync();
        var service = new AssessmentService(database.Context);
        var questions = service.GetAllQuestions();

        var olderSession = service.CreateAssessmentSession(
            database.StudentProfileId);

        foreach (var question in questions)
        {
            var responseResult = service.SubmitResponse(
                olderSession,
                question.Id,
                question.Options[0].Id);

            Assert.True(
                responseResult.IsValid,
                string.Join(" ", responseResult.Errors));
        }

        var olderCompletion =
            service.CompleteAssessmentSession(olderSession);

        Assert.True(
            olderCompletion.IsValid,
            string.Join(" ", olderCompletion.Errors));

        olderSession.CompletedAt = DateTime.UtcNow.AddDays(-1);
        await database.Context.SaveChangesAsync();

        var latestSession = service.CreateAssessmentSession(
            database.StudentProfileId);

        foreach (var question in questions)
        {
            var responseResult = service.SubmitResponse(
                latestSession,
                question.Id,
                question.Options[1].Id);

            Assert.True(
                responseResult.IsValid,
                string.Join(" ", responseResult.Errors));
        }

        var latestCompletion =
            service.CompleteAssessmentSession(latestSession);

        Assert.True(
            latestCompletion.IsValid,
            string.Join(" ", latestCompletion.Errors));

        var inProgressSession = service.CreateAssessmentSession(
            database.StudentProfileId);

        var result =
            await service.GetLatestCompletedAssessmentAsync(
                database.StudentProfileId);

        Assert.NotNull(result);
        Assert.Equal(latestSession.Id, result.Id);
        Assert.Equal("Completed", result.Status);
        Assert.Equal(questions.Count, result.Responses.Count);
        Assert.NotEqual(inProgressSession.Id, result.Id);
        Assert.NotEqual(olderSession.Id, result.Id);
    }

    [Fact]
    public async Task GetLatestCompletedAssessmentAsync_ReturnsNullWhenUnavailable()
    {
        await using var database = await TestDatabase.CreateAsync();
        var service = new AssessmentService(database.Context);

        var emptyIdResult =
            await service.GetLatestCompletedAssessmentAsync(
                Guid.Empty);

        var unknownProfileResult =
            await service.GetLatestCompletedAssessmentAsync(
                Guid.NewGuid());

        var withoutCompletedAssessment =
            await service.GetLatestCompletedAssessmentAsync(
                database.StudentProfileId);

        Assert.Null(emptyIdResult);
        Assert.Null(unknownProfileResult);
        Assert.Null(withoutCompletedAssessment);
    }
    private sealed class TestDatabase : IAsyncDisposable
    {
        private TestDatabase(
            SqliteConnection connection,
            DbContextOptions<CareerAdvisorDbContext> options,
            CareerAdvisorDbContext context,
            Guid studentProfileId)
        {
            Connection = connection;
            Options = options;
            Context = context;
            StudentProfileId = studentProfileId;
        }

        private SqliteConnection Connection { get; }

        public DbContextOptions<CareerAdvisorDbContext> Options
        {
            get;
        }

        public CareerAdvisorDbContext Context { get; }

        public Guid StudentProfileId { get; }

        public static async Task<TestDatabase> CreateAsync()
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

            var profile = new StudentProfile
            {
                Name = "Test Student",
                Programme = "Computer Science",
                AcademicLevel = AcademicLevel.Undergraduate,
                Interests = ["Technology"],
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

            context.StudentProfiles.Add(profile);
            await context.SaveChangesAsync();

            return new TestDatabase(
                connection,
                options,
                context,
                profile.Id);
        }

        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            await Connection.DisposeAsync();
        }
    }
}