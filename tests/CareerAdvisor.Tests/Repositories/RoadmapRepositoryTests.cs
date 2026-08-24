using CareerAdvisor.Core.Models;
using CareerAdvisor.Infrastructure.Data;
using CareerAdvisor.Infrastructure.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace CareerAdvisor.Tests.Repositories;

public sealed class RoadmapRepositoryTests
{
    [Fact]
    public async Task AddAsync_PersistsAggregateAndReopensOrderedInNewContext()
    {
        await using var database = await TestDatabase.CreateAsync();
        var roadmap = CreateRoadmap(database.ProfileId, database.CareerId, 3);
        roadmap.Steps.Reverse();

        await using (var writeContext = database.CreateContext())
        {
            Assert.True(await new RoadmapRepository(writeContext)
                .TryAddAsync(roadmap));
        }

        await using var readContext = database.CreateContext();
        var reopened = await new RoadmapRepository(readContext)
            .GetByIdForProfileAsync(database.ProfileId, roadmap.Id);

        Assert.NotNull(reopened);
        Assert.Equal([1, 2, 3], reopened.Steps.Select(step => step.Order));
        Assert.All(reopened.Steps,
            step => Assert.Equal(roadmap.Id, step.LearningRoadmapId));
        Assert.Equal(DateTimeKind.Utc, reopened.CreatedAt.Kind);
        Assert.Empty(readContext.ChangeTracker.Entries());
    }

    [Fact]
    public async Task SetStepCompletionAsync_PersistsCompletionAndUncompletion()
    {
        await using var database = await TestDatabase.CreateAsync();
        var roadmap = CreateRoadmap(database.ProfileId, database.CareerId);
        var stepId = roadmap.Steps[0].Id;

        await using (var context = database.CreateContext())
        {
            Assert.True(await new RoadmapRepository(context)
                .TryAddAsync(roadmap));
        }

        var completedAt = new DateTime(
            2026, 8, 24, 9, 0, 0, DateTimeKind.Utc);

        await using (var updateContext = database.CreateContext())
        {
            var updated = await new RoadmapRepository(updateContext)
                .SetStepCompletionAsync(
                    database.ProfileId,
                    roadmap.Id,
                    stepId,
                    true,
                    completedAt);
            Assert.True(updated);
        }

        await using (var reopenContext = database.CreateContext())
        {
            var reopened = await new RoadmapRepository(reopenContext)
                .GetByIdForProfileAsync(database.ProfileId, roadmap.Id);
            Assert.True(reopened!.Steps[0].IsCompleted);
            Assert.Equal(completedAt, reopened.Steps[0].CompletedAt);
            Assert.Equal(DateTimeKind.Utc,
                reopened.Steps[0].CompletedAt!.Value.Kind);
        }

        await using (var updateContext = database.CreateContext())
        {
            await new RoadmapRepository(updateContext).SetStepCompletionAsync(
                database.ProfileId,
                roadmap.Id,
                stepId,
                false,
                null);
        }

        await using var finalContext = database.CreateContext();
        var final = await new RoadmapRepository(finalContext)
            .GetByIdForProfileAsync(database.ProfileId, roadmap.Id);
        Assert.False(final!.Steps[0].IsCompleted);
        Assert.Null(final.Steps[0].CompletedAt);
    }

    [Fact]
    public async Task ProfileAndRoadmapScopedLookupsAndUpdatesDoNotCrossBoundaries()
    {
        await using var database = await TestDatabase.CreateAsync();
        var roadmap = CreateRoadmap(database.ProfileId, database.CareerId);

        await using (var context = database.CreateContext())
        {
            Assert.True(await new RoadmapRepository(context)
                .TryAddAsync(roadmap));
        }

        await using var readContext = database.CreateContext();
        var repository = new RoadmapRepository(readContext);

        Assert.Null(await repository.GetByIdForProfileAsync(
            Guid.NewGuid(), roadmap.Id));
        Assert.Null(await repository.GetByProfileAndCareerAsync(
            Guid.NewGuid(), database.CareerId));
        Assert.False(await repository.SetStepCompletionAsync(
            Guid.NewGuid(), roadmap.Id, roadmap.Steps[0].Id, true,
            DateTime.UtcNow));
        Assert.False(await repository.SetStepCompletionAsync(
            database.ProfileId, Guid.NewGuid(), roadmap.Steps[0].Id, true,
            DateTime.UtcNow));
        Assert.False(await repository.SetStepCompletionAsync(
            database.ProfileId, roadmap.Id, Guid.NewGuid(), true,
            DateTime.UtcNow));
    }

