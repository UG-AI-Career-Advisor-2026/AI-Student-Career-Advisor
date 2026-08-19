using CareerAdvisor.Core.Careers;
using CareerAdvisor.Core.Models;
using CareerAdvisor.Infrastructure.Data;
using CareerAdvisor.Infrastructure.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace CareerAdvisor.Tests.Data;

public class CareerCatalogSynchronizerTests
{
    [Fact]
    public async Task SynchronizeAsync_PersistsAllEightCareersWithStableIds()
    {
        await using var connection =
            new SqliteConnection("Data Source=:memory:");

        await connection.OpenAsync();
        var options = CreateOptions(connection);

        await using (var writeContext =
                     new CareerAdvisorDbContext(options))
        {
            await writeContext.Database.EnsureCreatedAsync();

            var synchronizer = CreateSynchronizer(writeContext);
            await synchronizer.SynchronizeAsync();
        }

        // A separate context simulates reopening the application.
        await using var readContext =
            new CareerAdvisorDbContext(options);

        var savedCareers = await readContext.CareerProfiles
            .AsNoTracking()
            .OrderBy(career => career.Code)
            .ToListAsync();

        Assert.Equal(8, savedCareers.Count);

        Assert.All(
            savedCareers,
            career =>
            {
                Assert.True(
                    CareerCatalogIdentity.TryGetId(
                        career.Code,
                        out var expectedId));

                Assert.Equal(expectedId, career.Id);
            });

        Assert.Equal(
            8,
            savedCareers
                .Select(career => career.Code)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count());
    }

    [Fact]
    public async Task SynchronizeAsync_RepeatedCallDoesNotCreateDuplicates()
    {
        await using var connection =
            new SqliteConnection("Data Source=:memory:");

        await connection.OpenAsync();
        var options = CreateOptions(connection);

        await using var context =
            new CareerAdvisorDbContext(options);

        await context.Database.EnsureCreatedAsync();

        var synchronizer = CreateSynchronizer(context);

        await synchronizer.SynchronizeAsync();
        await synchronizer.SynchronizeAsync();

        var savedCareers = await context.CareerProfiles
            .AsNoTracking()
            .ToListAsync();

        Assert.Equal(8, savedCareers.Count);
    }

    [Fact]
    public async Task SynchronizeAsync_UpdatesDetailsWithoutChangingStableId()
    {
        await using var connection =
            new SqliteConnection("Data Source=:memory:");

        await connection.OpenAsync();
        var options = CreateOptions(connection);

        var stableId = CareerCatalogIdentity.GetId("SD-001");

        await using (var setupContext =
                     new CareerAdvisorDbContext(options))
        {
            await setupContext.Database.EnsureCreatedAsync();

            setupContext.CareerProfiles.Add(
                new CareerProfile
                {
                    Id = stableId,
                    Code = "SD-001",
                    Title = "Outdated title",
                    Description = "Outdated description",
                    RequiredSkills = ["Outdated skill"],
                    RecommendedCertifications = ["Outdated certificate"],
                    SuggestedLearningTopics = ["Outdated topic"]
                });

            await setupContext.SaveChangesAsync();
        }

        await using (var synchronizationContext =
                     new CareerAdvisorDbContext(options))
        {
            var synchronizer =
                CreateSynchronizer(synchronizationContext);

            await synchronizer.SynchronizeAsync();
        }

        await using var readContext =
            new CareerAdvisorDbContext(options);

        var savedCareer = await readContext.CareerProfiles
            .AsNoTracking()
            .SingleAsync(career => career.Code == "SD-001");

        Assert.Equal(stableId, savedCareer.Id);
        Assert.Equal("Software Developer", savedCareer.Title);
        Assert.NotEqual(
            "Outdated description",
            savedCareer.Description);
        Assert.DoesNotContain(
            "Outdated skill",
            savedCareer.RequiredSkills);
    }

    [Fact]
    public async Task SynchronizeAsync_MismatchedExistingId_Throws()
    {
        await using var connection =
            new SqliteConnection("Data Source=:memory:");

        await connection.OpenAsync();
        var options = CreateOptions(connection);

        await using var context =
            new CareerAdvisorDbContext(options);

        await context.Database.EnsureCreatedAsync();

        context.CareerProfiles.Add(
            new CareerProfile
            {
                Id = Guid.NewGuid(),
                Code = "SD-001",
                Title = "Software Developer",
                Description = "Existing career with an invalid ID."
            });

        await context.SaveChangesAsync();

        var synchronizer = CreateSynchronizer(context);

        var exception = await Assert.ThrowsAsync<InvalidDataException>(
            () => synchronizer.SynchronizeAsync());

        Assert.Contains("stable catalogue ID", exception.Message);
    }

    private static CareerCatalogSynchronizer CreateSynchronizer(
        CareerAdvisorDbContext context)
    {
        var repository =
            new JsonCareerRepository(GetCatalogPath());

        return new CareerCatalogSynchronizer(
            context,
            repository);
    }

    private static DbContextOptions<CareerAdvisorDbContext> CreateOptions(
        SqliteConnection connection)
    {
        return new DbContextOptionsBuilder<CareerAdvisorDbContext>()
            .UseSqlite(connection)
            .Options;
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
}