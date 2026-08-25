using CareerAdvisor.Core.Enums;
using CareerAdvisor.Core.Interfaces;
using CareerAdvisor.Core.Models;
using CareerAdvisor.Core.SkillGaps;
using CareerAdvisor.Core.Validators;
using CareerAdvisor.Infrastructure.Data;
using CareerAdvisor.Infrastructure.MachineLearning;
using CareerAdvisor.Infrastructure.Repositories;
using CareerAdvisor.Infrastructure.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CareerAdvisor.Tests.Integration;

public sealed class Sprint4IntegrationTests
{
    private static readonly DateTime RoadmapUtc =
        new(2026, 8, 24, 16, 30, 0, DateTimeKind.Utc);

    [Fact]
    public async Task CompleteSprint4Journey_RealServicesPersistAndReopenAcrossRestart()
    {
        await using var database = await Sprint4Database.CreateAsync();
        ServiceProvider? firstProvider = await database.CreateProviderAsync();

        try
        {
            var careers = await GetCareersAsync(firstProvider);
            var profile = CreateProfile(careers, "Sprint 4 Journey Student");
            await SaveProfileAndCompleteAssessmentAsync(firstProvider, profile);

            RecommendationSession generated;

            await using (var scope = firstProvider.CreateAsyncScope())
            {
                generated = await scope.ServiceProvider
                    .GetRequiredService<IRecommendationService>()
                    .GenerateRecommendationsAsync(profile.Id);
            }

            Assert.Equal(3, generated.Recommendations.Count);
            Assert.Equal(
                3,
                generated.Recommendations
                    .Select(item => item.CareerProfileId)
                    .Distinct()
                    .Count());

            var recommendationSnapshot = RecommendationValues(generated);
            var selectedCareerId = generated.Recommendations[0].CareerProfileId;
            var selectedCareer = careers.Single(career =>
                career.Id == selectedCareerId);

            SkillGapResult firstGap;
            SkillGapResult secondGap;
            LearningRoadmap generatedRoadmap;
            LearningRoadmap updatedRoadmap;

            await using (var scope = firstProvider.CreateAsyncScope())
            {
                var skillGapService = scope.ServiceProvider
                    .GetRequiredService<ISkillGapService>();
                firstGap = await skillGapService.AnalyzeAsync(
                    profile.Id,
                    selectedCareerId);
                secondGap = await skillGapService.AnalyzeAsync(
                    profile.Id,
                    selectedCareerId);

                var roadmapService = scope.ServiceProvider
                    .GetRequiredService<IRoadmapService>();
                generatedRoadmap = await roadmapService.GenerateRoadmapAsync(
                    profile.Id,
                    selectedCareerId);
                var reopenedExisting = await roadmapService.GenerateRoadmapAsync(
                    profile.Id,
                    selectedCareerId);

                Assert.Equal(generatedRoadmap.Id, reopenedExisting.Id);
                Assert.Equal(
                    RoadmapStepValues(generatedRoadmap),
                    RoadmapStepValues(reopenedExisting));

                updatedRoadmap = await roadmapService
                    .UpdateRoadmapProgressAsync(
                        profile.Id,
                        generatedRoadmap.Id,
                        generatedRoadmap.Steps[0].Id,
                        true);
            }

            Assert.Equal(selectedCareer.RequiredSkills.Count, firstGap.Items.Count);
            Assert.True(firstGap.MissingCount > 0);
            Assert.True(firstGap.NeedsDevelopmentCount > 0);
            Assert.True(firstGap.MatchedCount > 0);
            Assert.Equal(
                firstGap.Items.Count,
                firstGap.MissingCount + firstGap.NeedsDevelopmentCount +
                firstGap.MatchedCount);
            Assert.Equal(SkillGapValues(firstGap), SkillGapValues(secondGap));
            Assert.NotSame(firstGap, secondGap);
            Assert.NotSame(firstGap.Items, secondGap.Items);
            Assert.Equal(
                firstGap.Items.Count,
                firstGap.Items
                    .Select(item => SkillNameNormalizer.Normalize(
                        item.RequiredSkillName))
                    .Distinct(StringComparer.Ordinal)
                    .Count());
            Assert.Equal(
                SkillGapValues(firstGap)
                    .OrderBy(item => ClassificationRank(item.Classification))
                    .ThenBy(item => item.RequiredSkillName, StringComparer.Ordinal),
                SkillGapValues(firstGap));

            Assert.InRange(
                generatedRoadmap.Steps.Count,
                1,
                LearningRoadmapValidator.MaximumStepCount);
            Assert.Equal(
                Enumerable.Range(1, generatedRoadmap.Steps.Count),
                generatedRoadmap.Steps.Select(step => step.Order));
            Assert.Equal(
                generatedRoadmap.Steps.Count,
                generatedRoadmap.Steps
                    .Select(step => SkillNameNormalizer.Normalize(step.Title))
                    .Distinct(StringComparer.Ordinal)
                    .Count());
            AssertSkillStepPriority(generatedRoadmap.Steps);
            Assert.True(updatedRoadmap.Steps[0].IsCompleted);
            Assert.Equal(RoadmapUtc, updatedRoadmap.Steps[0].CompletedAt);

            var roadmapId = generatedRoadmap.Id;
            var completedStepId = generatedRoadmap.Steps[0].Id;
            await firstProvider.DisposeAsync();
            firstProvider = null;

            await using var restartedProvider =
                await database.CreateProviderAsync();
            LearningRoadmap reopenedRoadmap;
            IReadOnlyList<RecommendationSession> history;
            RecommendationSession? reopenedSession;

            await using (var scope = restartedProvider.CreateAsyncScope())
            {
                reopenedRoadmap = await scope.ServiceProvider
                    .GetRequiredService<IRoadmapService>()
                    .GetRoadmapAsync(profile.Id, roadmapId);
                var historyService = scope.ServiceProvider
                    .GetRequiredService<IRecommendationHistoryService>();
                history = (await historyService.GetHistoryAsync(profile.Id))
                    .ToList();
                reopenedSession = await historyService.GetSessionAsync(
                    profile.Id,
                    generated.Id);
            }

            Assert.Equal(roadmapId, reopenedRoadmap.Id);
            Assert.Equal(
                Enumerable.Range(1, reopenedRoadmap.Steps.Count),
                reopenedRoadmap.Steps.Select(step => step.Order));
            var reopenedStep = reopenedRoadmap.Steps.Single(step =>
                step.Id == completedStepId);
            Assert.True(reopenedStep.IsCompleted);
            Assert.Equal(RoadmapUtc, reopenedStep.CompletedAt);

            Assert.NotNull(reopenedSession);
            Assert.Equal(recommendationSnapshot, RecommendationValues(reopenedSession));
            Assert.Equal(generated.Id, Assert.Single(history).Id);
        }
        finally
        {
            if (firstProvider is not null)
            {
                await firstProvider.DisposeAsync();
            }
        }
    }

