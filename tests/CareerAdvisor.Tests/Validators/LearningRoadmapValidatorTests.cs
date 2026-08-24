using CareerAdvisor.Core.Models;
using CareerAdvisor.Core.Validators;

namespace CareerAdvisor.Tests.Validators;

public sealed class LearningRoadmapValidatorTests
{
    private readonly LearningRoadmapValidator _validator = new();

    [Fact]
    public void Validate_ValidRoadmap_IsValid()
    {
        Assert.True(_validator.Validate(CreateValidRoadmap()).IsValid);
    }

    [Fact]
    public void Validate_NullRoadmap_ReturnsError()
    {
        var result = _validator.Validate(null);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("required"));
    }

    [Fact]
    public void Validate_InvalidIdentityAndCreatedAt_ReturnsErrors()
    {
        var roadmap = CreateValidRoadmap();
        roadmap.Id = Guid.Empty;
        roadmap.StudentProfileId = Guid.Empty;
        roadmap.CareerProfileId = Guid.Empty;
        roadmap.CreatedAt = default;

        var result = _validator.Validate(roadmap);

        Assert.False(result.IsValid);
        Assert.True(result.Errors.Count >= 4);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    public void Validate_StepCountOutsideBounds_ReturnsError(int count)
    {
        var roadmap = CreateValidRoadmap(count);

        Assert.False(_validator.Validate(roadmap).IsValid);
    }

    [Fact]
    public void Validate_NullSteps_ReturnsErrorWithoutThrowing()
    {
        var roadmap = CreateValidRoadmap();
        roadmap.Steps = null!;

        var result = _validator.Validate(roadmap);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_NullStep_ReturnsErrorWithoutThrowing()
    {
        var roadmap = CreateValidRoadmap(2);
        roadmap.Steps[1] = null!;

        var result = _validator.Validate(roadmap);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_InvalidStepIdentityOwnershipAndOrder_ReturnsErrors()
    {
        var roadmap = CreateValidRoadmap(2);
        roadmap.Steps[0].Id = Guid.Empty;
        roadmap.Steps[0].LearningRoadmapId = Guid.NewGuid();
        roadmap.Steps[1].Order = 1;

        var result = _validator.Validate(roadmap);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("belong"));
        Assert.Contains(result.Errors, error => error.Contains("contiguous"));
    }

    [Fact]
    public void Validate_DuplicateStepIdsAndNormalizedTitles_ReturnsErrors()
    {
        var roadmap = CreateValidRoadmap(2);
        roadmap.Steps[1].Id = roadmap.Steps[0].Id;
        roadmap.Steps[1].Title = "  STEP   1 ";

        var result = _validator.Validate(roadmap);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("IDs"));
        Assert.Contains(result.Errors, error => error.Contains("normalization"));
    }

    [Fact]
    public void Validate_BlankOrOversizedContentAndResourceLink_ReturnErrors()
    {
        var roadmap = CreateValidRoadmap(3);
        roadmap.Steps[0].Title = " ";
        roadmap.Steps[1].Title = new string(
            'T', LearningRoadmapValidator.MaximumTitleLength + 1);
        roadmap.Steps[1].Description = new string(
            'D', LearningRoadmapValidator.MaximumDescriptionLength + 1);
        roadmap.Steps[2].Description = " ";
        roadmap.Steps[2].ResourceLink = "https://example.test";

        var result = _validator.Validate(roadmap);

        Assert.False(result.IsValid);
        Assert.True(result.Errors.Count >= 5);
    }

    [Fact]
    public void Validate_NullResourceLink_ReturnsError()
    {
        var roadmap = CreateValidRoadmap();
        roadmap.Steps[0].ResourceLink = null!;

        Assert.False(_validator.Validate(roadmap).IsValid);
    }

    [Fact]
    public void Validate_CompletionStateMustMatchUtcTimestamp()
    {
        var roadmap = CreateValidRoadmap(3);
        roadmap.Steps[0].IsCompleted = true;
        roadmap.Steps[0].CompletedAt = null;
        roadmap.Steps[1].IsCompleted = false;
        roadmap.Steps[1].CompletedAt = DateTime.UtcNow;
        roadmap.Steps[2].IsCompleted = true;
        roadmap.Steps[2].CompletedAt = DateTime.SpecifyKind(
            DateTime.UtcNow,
            DateTimeKind.Local);

        var result = _validator.Validate(roadmap);

        Assert.False(result.IsValid);
        Assert.True(result.Errors.Count >= 3);
    }

    private static LearningRoadmap CreateValidRoadmap(int stepCount = 1)
    {
        var roadmap = new LearningRoadmap
        {
            Id = Guid.NewGuid(),
            StudentProfileId = Guid.NewGuid(),
            CareerProfileId = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow
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
}
