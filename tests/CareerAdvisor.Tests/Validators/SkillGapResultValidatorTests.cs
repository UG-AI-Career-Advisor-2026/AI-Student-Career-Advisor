using CareerAdvisor.Core.Enums;
using CareerAdvisor.Core.Models;
using CareerAdvisor.Core.Validators;

namespace CareerAdvisor.Tests.Validators;

public class SkillGapResultValidatorTests
{
    private readonly SkillGapResultValidator _validator = new();

    [Fact]
    public void Validate_ValidClassificationCombinations_ReturnsValid()
    {
        var result = CreateValidResult();

        var validation = _validator.Validate(result);

        Assert.True(validation.IsValid);
        Assert.Empty(validation.Errors);
    }

    [Fact]
    public void Validate_SameSavedSkillUsedForDifferentRequirements_RemainsValid()
    {
        var result = CreateValidResult();
        result.Items =
        [
            CreateMatchedItem("SQL Querying", "SQL"),
            CreateMatchedItem("SQL and Database Integration", "SQL")
        ];

        var validation = _validator.Validate(result);

        Assert.True(validation.IsValid);
    }

    [Fact]
    public void Validate_NullResult_ReturnsValidationError()
    {
        var validation = _validator.Validate(null);

        Assert.False(validation.IsValid);
        Assert.Contains("Skill-gap result is required.", validation.Errors);
    }

    [Fact]
    public void Validate_MissingIdentityAndCareerData_ReturnsAllErrors()
    {
        var result = CreateValidResult();
        result.StudentProfileId = Guid.Empty;
        result.CareerProfileId = Guid.Empty;
        result.CareerCode = " ";
        result.CareerTitle = string.Empty;

        var validation = _validator.Validate(result);

        Assert.False(validation.IsValid);
        Assert.Contains("StudentProfileId cannot be empty.", validation.Errors);
        Assert.Contains("CareerProfileId cannot be empty.", validation.Errors);
        Assert.Contains("Career code is required.", validation.Errors);
        Assert.Contains("Career title is required.", validation.Errors);
    }

    [Fact]
    public void Validate_NonIntermediateBaseline_ReturnsValidationError()
    {
        var result = CreateValidResult();
        result.BaselineProficiency = SkillProficiency.Advanced;

        var validation = _validator.Validate(result);

        Assert.False(validation.IsValid);
        Assert.Contains(
            "The skill-gap baseline must be Intermediate.",
            validation.Errors);
    }

    [Fact]
    public void Validate_InvalidBaselineEnum_ReturnsValidationError()
    {
        var result = CreateValidResult();
        result.BaselineProficiency = (SkillProficiency)999;

        var validation = _validator.Validate(result);

        Assert.False(validation.IsValid);
        Assert.Contains(
            "The skill-gap baseline must be Intermediate.",
            validation.Errors);
    }

    [Fact]
    public void Validate_NoItems_ReturnsValidationError()
    {
        var result = CreateValidResult();
        result.Items = [];

        var validation = _validator.Validate(result);

        Assert.False(validation.IsValid);
        Assert.Contains(
            "At least one required-skill result item is required.",
            validation.Errors);
    }

    [Fact]
    public void Validate_NullItems_ReturnsValidationErrorWithoutThrowing()
    {
        var result = CreateValidResult();
        result.Items = null!;

        var validation = _validator.Validate(result);

        Assert.False(validation.IsValid);
        Assert.Contains(
            "At least one required-skill result item is required.",
            validation.Errors);
    }

