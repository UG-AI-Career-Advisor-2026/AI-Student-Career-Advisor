using Xunit;
using CareerAdvisor.Core.Models;
using CareerAdvisor.Core.Enums;
using CareerAdvisor.Core.Validators;
using System;
using System.Collections.Generic;

namespace CareerAdvisor.Tests.Validators;

public class StudentProfileValidatorTests
{
    private readonly StudentProfileValidator _validator;

    public StudentProfileValidatorTests()
    {
        _validator = new StudentProfileValidator();
    }

    private StudentProfile GetValidProfile()
    {
        return new StudentProfile
        {
            Name = "Jane Doe",
            Programme = "Computer Science",
            AcademicLevel = AcademicLevel.Undergraduate,
            Interests = new List<string> { "AI", "Web Dev" },
            Skills = new List<StudentSkill>
            {
                new StudentSkill { SkillName = "C#", Proficiency = SkillProficiency.Intermediate }
            },
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedAt = DateTime.UtcNow
        };
    }

    [Fact]
    public void Validate_ValidProfile_ReturnsTrue()
    {
        var profile = GetValidProfile();
        var result = _validator.Validate(profile);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_InvalidName_ReturnsFalse(string invalidName)
    {
        var profile = GetValidProfile();
        profile.Name = invalidName;

        var result = _validator.Validate(profile);

        Assert.False(result.IsValid);
        Assert.Contains("Name is required.", result.Errors);
    }

    [Fact]
    public void Validate_MissingInterests_ReturnsFalse()
    {
        var profile = GetValidProfile();
        profile.Interests = new List<string>();

        var result = _validator.Validate(profile);

        Assert.False(result.IsValid);
        Assert.Contains("At least one interest is required.", result.Errors);
    }

    [Fact]
    public void Validate_EmptySkillName_ReturnsFalse()
    {
        var profile = GetValidProfile();
        profile.Skills.Add(new StudentSkill { SkillName = " " });

        var result = _validator.Validate(profile);

        Assert.False(result.IsValid);
        Assert.Contains("Skill names cannot be empty.", result.Errors);
    }

    [Fact]
    public void Validate_DuplicateSkillsIgnoringCase_ReturnsFalse()
    {
        var profile = GetValidProfile();
        profile.Skills.Add(new StudentSkill { SkillName = "c#" }); // Duplicate ignoring case

        var result = _validator.Validate(profile);

        Assert.False(result.IsValid);
        Assert.Contains("Duplicate skills are not allowed.", result.Errors);
    }

    [Fact]
    public void Validate_DuplicateInterestsIgnoringCase_ReturnsFalse()
    {
        var profile = GetValidProfile();
        profile.Interests.Add("ai"); // Duplicate ignoring case

        var result = _validator.Validate(profile);

        Assert.False(result.IsValid);
        Assert.Contains("Duplicate interests are not allowed.", result.Errors);
    }

    [Fact]
    public void Validate_UpdatedDateEarlierThanCreatedDate_ReturnsFalse()
    {
        var profile = GetValidProfile();
        profile.UpdatedAt = profile.CreatedAt.AddDays(-2);

        var result = _validator.Validate(profile);

        Assert.False(result.IsValid);
        Assert.Contains("Updated date cannot be earlier than created date.", result.Errors);
    }
}