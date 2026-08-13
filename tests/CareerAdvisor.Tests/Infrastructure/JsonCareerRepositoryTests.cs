using CareerAdvisor.Infrastructure.Repositories;

namespace CareerAdvisor.Tests.Infrastructure;

public class JsonCareerRepositoryTests
{
    private static string GetCatalogPath()
    {
        return Path.GetFullPath(
            Path.Combine(
                AppContext.BaseDirectory,
                "..", "..", "..", "..", "..",
                "data",
                "career-catalog.json"));
    }

    [Fact]
    public async Task GetAllAsync_ReturnsExactlyEightCareersWithUniqueCodes()
    {
        var repository = new JsonCareerRepository(GetCatalogPath());

        var careers = (await repository.GetAllAsync()).ToList();

        Assert.Equal(8, careers.Count);
        Assert.Equal(
            careers.Count,
            careers.Select(career => career.Code)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count());
    }

    [Theory]
    [InlineData("SD-001", "Software Developer")]
    [InlineData("DA-002", "Data Analyst")]
    [InlineData("AI-008", "AI/ML Engineer")]
    public async Task GetByCodeAsync_ValidCode_ReturnsExpectedCareer(
        string code,
        string expectedTitle)
    {
        var repository = new JsonCareerRepository(GetCatalogPath());

        var career = await repository.GetByCodeAsync(code);

        Assert.NotNull(career);
        Assert.Equal(expectedTitle, career.Title);
    }

    [Fact]
    public async Task GetByCodeAsync_IsCaseInsensitiveAndTrimsWhitespace()
    {
        var repository = new JsonCareerRepository(GetCatalogPath());

        var career = await repository.GetByCodeAsync("  sd-001  ");

        Assert.NotNull(career);
        Assert.Equal("Software Developer", career.Title);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("INVALID-CODE")]
    public async Task GetByCodeAsync_InvalidCode_ReturnsNull(string code)
    {
        var repository = new JsonCareerRepository(GetCatalogPath());

        var career = await repository.GetByCodeAsync(code);

        Assert.Null(career);
    }

    [Fact]
    public void Constructor_MissingFile_ThrowsFileNotFoundException()
    {
        var missingPath = Path.Combine(
            Path.GetTempPath(),
            $"{Guid.NewGuid()}.json");

        Assert.Throws<FileNotFoundException>(
            () => new JsonCareerRepository(missingPath));
    }
}