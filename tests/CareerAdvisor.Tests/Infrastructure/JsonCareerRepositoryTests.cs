using CareerAdvisor.Infrastructure.Repositories;
using System.Text.Json.Nodes;

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

    [Fact]
    public void Constructor_CareerWithFewerThanSixRequiredSkills_ThrowsInvalidDataException()
    {
        var path = CreateModifiedCatalog(root =>
        {
            var requiredSkills = root[0]!["requiredSkills"]!.AsArray();
            requiredSkills.RemoveAt(requiredSkills.Count - 1);
        });

        try
        {
            Assert.Throws<InvalidDataException>(
                () => new JsonCareerRepository(path));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Constructor_NullRequiredSkills_ThrowsInvalidDataException()
    {
        var path = CreateModifiedCatalog(root =>
        {
            root[0]!["requiredSkills"] = null;
        });

        try
        {
            Assert.Throws<InvalidDataException>(
                () => new JsonCareerRepository(path));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Constructor_EmptyRequiredSkills_ThrowsInvalidDataException()
    {
        var path = CreateModifiedCatalog(root =>
        {
            root[0]!["requiredSkills"] = new JsonArray();
        });

        try
        {
            Assert.Throws<InvalidDataException>(
                () => new JsonCareerRepository(path));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Constructor_WhitespaceRequiredSkill_ThrowsInvalidDataException()
    {
        var path = CreateModifiedCatalog(root =>
        {
            root[0]!["requiredSkills"]![0] = "   ";
        });

        try
        {
            Assert.Throws<InvalidDataException>(
                () => new JsonCareerRepository(path));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Constructor_UntrimmedRequiredSkill_ThrowsInvalidDataException()
    {
        var path = CreateModifiedCatalog(root =>
        {
            root[0]!["requiredSkills"]![0] = " C# or Java Programming";
        });

        try
        {
            Assert.Throws<InvalidDataException>(
                () => new JsonCareerRepository(path));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Constructor_DuplicateNormalizedRequiredSkill_ThrowsInvalidDataException()
    {
        var path = CreateModifiedCatalog(root =>
        {
            root[0]!["requiredSkills"]![1] = "c# OR java programming";
        });

        try
        {
            Assert.Throws<InvalidDataException>(
                () => new JsonCareerRepository(path));
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static string CreateModifiedCatalog(
        Action<JsonArray> modify)
    {
        var root = JsonNode
            .Parse(File.ReadAllText(GetCatalogPath()))!
            .AsArray();

        modify(root);

        var path = Path.Combine(
            Path.GetTempPath(),
            $"career-catalog-{Guid.NewGuid()}.json");

        File.WriteAllText(path, root.ToJsonString());
        return path;
    }
}