    [Fact]
    public void Validate_NullItem_ReturnsValidationErrorWithoutThrowing()
    {
        var result = CreateValidResult();
        result.Items = [null!];

        var validation = _validator.Validate(result);

        Assert.False(validation.IsValid);
        Assert.Contains(
            "Skill-gap item 1 is required.",
            validation.Errors);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_BlankRequiredSkillName_ReturnsValidationError(
        string requiredSkillName)
    {
        var result = CreateValidResult();
        result.Items[0].RequiredSkillName = requiredSkillName;

        var validation = _validator.Validate(result);

        Assert.False(validation.IsValid);
        Assert.Contains(
            "Skill-gap item 1 must include a required skill name.",
            validation.Errors);
    }

    [Fact]
    public void Validate_DuplicateNormalizedRequiredSkills_ReturnsValidationError()
    {
        var result = CreateValidResult();
        result.Items =
        [
            CreateMatchedItem("CI/CD Integration", "CI/CD"),
            CreateMatchedItem("ci cd integration", "CI/CD")
        ];

        var validation = _validator.Validate(result);

        Assert.False(validation.IsValid);
        Assert.Contains(
            "Duplicate normalized required skills are not allowed.",
            validation.Errors);
    }

    [Fact]
    public void Validate_InvalidClassification_ReturnsValidationError()
    {
        var result = CreateValidResult();
        result.Items[0].Classification = (SkillGapClassification)999;

        var validation = _validator.Validate(result);

        Assert.False(validation.IsValid);
        Assert.Contains(
            "Skill-gap item 1 has an invalid classification.",
            validation.Errors);
    }

    [Fact]
    public void Validate_InvalidCurrentProficiency_ReturnsValidationError()
    {
        var result = CreateValidResult();
        result.Items[1].CurrentProficiency = (SkillProficiency)999;

        var validation = _validator.Validate(result);

        Assert.False(validation.IsValid);
        Assert.Contains(
            "Skill-gap item 2 has an invalid current proficiency.",
            validation.Errors);
    }

    [Theory]
    [InlineData(SkillGapClassification.Missing, "SQL", null)]
    [InlineData(
        SkillGapClassification.Missing,
        null,
        SkillProficiency.Beginner)]
    [InlineData(
        SkillGapClassification.NeedsDevelopment,
        null,
        SkillProficiency.Beginner)]
    [InlineData(
        SkillGapClassification.NeedsDevelopment,
        "SQL",
        SkillProficiency.Intermediate)]
    [InlineData(
        SkillGapClassification.Matched,
        null,
        SkillProficiency.Intermediate)]
    [InlineData(
        SkillGapClassification.Matched,
        "SQL",
        SkillProficiency.Beginner)]
    public void Validate_InconsistentItemState_ReturnsValidationError(
        SkillGapClassification classification,
        string? matchedName,
        SkillProficiency? proficiency)
    {
        var result = CreateValidResult();
        result.Items =
        [
            new SkillGapItem
            {
                RequiredSkillName = "SQL Querying",
                Classification = classification,
                MatchedStudentSkillName = matchedName,
                CurrentProficiency = proficiency
            }
        ];

        var validation = _validator.Validate(result);

        Assert.False(validation.IsValid);
        Assert.NotEmpty(validation.Errors);
    }

    [Theory]
    [InlineData(SkillGapClassification.NeedsDevelopment, null)]
    [InlineData(SkillGapClassification.NeedsDevelopment, "")]
    [InlineData(SkillGapClassification.NeedsDevelopment, "   ")]
    [InlineData(SkillGapClassification.Matched, null)]
    [InlineData(SkillGapClassification.Matched, "")]
    [InlineData(SkillGapClassification.Matched, "   ")]
    public void Validate_ClassificationRequiringMatch_RejectsBlankMatchedName(
        SkillGapClassification classification,
        string? matchedName)
    {
        var result = CreateValidResult();
        result.Items =
        [
            new SkillGapItem
            {
                RequiredSkillName = "SQL Querying",
                Classification = classification,
                MatchedStudentSkillName = matchedName,
                CurrentProficiency = classification ==
                    SkillGapClassification.NeedsDevelopment
                        ? SkillProficiency.Beginner
                        : SkillProficiency.Intermediate
            }
        ];

        var validation = _validator.Validate(result);

        Assert.False(validation.IsValid);
        Assert.NotEmpty(validation.Errors);
    }

    private static SkillGapResult CreateValidResult()
    {
        return new SkillGapResult
        {
            StudentProfileId = Guid.NewGuid(),
            CareerProfileId = Guid.NewGuid(),
            CareerCode = "DA-002",
            CareerTitle = "Data Analyst",
            BaselineProficiency = SkillProficiency.Intermediate,
            Items =
            [
                new SkillGapItem
                {
                    RequiredSkillName = "Statistical Analysis",
                    Classification = SkillGapClassification.Missing
                },
                new SkillGapItem
                {
                    RequiredSkillName = "SQL Querying",
                    Classification = SkillGapClassification.NeedsDevelopment,
                    MatchedStudentSkillName = "SQL",
                    CurrentProficiency = SkillProficiency.Beginner
                },
                CreateMatchedItem("Excel/Spreadsheets", "Excel")
            ]
        };
    }

    private static SkillGapItem CreateMatchedItem(
        string requiredSkill,
        string matchedSkill)
    {
        return new SkillGapItem
        {
            RequiredSkillName = requiredSkill,
            Classification = SkillGapClassification.Matched,
            MatchedStudentSkillName = matchedSkill,
            CurrentProficiency = SkillProficiency.Intermediate
        };
    }
}