    [Fact]
    public async Task InvalidSprint4Prerequisites_DoNotWriteOrFabricateResults()
    {
        await using var database = await Sprint4Database.CreateAsync();
        await using var provider = await database.CreateProviderAsync();
        var careers = await GetCareersAsync(provider);
        var profile = CreateProfile(careers, "Prerequisite Test Student");

        await using (var scope = provider.CreateAsyncScope())
        {
            await scope.ServiceProvider
                .GetRequiredService<IStudentProfileRepository>()
                .AddAsync(profile);
        }

        await using (var scope = provider.CreateAsyncScope())
        {
            var skillGapService = scope.ServiceProvider
                .GetRequiredService<ISkillGapService>();
            var roadmapService = scope.ServiceProvider
                .GetRequiredService<IRoadmapService>();
            var historyService = scope.ServiceProvider
                .GetRequiredService<IRecommendationHistoryService>();

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                skillGapService.AnalyzeAsync(Guid.NewGuid(), careers[0].Id));
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                skillGapService.AnalyzeAsync(profile.Id, Guid.NewGuid()));
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                roadmapService.GenerateRoadmapAsync(profile.Id, careers[0].Id));

            Assert.Empty(await historyService.GetHistoryAsync(profile.Id));
            Assert.Null(await historyService.GetSessionAsync(
                profile.Id,
                Guid.NewGuid()));
        }

        await using var verificationScope = provider.CreateAsyncScope();
        var context = verificationScope.ServiceProvider
            .GetRequiredService<CareerAdvisorDbContext>();
        Assert.Equal(0, await context.RecommendationSessions.CountAsync());
        Assert.Equal(0, await context.CareerRecommendations.CountAsync());
        Assert.Equal(0, await context.LearningRoadmaps.CountAsync());
        Assert.Equal(0, await context.RoadmapSteps.CountAsync());
    }

    [Fact]
    public async Task Sprint4Isolation_HidesOtherProfilesRoadmapsAndHistory()
    {
        await using var database = await Sprint4Database.CreateAsync();
        await using var provider = await database.CreateProviderAsync();
        var careers = await GetCareersAsync(provider);
        var firstProfile = CreateProfile(careers, "Profile A");
        var secondProfile = CreateProfile(careers, "Profile B");
        await SaveProfileAndCompleteAssessmentAsync(provider, firstProfile);

        await using (var scope = provider.CreateAsyncScope())
        {
            await scope.ServiceProvider
                .GetRequiredService<IStudentProfileRepository>()
                .AddAsync(secondProfile);
        }

        RecommendationSession firstSession;
        RecommendationSession secondSession;
        LearningRoadmap roadmap;

        await using (var scope = provider.CreateAsyncScope())
        {
            var recommendationService = scope.ServiceProvider
                .GetRequiredService<IRecommendationService>();
            firstSession = await recommendationService
                .GenerateRecommendationsAsync(firstProfile.Id);
            secondSession = await recommendationService
                .GenerateRecommendationsAsync(firstProfile.Id);
            Assert.Equal(firstSession.Id, secondSession.Id);
            roadmap = await scope.ServiceProvider
                .GetRequiredService<IRoadmapService>()
                .GenerateRoadmapAsync(
                    firstProfile.Id,
                    firstSession.Recommendations[0].CareerProfileId);
        }

        await using (var scope = provider.CreateAsyncScope())
        {
            var historyService = scope.ServiceProvider
                .GetRequiredService<IRecommendationHistoryService>();
            var firstHistory = (await historyService
                    .GetHistoryAsync(firstProfile.Id))
                .ToList();
            var savedSession = Assert.Single(firstHistory);
            Assert.Equal(firstSession.Id, savedSession.Id);
            Assert.Empty(await historyService.GetHistoryAsync(secondProfile.Id));
            Assert.Null(await historyService.GetSessionAsync(
                secondProfile.Id,
                firstSession.Id));
            Assert.Null(await historyService.GetSessionAsync(
                secondProfile.Id,
                Guid.NewGuid()));

            var roadmapService = scope.ServiceProvider
                .GetRequiredService<IRoadmapService>();
            var crossProfileRoadmap = await Assert.ThrowsAsync<InvalidOperationException>(
                () => roadmapService.GetRoadmapAsync(
                    secondProfile.Id,
                    roadmap.Id));
            var unknownRoadmap = await Assert.ThrowsAsync<InvalidOperationException>(
                () => roadmapService.GetRoadmapAsync(
                    secondProfile.Id,
                    Guid.NewGuid()));
            Assert.Equal(unknownRoadmap.Message, crossProfileRoadmap.Message);

            var crossProfileUpdate = await Assert.ThrowsAsync<InvalidOperationException>(
                () => roadmapService.UpdateRoadmapProgressAsync(
                    secondProfile.Id,
                    roadmap.Id,
                    roadmap.Steps[0].Id,
                    true));
            var unknownUpdate = await Assert.ThrowsAsync<InvalidOperationException>(
                () => roadmapService.UpdateRoadmapProgressAsync(
                    secondProfile.Id,
                    Guid.NewGuid(),
                    roadmap.Steps[0].Id,
                    true));
            Assert.Equal(unknownUpdate.Message, crossProfileUpdate.Message);
        }

        await using var verificationScope = provider.CreateAsyncScope();
        var context = verificationScope.ServiceProvider
            .GetRequiredService<CareerAdvisorDbContext>();
        Assert.Equal(1, await context.RecommendationSessions.CountAsync());
        Assert.Equal(3, await context.CareerRecommendations.CountAsync());
        Assert.Equal(1, await context.LearningRoadmaps.CountAsync());
        Assert.All(
            await context.RoadmapSteps.AsNoTracking().ToListAsync(),
            step =>
            {
                Assert.False(step.IsCompleted);
                Assert.Null(step.CompletedAt);
            });
    }

    private static async Task<IReadOnlyList<CareerProfile>> GetCareersAsync(
        IServiceProvider provider)
    {
        return (await provider.GetRequiredService<ICareerRepository>()
                .GetAllAsync())
            .OrderBy(career => career.Code, StringComparer.Ordinal)
            .ToList();
    }

    private static StudentProfile CreateProfile(
        IReadOnlyList<CareerProfile> careers,
        string name)
    {
        var skills = careers.SelectMany(career => new[]
            {
                CreateSkill(
                    career.RequiredSkills[0],
                    SkillProficiency.Beginner),
                CreateSkill(
                    career.RequiredSkills[1],
                    SkillProficiency.Intermediate)
            })
            .GroupBy(
                skill => SkillNameNormalizer.Normalize(skill.SkillName),
                StringComparer.Ordinal)
            .Select(group => group.First())
            .ToList();

        return new StudentProfile
        {
            Id = Guid.NewGuid(),
            Name = name,
            Programme = "Computer Science",
            AcademicLevel = AcademicLevel.Undergraduate,
            Interests =
            [
                "Technology",
                "Data Analysis",
                "Artificial Intelligence"
            ],
            Skills = skills
        };
    }

    private static StudentSkill CreateSkill(
        string name,
        SkillProficiency proficiency)
    {
        return new StudentSkill
        {
            Id = Guid.NewGuid(),
            SkillName = name,
            Proficiency = proficiency
        };
    }

    private static async Task SaveProfileAndCompleteAssessmentAsync(
        ServiceProvider provider,
        StudentProfile profile)
    {
        await using var scope = provider.CreateAsyncScope();
        await scope.ServiceProvider
            .GetRequiredService<IStudentProfileRepository>()
            .AddAsync(profile);

        var assessmentService = scope.ServiceProvider
            .GetRequiredService<IAssessmentService>();
        var session = assessmentService.CreateAssessmentSession(profile.Id);

        foreach (var question in assessmentService.GetAllQuestions())
        {
            var response = assessmentService.SubmitResponse(
                session,
                question.Id,
                question.Options[0].Id);
            Assert.True(response.IsValid, string.Join(" ", response.Errors));
        }

        var completion = assessmentService.CompleteAssessmentSession(session);
        Assert.True(completion.IsValid, string.Join(" ", completion.Errors));
    }

    private static IReadOnlyList<RecommendationValue> RecommendationValues(
        RecommendationSession session)
    {
        return session.Recommendations
            .OrderBy(item => item.Id)
            .Select(item => new RecommendationValue(
                item.Id,
                item.RecommendationSessionId,
                item.CareerProfileId,
                item.MatchScore,
                item.Reasoning))
            .ToList();
    }

    private static IReadOnlyList<SkillGapValue> SkillGapValues(
        SkillGapResult result)
    {
        return result.Items.Select(item => new SkillGapValue(
                item.RequiredSkillName,
                item.Classification,
                item.MatchedStudentSkillName,
                item.CurrentProficiency))
            .ToList();
    }

    private static IReadOnlyList<RoadmapStepValue> RoadmapStepValues(
        LearningRoadmap roadmap)
    {
        return roadmap.Steps
            .OrderBy(step => step.Order)
            .ThenBy(step => step.Id)
            .Select(step => new RoadmapStepValue(
                step.Order,
                step.Title,
                step.Description,
                step.ResourceLink,
                step.IsCompleted,
                step.CompletedAt))
            .ToList();
    }

    private static int ClassificationRank(
        SkillGapClassification classification)
    {
        return classification switch
        {
            SkillGapClassification.Missing => 0,
            SkillGapClassification.NeedsDevelopment => 1,
            SkillGapClassification.Matched => 2,
            _ => throw new ArgumentOutOfRangeException(nameof(classification))
        };
    }

    private static void AssertSkillStepPriority(
        IReadOnlyList<RoadmapStep> steps)
    {
        var categories = steps.Select(step =>
            step.Title.StartsWith("Build foundation: ", StringComparison.Ordinal)
                ? 0
                : step.Title.StartsWith(
                    "Develop further: ",
                    StringComparison.Ordinal)
                    ? 1
                    : step.Title.StartsWith(
                        "Study topic: ",
                        StringComparison.Ordinal)
                        ? 2
                        : step.Title.StartsWith(
                            "Explore certification: ",
                            StringComparison.Ordinal)
                            ? 3
                            : 4).ToList();

        Assert.Equal(categories.Order(), categories);
    }

    private sealed record RecommendationValue(
        Guid Id,
        Guid SessionId,
        Guid CareerId,
        double MatchScore,
        string Reasoning);

    private sealed record SkillGapValue(
        string RequiredSkillName,
        SkillGapClassification Classification,
        string? MatchedStudentSkillName,
        SkillProficiency? CurrentProficiency);

    private sealed record RoadmapStepValue(
        int Order,
        string Title,
        string Description,
        string ResourceLink,
        bool IsCompleted,
        DateTime? CompletedAt);

    private sealed class FixedTimeProvider : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(RoadmapUtc);
    }

    private sealed class Sprint4Database : IAsyncDisposable
    {
        private readonly string _directoryPath;
        private readonly string _databasePath;
        private readonly string _catalogPath;
        private readonly string _modelPath;
        private readonly string _metadataPath;

        private Sprint4Database(
            string directoryPath,
            string databasePath,
            string catalogPath,
            string modelPath,
            string metadataPath)
        {
            _directoryPath = directoryPath;
            _databasePath = databasePath;
            _catalogPath = catalogPath;
            _modelPath = modelPath;
            _metadataPath = metadataPath;
        }

        public static Task<Sprint4Database> CreateAsync()
        {
            var repositoryRoot = FindRepositoryRoot();
            var directoryPath = Path.Combine(
                Path.GetTempPath(),
                $"careeriq-sprint4-{Guid.NewGuid():N}");
            Directory.CreateDirectory(directoryPath);

            return Task.FromResult(new Sprint4Database(
                directoryPath,
                Path.Combine(directoryPath, "sprint4.db"),
                Path.Combine(repositoryRoot, "data", "career-catalog.json"),
                Path.Combine(
                    repositoryRoot,
                    "data",
                    "models",
                    "career-recommendation-model.zip"),
                Path.Combine(
                    repositoryRoot,
                    "data",
                    "models",
                    "career-recommendation-model.metadata.json")));
        }

        public async Task<ServiceProvider> CreateProviderAsync()
        {
            var services = new ServiceCollection();
            services.AddDbContext<CareerAdvisorDbContext>(options =>
                options.UseSqlite(
                    $"Data Source={_databasePath};Pooling=False"));
            services.AddSingleton<ICareerRepository>(
                new JsonCareerRepository(_catalogPath));
            services.AddSingleton<RecommendationInputBuilder>();
            services.AddSingleton<ICareerModelPredictor>(_ =>
                new CareerModelPredictor(_modelPath, _metadataPath));
            services.AddScoped<IStudentProfileRepository,
                StudentProfileRepository>();
            services.AddScoped<IRecommendationRepository,
                RecommendationRepository>();
            services.AddScoped<IRoadmapRepository, RoadmapRepository>();
            services.AddScoped<IAssessmentService, AssessmentService>();
            services.AddScoped<IRecommendationService, RecommendationService>();
            services.AddScoped<ISkillGapService, SkillGapService>();
            services.AddScoped<IRoadmapService, RoadmapService>();
            services.AddScoped<IRecommendationHistoryService,
                RecommendationHistoryService>();
            services.AddScoped<SkillGapResultValidator>();
            services.AddScoped<LearningRoadmapValidator>();
            services.AddScoped<CareerCatalogSynchronizer>();
            services.AddSingleton<TimeProvider>(new FixedTimeProvider());

            var provider = services.BuildServiceProvider(
                new ServiceProviderOptions
                {
                    ValidateOnBuild = true,
                    ValidateScopes = true
                });

            try
            {
                await using var scope = provider.CreateAsyncScope();
                var context = scope.ServiceProvider
                    .GetRequiredService<CareerAdvisorDbContext>();
                await context.Database.MigrateAsync();
                await scope.ServiceProvider
                    .GetRequiredService<CareerCatalogSynchronizer>()
                    .SynchronizeAsync();
                return provider;
            }
            catch
            {
                await provider.DisposeAsync();
                throw;
            }
        }

        public ValueTask DisposeAsync()
        {
            SqliteConnection.ClearAllPools();

            if (Directory.Exists(_directoryPath))
            {
                Directory.Delete(_directoryPath, recursive: true);
            }

            return ValueTask.CompletedTask;
        }

        private static string FindRepositoryRoot()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);

            while (directory is not null)
            {
                if (File.Exists(Path.Combine(
                        directory.FullName,
                        "CareerAdvisor.sln")))
                {
                    return directory.FullName;
                }

                directory = directory.Parent;
            }

            throw new DirectoryNotFoundException(
                "Could not locate the repository root for Sprint 4 tests.");
        }
    }
}