    [Fact]
    public async Task UniqueProfileCareerAndStepOrderConstraintsAreEnforced()
    {
        await using var database = await TestDatabase.CreateAsync();
        var first = CreateRoadmap(database.ProfileId, database.CareerId);

        await using (var context = database.CreateContext())
        {
            Assert.True(await new RoadmapRepository(context)
                .TryAddAsync(first));
        }

        await using (var duplicateContext = database.CreateContext())
        {
            var duplicate = CreateRoadmap(
                database.ProfileId,
                database.CareerId);
            Assert.False(await new RoadmapRepository(duplicateContext)
                .TryAddAsync(duplicate));
            Assert.DoesNotContain(
                duplicateContext.ChangeTracker.Entries(),
                entry => entry.Entity is LearningRoadmap or RoadmapStep);

            duplicateContext.CareerProfiles.Add(CreateCareer());
            await duplicateContext.SaveChangesAsync();
        }

        await using var contextWithDuplicateOrder = database.CreateContext();
        var secondCareer = CreateCareer();
        contextWithDuplicateOrder.CareerProfiles.Add(secondCareer);
        await contextWithDuplicateOrder.SaveChangesAsync();
        var invalid = CreateRoadmap(database.ProfileId, secondCareer.Id, 2);
        invalid.Steps[1].Order = 1;

        await Assert.ThrowsAsync<DbUpdateException>(() =>
            new RoadmapRepository(contextWithDuplicateOrder)
                .TryAddAsync(invalid));
    }

    [Fact]
    public async Task ConcurrentCompletion_PreservesFirstPersistedTimestamp()
    {
        await using var database = await TestDatabase.CreateAsync();
        var roadmap = CreateRoadmap(database.ProfileId, database.CareerId);

        await using (var seedContext = database.CreateContext())
        {
            Assert.True(await new RoadmapRepository(seedContext)
                .TryAddAsync(roadmap));
        }

        await using var firstContext = database.CreateContext();
        await using var secondContext = database.CreateContext();
        var firstRepository = new RoadmapRepository(firstContext);
        var secondRepository = new RoadmapRepository(secondContext);
        var firstObserved = await firstRepository.GetByIdForProfileAsync(
            database.ProfileId,
            roadmap.Id);
        var secondObserved = await secondRepository.GetByIdForProfileAsync(
            database.ProfileId,
            roadmap.Id);
        Assert.False(firstObserved!.Steps[0].IsCompleted);
        Assert.False(secondObserved!.Steps[0].IsCompleted);

        var firstTimestamp = new DateTime(
            2026, 8, 24, 10, 0, 0, DateTimeKind.Utc);
        var secondTimestamp = firstTimestamp.AddMinutes(1);

        Assert.True(await firstRepository.SetStepCompletionAsync(
            database.ProfileId,
            roadmap.Id,
            roadmap.Steps[0].Id,
            true,
            firstTimestamp));
        Assert.True(await secondRepository.SetStepCompletionAsync(
            database.ProfileId,
            roadmap.Id,
            roadmap.Steps[0].Id,
            true,
            secondTimestamp));

        await using var verifyContext = database.CreateContext();
        var persisted = await new RoadmapRepository(verifyContext)
            .GetByIdForProfileAsync(database.ProfileId, roadmap.Id);
        Assert.True(persisted!.Steps[0].IsCompleted);
        Assert.Equal(firstTimestamp, persisted.Steps[0].CompletedAt);
        Assert.NotEqual(secondTimestamp, persisted.Steps[0].CompletedAt);
    }

    [Fact]
    public async Task IncompleteStepWithTimestamp_IsNotTreatedAsIdempotent()
    {
        await using var database = await TestDatabase.CreateAsync();
        var roadmap = CreateRoadmap(database.ProfileId, database.CareerId);

        await using (var seedContext = database.CreateContext())
        {
            Assert.True(await new RoadmapRepository(seedContext)
                .TryAddAsync(roadmap));
            await seedContext.Database.ExecuteSqlInterpolatedAsync($"""
                UPDATE RoadmapSteps
                SET CompletedAt = {DateTime.UtcNow}
                WHERE Id = {roadmap.Steps[0].Id};
                """);
        }

        await using var updateContext = database.CreateContext();
        var updated = await new RoadmapRepository(updateContext)
            .SetStepCompletionAsync(
                database.ProfileId,
                roadmap.Id,
                roadmap.Steps[0].Id,
                false,
                null);

        Assert.False(updated);
        var completedAt = await updateContext.RoadmapSteps
            .Where(step => step.Id == roadmap.Steps[0].Id)
            .Select(step => step.CompletedAt)
            .SingleAsync();
        Assert.NotNull(completedAt);
    }

