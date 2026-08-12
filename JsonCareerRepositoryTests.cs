using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CareerAdvisor.Infrastructure.Persistence;
using Xunit;

namespace CareerAdvisor.Tests.Infrastructure;

public class JsonCareerRepositoryTests
{
    private readonly string _testJsonPath;

    public JsonCareerRepositoryTests()
    {
        // This assumes the test runner executes from a path where this relative path is valid.
        // You may need to adjust this based on your solution structure and test runner configuration.
        _testJsonPath = Path.GetFullPath(Path.Combine("..", "..", "..", "..", "data", "career-catalog.json"));
    }

    [Fact]
    public void Constructor_Throws_WhenFileNotFound()
    {
        // Arrange
        var invalidPath = "non_existent_file.json";

        // Act & Assert
        var ex = Assert.Throws<FileNotFoundException>(() => new JsonCareerRepository(invalidPath));
        Assert.Contains("The career catalog file was not found", ex.Message);
    }

    [Fact]
    public async Task GetCareersAsync_ReturnsAllEightCareers()
    {
        // Arrange
        var repository = new JsonCareerRepository(_testJsonPath);

        // Act
        var careers = await repository.GetCareersAsync();

        // Assert
        Assert.NotNull(careers);
        Assert.Equal(8, careers.Count());
    }

    [Fact]
    public async Task GetCareersAsync_AllCareerCodesAreUnique()
    {
        // Arrange
        var repository = new JsonCareerRepository(_testJsonPath);

        // Act
        var careers = await repository.GetCareersAsync();
        var codes = careers.Select(c => c.Code).ToList();

        // Assert
        Assert.Equal(codes.Count, codes.Distinct().Count());
    }

    [Theory]
    [InlineData("SD-001", "Software Developer")]
    [InlineData("DA-002", "Data Analyst")]
    [InlineData("AI-008", "AI/ML Engineer")]
    public async Task GetCareerByCodeAsync_ReturnsCorrectCareer_ForValidCode(string code, string expectedName)
    {
        // Arrange
        var repository = new JsonCareerRepository(_testJsonPath);

        // Act
        var career = await repository.GetCareerByCodeAsync(code);

        // Assert
        Assert.NotNull(career);
        Assert.Equal(expectedName, career.Name);
    }

    [Fact]
    public async Task GetCareerByCodeAsync_ReturnsNull_ForInvalidCode()
    {
        // Arrange
        var repository = new JsonCareerRepository(_testJsonPath);

        // Act
        var career = await repository.GetCareerByCodeAsync("INVALID-CODE");

        // Assert
        Assert.Null(career);
    }
}