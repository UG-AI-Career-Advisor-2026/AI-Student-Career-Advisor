using CareerAdvisor.Core.Enums;
using CareerAdvisor.Core.Interfaces;
using CareerAdvisor.Core.Models;
using CareerAdvisor.Core.Validators;
using CareerAdvisor.Infrastructure.Data;
using CareerAdvisor.Infrastructure.Repositories;
using CareerAdvisor.Infrastructure.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace CareerAdvisor.Tests.Integration;

public sealed class LearningRoadmapPersistenceTests
{
    private const string PreviousMigration =
        "20260819002937_StabilizeRecommendationPersistence";

    private static readonly DateTime CreatedUtc =
        new(2026, 8, 24, 14, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task FullMigrationChain_HasExpectedRoadmapSchemaAndNoPendingChanges()
    {
        await using var database = await MigratedDatabase.CreateAsync();
        await using var context = database.CreateContext();

        Assert.Empty(await context.Database.GetPendingMigrationsAsync());
        Assert.False(context.Database.HasPendingModelChanges());

        var roadmapColumns = await ReadPragmaAsync(
            context, "PRAGMA table_info('LearningRoadmaps');");
        Assert.Contains(roadmapColumns,
            row => row.Contains("CreatedAt|TEXT|1", StringComparison.Ordinal));

        var stepColumns = await ReadPragmaAsync(
            context, "PRAGMA table_info('RoadmapSteps');");
        Assert.Contains(stepColumns,
            row => row.Contains("LearningRoadmapId|TEXT|1",
                StringComparison.Ordinal));
        Assert.Contains(stepColumns,
            row => row.Contains("CompletedAt|TEXT|0",
                StringComparison.Ordinal));

        var roadmapIndexes = await ReadPragmaAsync(
            context, "PRAGMA index_list('LearningRoadmaps');");
        Assert.Contains(roadmapIndexes,
            row => row.Contains(
                "IX_LearningRoadmaps_StudentProfileId_CareerProfileId|1",
                StringComparison.Ordinal));

        var stepIndexes = await ReadPragmaAsync(
            context, "PRAGMA index_list('RoadmapSteps');");
        Assert.Contains(stepIndexes,
            row => row.Contains(
                "IX_RoadmapSteps_LearningRoadmapId_Order|1",
                StringComparison.Ordinal));

        var roadmapIndexColumns = await ReadPragmaAsync(
            context,
            "PRAGMA index_info(" +
            "'IX_LearningRoadmaps_StudentProfileId_CareerProfileId');");
        Assert.Equal(
            ["StudentProfileId", "CareerProfileId"],
            roadmapIndexColumns.Select(row => row.Split('|')[2]));

        var stepIndexColumns = await ReadPragmaAsync(
            context,
            "PRAGMA index_info(" +
            "'IX_RoadmapSteps_LearningRoadmapId_Order');");
        Assert.Equal(
            ["LearningRoadmapId", "Order"],
            stepIndexColumns.Select(row => row.Split('|')[2]));

        var roadmapForeignKeys = await ReadPragmaAsync(
            context, "PRAGMA foreign_key_list('LearningRoadmaps');");
        AssertForeignKey(
            roadmapForeignKeys,
            "StudentProfileId",
            "StudentProfiles",
            "Id",
            "CASCADE");
        AssertForeignKey(
            roadmapForeignKeys,
            "CareerProfileId",
            "CareerProfiles",
            "Id",
            "RESTRICT");

        var stepForeignKeys = await ReadPragmaAsync(
            context, "PRAGMA foreign_key_list('RoadmapSteps');");
        AssertForeignKey(
            stepForeignKeys,
            "LearningRoadmapId",
            "LearningRoadmaps",
            "Id",
            "CASCADE");
    }

    [Fact]
    public async Task Migration_BackfillsLegacyTimestampsWithoutChangingProgress()
    {
        await using var database = await MigratedDatabase.CreateAsync(
            migrateToLatest: false);
        await using var context = database.CreateContext();
        var migrator = context.GetService<IMigrator>();
        await migrator.MigrateAsync(PreviousMigration);

        var profileId = Guid.NewGuid();
        var careerId = Guid.NewGuid();
        var roadmapId = Guid.NewGuid();
        var completedStepId = Guid.NewGuid();
        var incompleteStepId = Guid.NewGuid();

        await InsertLegacyParentsAsync(context, profileId, careerId);
        await context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO LearningRoadmaps
                (Id, StudentProfileId, CareerProfileId)
            VALUES ({roadmapId}, {profileId}, {careerId});
            """);
        await InsertLegacyStepAsync(
            context, completedStepId, roadmapId, 1, true);
        await InsertLegacyStepAsync(
            context, incompleteStepId, roadmapId, 2, false);

        await migrator.MigrateAsync();

        context.ChangeTracker.Clear();
        var reopened = await new RoadmapRepository(context)
            .GetByIdForProfileAsync(profileId, roadmapId);

        Assert.NotNull(reopened);
        var expected = new DateTime(
            2026, 8, 24, 0, 0, 0, DateTimeKind.Utc);
        Assert.Equal(expected, reopened.CreatedAt);
        Assert.Equal(expected, reopened.Steps[0].CompletedAt);
        Assert.True(reopened.Steps[0].IsCompleted);
        Assert.Null(reopened.Steps[1].CompletedAt);
        Assert.False(reopened.Steps[1].IsCompleted);
    }

    [Theory]
    [InlineData(LegacyMalformedCase.OrphanStep, 0, 1, 1299)]
    [InlineData(LegacyMalformedCase.DuplicateRoadmaps, 2, 0, 2067)]
    [InlineData(LegacyMalformedCase.DuplicateStepOrders, 1, 2, 2067)]
    public async Task Migration_MalformedLegacyData_RollsBackCompletely(
        LegacyMalformedCase malformedCase,
        int expectedRoadmaps,
        int expectedSteps,
        int expectedExtendedErrorCode)
    {
        await using var database = await MigratedDatabase.CreateAsync(
            migrateToLatest: false);
        await using var context = database.CreateContext();
        var migrator = context.GetService<IMigrator>();
        await migrator.MigrateAsync(PreviousMigration);

        var profileId = Guid.NewGuid();
        var careerId = Guid.NewGuid();
        var roadmapId = Guid.NewGuid();

        switch (malformedCase)
        {
            case LegacyMalformedCase.OrphanStep:
                await context.Database.ExecuteSqlInterpolatedAsync($"""
                    INSERT INTO RoadmapSteps
                        (Id, "Order", Title, Description, ResourceLink,
                         IsCompleted, LearningRoadmapId)
                    VALUES
                        ({Guid.NewGuid()}, 1, {'O' + "rphan"},
                         {'O' + "rphan step"}, {string.Empty}, 0, NULL);
                    """);
                break;

            case LegacyMalformedCase.DuplicateRoadmaps:
                await InsertLegacyParentsAsync(context, profileId, careerId);
                await InsertLegacyRoadmapAsync(
                    context, roadmapId, profileId, careerId);
                await InsertLegacyRoadmapAsync(
                    context, Guid.NewGuid(), profileId, careerId);
                break;

            case LegacyMalformedCase.DuplicateStepOrders:
                await InsertLegacyParentsAsync(context, profileId, careerId);
                await InsertLegacyRoadmapAsync(
                    context, roadmapId, profileId, careerId);
                await InsertLegacyStepAsync(
                    context, Guid.NewGuid(), roadmapId, 1, false);
                await InsertLegacyStepAsync(
                    context, Guid.NewGuid(), roadmapId, 1, false);
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(malformedCase));
        }

        var exception = await Assert.ThrowsAsync<SqliteException>(
            () => migrator.MigrateAsync());
        Assert.Equal(19, exception.SqliteErrorCode);
        Assert.Equal(
            expectedExtendedErrorCode,
            exception.SqliteExtendedErrorCode);

        await using var connection = await database.OpenConnectionAsync();
        Assert.Equal(
            1L,
            await ExecuteScalarAsync<long>(connection, $"""
                SELECT COUNT(*) FROM __EFMigrationsHistory
                WHERE MigrationId = '{PreviousMigration}';
                """));
        Assert.Equal(
            0L,
            await ExecuteScalarAsync<long>(connection, """
                SELECT COUNT(*) FROM __EFMigrationsHistory
                WHERE MigrationId =
                    '20260824182741_ImplementLearningRoadmapPersistence';
                """));
        Assert.Equal(
            expectedRoadmaps,
            await ExecuteScalarAsync<long>(
                connection,
                "SELECT COUNT(*) FROM LearningRoadmaps;"));
        Assert.Equal(
            expectedSteps,
            await ExecuteScalarAsync<long>(
                connection,
                "SELECT COUNT(*) FROM RoadmapSteps;"));
        Assert.Equal(
            0L,
            await ExecuteScalarAsync<long>(connection, """
                SELECT COUNT(*) FROM sqlite_master
                WHERE type = 'table' AND name LIKE 'ef_temp_%';
                """));

        var roadmapColumns = await ReadPragmaAsync(
            connection,
            "PRAGMA table_info('LearningRoadmaps');");
        Assert.DoesNotContain(roadmapColumns,
            row => row.Split('|')[1] == "CreatedAt");
        var stepColumns = await ReadPragmaAsync(
            connection,
            "PRAGMA table_info('RoadmapSteps');");
        Assert.DoesNotContain(stepColumns,
            row => row.Split('|')[1] == "CompletedAt");
        Assert.Contains(stepColumns, row =>
        {
            var fields = row.Split('|');
            return fields[1] == "LearningRoadmapId" && fields[3] == "0";
        });

        var roadmapIndexes = await ReadPragmaAsync(
            connection,
            "PRAGMA index_list('LearningRoadmaps');");
        Assert.Contains(roadmapIndexes,
            row => row.Contains(
                "IX_LearningRoadmaps_StudentProfileId|0",
                StringComparison.Ordinal));
        Assert.Contains(roadmapIndexes,
            row => row.Contains(
                "IX_LearningRoadmaps_CareerProfileId|0",
                StringComparison.Ordinal));
        Assert.DoesNotContain(roadmapIndexes,
            row => row.Contains(
                "IX_LearningRoadmaps_StudentProfileId_CareerProfileId",
                StringComparison.Ordinal));
        var stepIndexes = await ReadPragmaAsync(
            connection,
            "PRAGMA index_list('RoadmapSteps');");
        Assert.Contains(stepIndexes,
            row => row.Contains(
                "IX_RoadmapSteps_LearningRoadmapId|0",
                StringComparison.Ordinal));
        Assert.DoesNotContain(stepIndexes,
            row => row.Contains(
                "IX_RoadmapSteps_LearningRoadmapId_Order",
                StringComparison.Ordinal));

        Assert.Equal(
            ["StudentProfileId"],
            (await ReadPragmaAsync(
                connection,
                "PRAGMA index_info('IX_LearningRoadmaps_StudentProfileId');"))
            .Select(row => row.Split('|')[2]));
        Assert.Equal(
            ["CareerProfileId"],
            (await ReadPragmaAsync(
                connection,
                "PRAGMA index_info('IX_LearningRoadmaps_CareerProfileId');"))
            .Select(row => row.Split('|')[2]));
        Assert.Equal(
            ["LearningRoadmapId"],
            (await ReadPragmaAsync(
                connection,
                "PRAGMA index_info('IX_RoadmapSteps_LearningRoadmapId');"))
            .Select(row => row.Split('|')[2]));

        var roadmapForeignKeys = await ReadPragmaAsync(
            connection,
            "PRAGMA foreign_key_list('LearningRoadmaps');");
        AssertForeignKey(
            roadmapForeignKeys,
            "StudentProfileId",
            "StudentProfiles",
            "Id",
            "CASCADE");
        AssertForeignKey(
            roadmapForeignKeys,
            "CareerProfileId",
            "CareerProfiles",
            "Id",
            "RESTRICT");

        var stepForeignKeys = await ReadPragmaAsync(
            connection,
            "PRAGMA foreign_key_list('RoadmapSteps');");
        AssertForeignKey(
            stepForeignKeys,
            "LearningRoadmapId",
            "LearningRoadmaps",
            "Id",
            "CASCADE");

        if (malformedCase == LegacyMalformedCase.DuplicateStepOrders)
        {
            var orders = await ReadSingleColumnAsync<long>(
                connection,
                "SELECT \"Order\" FROM RoadmapSteps ORDER BY rowid;");
            Assert.Equal([1L, 1L], orders);
        }
    }

    [Fact]
    public async Task GeneratedRoadmapAndProgressReopenAcrossFreshContexts()
    {
        await using var database = await MigratedDatabase.CreateAsync();
        var profile = CreateProfile();
        var careers = CreateCareers();

        await using (var seedContext = database.CreateContext())
        {
            seedContext.StudentProfiles.Add(profile);
            seedContext.CareerProfiles.AddRange(careers);
            await seedContext.SaveChangesAsync();

            await new RecommendationRepository(seedContext).AddAsync(
                CreateRecommendationSession(profile.Id, careers));
        }

        var selectedCareer = careers[0];
        LearningRoadmap generated;

        await using (var generationContext = database.CreateContext())
        {
            generated = await CreateRoadmapService(
                    generationContext,
                    selectedCareer,
                    CreateSkillGap(profile.Id, selectedCareer))
                .GenerateRoadmapAsync(profile.Id, selectedCareer.Id);
        }

        Assert.Equal(12, generated.Steps.Count);
        var stepId = generated.Steps[0].Id;

        LearningRoadmap firstDetached;
        LearningRoadmap secondDetached;

        await using (var firstReadContext = database.CreateContext())
        {
            firstDetached = await CreateRoadmapService(
                    firstReadContext,
                    selectedCareer,
                    new UnexpectedSkillGapService())
                .GetRoadmapAsync(profile.Id, generated.Id);
        }

        await using (var secondReadContext = database.CreateContext())
        {
            secondDetached = await CreateRoadmapService(
                    secondReadContext,
                    selectedCareer,
                    new UnexpectedSkillGapService())
                .GetRoadmapAsync(profile.Id, generated.Id);
        }

        Assert.NotSame(firstDetached, secondDetached);
        Assert.NotSame(firstDetached.Steps, secondDetached.Steps);
        Assert.All(firstDetached.Steps.Zip(secondDetached.Steps), pair =>
        {
            Assert.NotSame(pair.First, pair.Second);
            Assert.Equal(pair.First.Id, pair.Second.Id);
            Assert.Equal(pair.First.Order, pair.Second.Order);
            Assert.Equal(pair.First.Title, pair.Second.Title);
            Assert.Equal(pair.First.Description, pair.Second.Description);
        });

        var persistedFirstTitle = secondDetached.Steps[0].Title;
        firstDetached.Steps[0].Title = "Detached local mutation";

        await using (var unchangedContext = database.CreateContext())
        {
            var unchanged = await CreateRoadmapService(
                    unchangedContext,
                    selectedCareer,
                    new UnexpectedSkillGapService())
                .GetRoadmapAsync(profile.Id, generated.Id);
            Assert.Equal(persistedFirstTitle, unchanged.Steps[0].Title);
        }

        await using (var progressContext = database.CreateContext())
        {
            var completed = await CreateRoadmapService(
                    progressContext,
                    selectedCareer,
                    new UnexpectedSkillGapService())
                .UpdateRoadmapProgressAsync(
                    profile.Id,
                    generated.Id,
                    stepId,
                    true);
            Assert.Equal(CreatedUtc, completed.Steps[0].CompletedAt);
        }

        await using var restartContext = database.CreateContext();
        var reopened = await CreateRoadmapService(
                restartContext,
                selectedCareer,
                new UnexpectedSkillGapService())
            .GetRoadmapAsync(profile.Id, generated.Id);

        Assert.Equal(Enumerable.Range(1, reopened.Steps.Count),
            reopened.Steps.Select(step => step.Order));
        Assert.True(reopened.Steps[0].IsCompleted);
        Assert.Equal(CreatedUtc, reopened.Steps[0].CompletedAt);
        Assert.Equal(DateTimeKind.Utc, reopened.CreatedAt.Kind);
    }

    [Fact]
    public async Task ConcurrentGeneration_ReturnsOnePersistedAggregateToBothCallers()
    {
        await using var database = await MigratedDatabase.CreateAsync();
        var profile = CreateProfile();
        var careers = CreateCareers();

        await using (var seedContext = database.CreateContext())
        {
            seedContext.StudentProfiles.Add(profile);
            seedContext.CareerProfiles.AddRange(careers);
            await seedContext.SaveChangesAsync();
            await new RecommendationRepository(seedContext).AddAsync(
                CreateRecommendationSession(profile.Id, careers));
        }

        var career = careers[0];
        var coordinator = new GenerationCoordinator();
        await using var firstContext = database.CreateContext();
        await using var secondContext = database.CreateContext();
        var firstRepository = new CoordinatedRoadmapRepository(
            new RoadmapRepository(firstContext),
            coordinator,
            isFirstWriter: true);
        var secondRepository = new CoordinatedRoadmapRepository(
            new RoadmapRepository(secondContext),
            coordinator,
            isFirstWriter: false);
        var firstService = CreateRoadmapService(
            firstContext,
            career,
            CreateSkillGap(profile.Id, career),
            firstRepository);
        var secondService = CreateRoadmapService(
            secondContext,
            career,
            CreateSkillGap(profile.Id, career),
            secondRepository);

        var firstTask = firstService.GenerateRoadmapAsync(
            profile.Id,
            career.Id);
        var secondTask = secondService.GenerateRoadmapAsync(
            profile.Id,
            career.Id);
        var results = await Task.WhenAll(firstTask, secondTask);

        Assert.Equal(results[0].Id, results[1].Id);
        Assert.Equal(
            results[0].Steps.Select(StepValue),
            results[1].Steps.Select(StepValue));
        Assert.Equal(Enumerable.Range(1, results[0].Steps.Count),
            results[0].Steps.Select(step => step.Order));

        await using var verifyContext = database.CreateContext();
        var persisted = await verifyContext.LearningRoadmaps
            .AsNoTracking()
            .Include(roadmap => roadmap.Steps)
            .SingleAsync();
        Assert.Equal(results[0].Id, persisted.Id);
        Assert.Equal(results[0].Steps.Count, persisted.Steps.Count);
        Assert.Equal(
            persisted.Steps.Count,
            persisted.Steps.Select(step => step.Order).Distinct().Count());

        var reopened = await new RoadmapRepository(verifyContext)
            .GetByIdForProfileAsync(profile.Id, persisted.Id);
        Assert.NotNull(reopened);
        Assert.Equal(
            results[0].Steps.Select(StepValue),
            reopened.Steps.Select(StepValue));
    }

    private static RoadmapService CreateRoadmapService(
        CareerAdvisorDbContext context,
        CareerProfile career,
        ISkillGapService skillGapService,
        IRoadmapRepository? roadmapRepository = null)
    {
        return new RoadmapService(
            new StudentProfileRepository(context),
            new CareerRepositoryDouble(career),
            new RecommendationRepository(context),
            roadmapRepository ?? new RoadmapRepository(context),
            skillGapService,
            new SkillGapResultValidator(),
            new LearningRoadmapValidator(),
            new FixedTimeProvider(CreatedUtc));
    }

    private static StudentProfile CreateProfile() => new()
    {
        Id = Guid.NewGuid(),
        Name = "Ama Mensah",
        Programme = "Computer Science"
    };

    private static List<CareerProfile> CreateCareers()
    {
        return Enumerable.Range(1, 3).Select(number => new CareerProfile
        {
            Id = Guid.NewGuid(),
            Code = $"TEST-{number}",
            Title = $"Test Career {number}",
            Description = $"Description {number}",
            RequiredSkills =
                ["Skill A", "Skill B", "Skill C", "Skill D", "Skill E", "Skill F"],
            SuggestedLearningTopics =
                ["Topic A", "Topic B", "Topic C", "Topic D"],
            RecommendedCertifications =
                ["Certification A", "Certification B"]
        }).ToList();
    }

    private static RecommendationSession CreateRecommendationSession(
        Guid profileId,
        IReadOnlyList<CareerProfile> careers)
    {
        var sessionId = Guid.NewGuid();
        return new RecommendationSession
        {
            Id = sessionId,
            StudentProfileId = profileId,
            Recommendations = careers.Select(career =>
                new CareerRecommendation
                {
                    Id = Guid.NewGuid(),
                    RecommendationSessionId = sessionId,
                    CareerProfileId = career.Id,
                    MatchScore = 0.8,
                    Reasoning = "Persisted recommendation explanation."
                }).ToList()
        };
    }

    private static ISkillGapService CreateSkillGap(
        Guid profileId,
        CareerProfile career)
    {
        return new SkillGapServiceDouble(new SkillGapResult
        {
            StudentProfileId = profileId,
            CareerProfileId = career.Id,
            CareerCode = career.Code,
            CareerTitle = career.Title,
            BaselineProficiency = SkillProficiency.Intermediate,
            Items = career.RequiredSkills.Select(skill => new SkillGapItem
            {
                RequiredSkillName = skill,
                Classification = SkillGapClassification.Missing
            }).ToList()
        });
    }

    private static async Task InsertLegacyParentsAsync(
        CareerAdvisorDbContext context,
        Guid profileId,
        Guid careerId)
    {
        await context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO StudentProfiles
                (Id, Name, Programme, AcademicLevel, Interests,
                 CreatedAt, UpdatedAt)
            VALUES
                ({profileId}, {'A' + "ma"}, {'C' + "omputer Science"}, 0,
                 {'[' + "]"}, {CreatedUtc}, {CreatedUtc});
            """);
        await context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO CareerProfiles
                (Id, Code, Title, Description, RequiredSkills,
                 RecommendedCertifications, SuggestedLearningTopics)
            VALUES
                ({careerId}, {'L' + "EGACY"}, {'L' + "egacy Career"},
                 {'L' + "egacy description"}, {'[' + "]"}, {'[' + "]"},
                 {'[' + "]"});
            """);
    }

    private static Task<int> InsertLegacyStepAsync(
        CareerAdvisorDbContext context,
        Guid stepId,
        Guid roadmapId,
        int order,
        bool completed)
    {
        return context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO RoadmapSteps
                (Id, "Order", Title, Description, ResourceLink,
                 IsCompleted, LearningRoadmapId)
            VALUES
                ({stepId}, {order}, {"Legacy step " + order},
                 {"Legacy description " + order}, {string.Empty},
                 {completed}, {roadmapId});
            """);
    }

    private static Task<int> InsertLegacyRoadmapAsync(
        CareerAdvisorDbContext context,
        Guid roadmapId,
        Guid profileId,
        Guid careerId)
    {
        return context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO LearningRoadmaps
                (Id, StudentProfileId, CareerProfileId)
            VALUES ({roadmapId}, {profileId}, {careerId});
            """);
    }

    private static async Task<List<string>> ReadPragmaAsync(
        CareerAdvisorDbContext context,
        string sql)
    {
        var connection = context.Database.GetDbConnection();
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await using var reader = await command.ExecuteReaderAsync();
        var rows = new List<string>();

        while (await reader.ReadAsync())
        {
            var values = Enumerable.Range(0, reader.FieldCount)
                .Select(index => reader.GetValue(index)?.ToString() ?? "")
                .ToArray();
            rows.Add(string.Join('|', values));
        }

        return rows;
    }

    private static async Task<List<string>> ReadPragmaAsync(
        SqliteConnection connection,
        string sql)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await using var reader = await command.ExecuteReaderAsync();
        var rows = new List<string>();

        while (await reader.ReadAsync())
        {
            var values = Enumerable.Range(0, reader.FieldCount)
                .Select(index => reader.GetValue(index)?.ToString() ?? "")
                .ToArray();
            rows.Add(string.Join('|', values));
        }

        return rows;
    }

    private static async Task<T> ExecuteScalarAsync<T>(
        SqliteConnection connection,
        string sql)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        var value = await command.ExecuteScalarAsync();
        return (T)Convert.ChangeType(value!, typeof(T));
    }

    private static async Task<List<T>> ReadSingleColumnAsync<T>(
        SqliteConnection connection,
        string sql)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await using var reader = await command.ExecuteReaderAsync();
        var values = new List<T>();

        while (await reader.ReadAsync())
        {
            values.Add((T)Convert.ChangeType(reader.GetValue(0), typeof(T)));
        }

        return values;
    }

    private static void AssertForeignKey(
        IEnumerable<string> rows,
        string sourceColumn,
        string targetTable,
        string targetColumn,
        string onDelete)
    {
        Assert.Contains(rows, row =>
        {
            var fields = row.Split('|');
            return fields.Length >= 8 &&
                fields[2] == targetTable &&
                fields[3] == sourceColumn &&
                fields[4] == targetColumn &&
                fields[6] == onDelete;
        });
    }

    private static (int, string, string, string) StepValue(RoadmapStep step) =>
        (step.Order, step.Title, step.Description, step.ResourceLink);

    private sealed class MigratedDatabase : IAsyncDisposable
    {
        private readonly string _path;
        private readonly DbContextOptions<CareerAdvisorDbContext> _options;

        private MigratedDatabase(
            string path,
            DbContextOptions<CareerAdvisorDbContext> options)
        {
            _path = path;
            _options = options;
        }

        public static async Task<MigratedDatabase> CreateAsync(
            bool migrateToLatest = true)
        {
            var path = Path.Combine(
                Path.GetTempPath(),
                $"careeriq-roadmap-{Guid.NewGuid():N}.db");
            var options = new DbContextOptionsBuilder<CareerAdvisorDbContext>()
                .UseSqlite($"Data Source={path}")
                .Options;
            var database = new MigratedDatabase(path, options);

            if (migrateToLatest)
            {
                await using var context = database.CreateContext();
                await context.Database.MigrateAsync();
            }

            return database;
        }

        public CareerAdvisorDbContext CreateContext() => new(_options);

        public async Task<SqliteConnection> OpenConnectionAsync()
        {
            var connection = new SqliteConnection($"Data Source={_path}");
            await connection.OpenAsync();
            return connection;
        }

        public ValueTask DisposeAsync()
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(_path)) File.Delete(_path);
            return ValueTask.CompletedTask;
        }
    }

    private sealed class CareerRepositoryDouble(CareerProfile career)
        : ICareerRepository
    {
        public Task<CareerProfile?> GetByIdAsync(Guid id) =>
            Task.FromResult(id == career.Id ? career : null);
        public Task<CareerProfile?> GetByCodeAsync(string code) =>
            Task.FromResult(code == career.Code ? career : null);
        public Task<IEnumerable<CareerProfile>> GetAllAsync() =>
            Task.FromResult<IEnumerable<CareerProfile>>([career]);
        public Task AddAsync(CareerProfile entity) => throw new NotSupportedException();
        public Task UpdateAsync(CareerProfile entity) => throw new NotSupportedException();
        public Task DeleteAsync(Guid id) => throw new NotSupportedException();
    }

    private sealed class SkillGapServiceDouble(SkillGapResult result)
        : ISkillGapService
    {
        public Task<SkillGapResult> AnalyzeAsync(
            Guid studentProfileId, Guid careerProfileId) =>
            Task.FromResult(result);
    }

    private sealed class UnexpectedSkillGapService : ISkillGapService
    {
        public Task<SkillGapResult> AnalyzeAsync(
            Guid studentProfileId, Guid careerProfileId) =>
            throw new InvalidOperationException(
                "Skill-gap analysis should not run while reopening progress.");
    }

    private sealed class FixedTimeProvider(DateTime utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(utcNow);
    }

    public enum LegacyMalformedCase
    {
        OrphanStep,
        DuplicateRoadmaps,
        DuplicateStepOrders
    }

    private sealed class GenerationCoordinator
    {
        private int _lookupCount;

        public TaskCompletionSource BothLookupsReached { get; } = new(
            TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource FirstInsertFinished { get; } = new(
            TaskCreationOptions.RunContinuationsAsynchronously);

        public void RecordLookup()
        {
            if (Interlocked.Increment(ref _lookupCount) == 2)
            {
                BothLookupsReached.TrySetResult();
            }
        }
    }

    private sealed class CoordinatedRoadmapRepository(
        IRoadmapRepository inner,
        GenerationCoordinator coordinator,
        bool isFirstWriter) : IRoadmapRepository
    {
        private bool _initialLookupObserved;

        public Task<LearningRoadmap?> GetByIdForProfileAsync(
            Guid studentProfileId,
            Guid roadmapId) => inner.GetByIdForProfileAsync(
                studentProfileId,
                roadmapId);

        public async Task<LearningRoadmap?> GetByProfileAndCareerAsync(
            Guid studentProfileId,
            Guid careerProfileId)
        {
            var roadmap = await inner.GetByProfileAndCareerAsync(
                studentProfileId,
                careerProfileId);

            if (roadmap is null && !_initialLookupObserved)
            {
                _initialLookupObserved = true;
                coordinator.RecordLookup();
                await coordinator.BothLookupsReached.Task;
            }

            return roadmap;
        }

        public async Task<bool> TryAddAsync(LearningRoadmap roadmap)
        {
            if (!isFirstWriter)
            {
                await coordinator.FirstInsertFinished.Task;
                return await inner.TryAddAsync(roadmap);
            }

            try
            {
                return await inner.TryAddAsync(roadmap);
            }
            finally
            {
                coordinator.FirstInsertFinished.TrySetResult();
            }
        }

        public Task<bool> SetStepCompletionAsync(
            Guid studentProfileId,
            Guid roadmapId,
            Guid stepId,
            bool isCompleted,
            DateTime? completedAt) => inner.SetStepCompletionAsync(
                studentProfileId,
                roadmapId,
                stepId,
                isCompleted,
                completedAt);
    }
}