    [Fact]
    public async Task FailedStepInsertionLeavesNoPartialAggregate()
    {
        await using var database = await TestDatabase.CreateAsync();
        var roadmap = CreateRoadmap(database.ProfileId, database.CareerId, 2);

        await using (var context = database.CreateContext())
        {
            await context.Database.ExecuteSqlRawAsync(
                """
                CREATE TRIGGER FailSecondRoadmapStep
                BEFORE INSERT ON RoadmapSteps
                WHEN NEW."Order" = 2
                BEGIN
                    SELECT RAISE(ABORT, 'injected step failure');
                END;
                """);

            await Assert.ThrowsAsync<DbUpdateException>(() =>
                new RoadmapRepository(context).TryAddAsync(roadmap));
        }

        await using var verifyContext = database.CreateContext();
        Assert.Empty(await verifyContext.LearningRoadmaps.ToListAsync());
        Assert.Empty(await verifyContext.RoadmapSteps.ToListAsync());
    }

    [Fact]
    public async Task DeleteBehaviors_CascadeProfileAndRoadmapAndRestrictCareer()
    {
        await using var database = await TestDatabase.CreateAsync();
        var roadmap = CreateRoadmap(database.ProfileId, database.CareerId, 2);

        await using (var context = database.CreateContext())
        {
            Assert.True(await new RoadmapRepository(context)
                .TryAddAsync(roadmap));
        }

        await using (var restrictContext = database.CreateContext())
        {
            var career = await restrictContext.CareerProfiles.SingleAsync();
            restrictContext.CareerProfiles.Remove(career);
            await Assert.ThrowsAsync<DbUpdateException>(
                () => restrictContext.SaveChangesAsync());
        }

        await using (var roadmapDeleteContext = database.CreateContext())
        {
            var saved = await roadmapDeleteContext.LearningRoadmaps
                .SingleAsync();
            roadmapDeleteContext.LearningRoadmaps.Remove(saved);
            await roadmapDeleteContext.SaveChangesAsync();
        }

        await using (var verifySteps = database.CreateContext())
        {
            Assert.Empty(await verifySteps.RoadmapSteps.ToListAsync());
        }

        var second = CreateRoadmap(database.ProfileId, database.CareerId);
        await using (var addContext = database.CreateContext())
        {
            Assert.True(await new RoadmapRepository(addContext)
                .TryAddAsync(second));
        }

        await using (var profileDeleteContext = database.CreateContext())
        {
            var profile = await profileDeleteContext.StudentProfiles
                .SingleAsync();
            profileDeleteContext.StudentProfiles.Remove(profile);
            await profileDeleteContext.SaveChangesAsync();
        }

        await using var finalContext = database.CreateContext();
        Assert.Empty(await finalContext.LearningRoadmaps.ToListAsync());
        Assert.Empty(await finalContext.RoadmapSteps.ToListAsync());
    }

    private static LearningRoadmap CreateRoadmap(
        Guid profileId,
        Guid careerId,
        int stepCount = 1)
    {
        var roadmap = new LearningRoadmap
        {
            Id = Guid.NewGuid(),
            StudentProfileId = profileId,
            CareerProfileId = careerId,
            CreatedAt = new DateTime(
                2026, 8, 24, 8, 0, 0, DateTimeKind.Utc)
        };
        roadmap.Steps = Enumerable.Range(1, stepCount)
            .Select(order => new RoadmapStep
            {
                Id = Guid.NewGuid(),
                LearningRoadmapId = roadmap.Id,
                Order = order,
                Title = $"Step {order}",
                Description = $"Description {order}",
                ResourceLink = string.Empty
            })
            .ToList();
        return roadmap;
    }

    private static CareerProfile CreateCareer() => new()
    {
        Id = Guid.NewGuid(),
        Code = $"T{Guid.NewGuid():N}"[..20],
        Title = "Test career",
        Description = "Test career description."
    };

    private sealed class TestDatabase : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly DbContextOptions<CareerAdvisorDbContext> _options;

        private TestDatabase(
            SqliteConnection connection,
            DbContextOptions<CareerAdvisorDbContext> options,
            Guid profileId,
            Guid careerId)
        {
            _connection = connection;
            _options = options;
            ProfileId = profileId;
            CareerId = careerId;
        }

        public Guid ProfileId { get; }
        public Guid CareerId { get; }

        public static async Task<TestDatabase> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var options = new DbContextOptionsBuilder<CareerAdvisorDbContext>()
                .UseSqlite(connection)
                .Options;
            var profile = new StudentProfile
            {
                Id = Guid.NewGuid(),
                Name = "Ama Mensah",
                Programme = "Computer Science"
            };
            var career = CreateCareer();

            await using (var context = new CareerAdvisorDbContext(options))
            {
                await context.Database.EnsureCreatedAsync();
                context.StudentProfiles.Add(profile);
                context.CareerProfiles.Add(career);
                await context.SaveChangesAsync();
            }

            return new TestDatabase(
                connection, options, profile.Id, career.Id);
        }

        public CareerAdvisorDbContext CreateContext() => new(_options);

        public async ValueTask DisposeAsync()
        {
            await _connection.DisposeAsync();
        }
    }
}
