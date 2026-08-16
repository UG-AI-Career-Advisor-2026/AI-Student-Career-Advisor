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