using CareerAdvisor.Core.Enums;
using CareerAdvisor.Core.Models;
using CareerAdvisor.Core.SkillGaps;

namespace CareerAdvisor.Tests.SkillGaps;

public class SkillNameMatcherTests
{
    [Fact]
    public void FindBestMatch_ExactCanonicalPhrase_ReturnsSkill()
    {
        var skill = CreateSkill("Unit Testing", SkillProficiency.Intermediate);

        var match = SkillNameMatcher.FindBestMatch(
            "Unit Testing",
            [skill]);

        Assert.Same(skill, match);
    }

    [Theory]
    [InlineData("java", "C# or Java Programming")]
    [InlineData("Power-BI", "Data Visualization (Tableau/Power BI)")]
    [InlineData("terraform", "Infrastructure as Code (Terraform)")]
    [InlineData("POSTGRES", "SQL Server / PostgreSQL / MySQL")]
    public void FindBestMatch_ApprovedAliasWithFormatting_ReturnsSkill(
        string savedSkillName,
        string requiredSkillName)
    {
        var skill = CreateSkill(
            savedSkillName,
            SkillProficiency.Intermediate);

        var match = SkillNameMatcher.FindBestMatch(
            requiredSkillName,
            [skill]);

        Assert.Same(skill, match);
    }

    [Theory]
    [InlineData("C# OR JAVA PROGRAMMING", "Java")]
    [InlineData("  C#   or   Java   Programming  ", "Java")]
    [InlineData(
        "Data Visualization - Tableau / Power BI",
        "Tableau")]
    public void FindBestMatch_NormalizedRequiredSkill_ResolvesApprovedAlias(
        string requiredSkillName,
        string savedSkillName)
    {
        var skill = CreateSkill(
            savedSkillName,
            SkillProficiency.Intermediate);

        var match = SkillNameMatcher.FindBestMatch(
            requiredSkillName,
            [skill]);

        Assert.Same(skill, match);
    }

    [Fact]
    public void FindBestMatch_ConsecutiveWholeTokenPhrase_ReturnsSkill()
    {
        var skill = CreateSkill(
            "Building reliable REST API services",
            SkillProficiency.Advanced);

        var match = SkillNameMatcher.FindBestMatch(
            "RESTful API Design",
            [skill]);

        Assert.Same(skill, match);
    }

    [Theory]
    [InlineData("NoSQL", "SQL Querying")]
    [InlineData("JavaScript", "C# or Java Programming")]
    [InlineData("training systems", "AI")]
    [InlineData("source control", "Git Version Control")]
    public void FindBestMatch_SubstringOrUnapprovedSynonym_ReturnsNull(
        string savedSkillName,
        string requiredSkillName)
    {
        var match = SkillNameMatcher.FindBestMatch(
            requiredSkillName,
            [CreateSkill(savedSkillName, SkillProficiency.Expert)]);

        Assert.Null(match);
    }

    [Fact]
    public void FindBestMatch_DuplicateNormalizedCandidates_UsesHighestProficiency()
    {
        var beginner = CreateSkill(" power-bi ", SkillProficiency.Beginner);
        var advanced = CreateSkill("POWER BI", SkillProficiency.Advanced);

        var match = SkillNameMatcher.FindBestMatch(
            "Data Visualization (Tableau/Power BI)",
            [beginner, advanced]);

        Assert.Same(advanced, match);
    }

    [Fact]
    public void FindBestMatch_DifferentCandidates_UsesHighestProficiency()
    {
        var cSharp = CreateSkill("C#", SkillProficiency.Beginner);
        var java = CreateSkill("Java", SkillProficiency.Expert);

        var match = SkillNameMatcher.FindBestMatch(
            "C# or Java Programming",
            [cSharp, java]);

        Assert.Same(java, match);
    }

    [Fact]
    public void FindBestMatch_EqualProficiency_PrefersExactCanonicalPhrase()
    {
        var alias = CreateSkill("Java Programming", SkillProficiency.Advanced);
        var canonical = CreateSkill(
            "C# or Java Programming",
            SkillProficiency.Advanced);

        var match = SkillNameMatcher.FindBestMatch(
            "C# or Java Programming",
            [alias, canonical]);

        Assert.Same(canonical, match);
    }

    [Fact]
    public void FindBestMatch_EqualProficiency_PrefersMostSpecificPhrase()
    {
        var shortAlias = CreateSkill("Java", SkillProficiency.Advanced);
        var longerAlias = CreateSkill(
            "Java Programming",
            SkillProficiency.Advanced);

        var match = SkillNameMatcher.FindBestMatch(
            "C# or Java Programming",
            [shortAlias, longerAlias]);

        Assert.Same(longerAlias, match);
    }

    [Fact]
    public void FindBestMatch_OtherwiseTied_UsesStableLexicalOrder()
    {
        var azure = CreateSkill("Azure", SkillProficiency.Intermediate);
        var aws = CreateSkill("AWS", SkillProficiency.Intermediate);

        var forwardMatch = SkillNameMatcher.FindBestMatch(
            "AWS/Azure/GCP Platforms",
            [azure, aws]);

        var reverseMatch = SkillNameMatcher.FindBestMatch(
            "AWS/Azure/GCP Platforms",
            [aws, azure]);

        Assert.Same(aws, forwardMatch);
        Assert.Same(aws, reverseMatch);
    }

    [Fact]
    public void FindBestMatch_InvalidMatchingProficiency_Throws()
    {
        var invalid = CreateSkill("SQL", (SkillProficiency)999);

        Assert.Throws<ArgumentOutOfRangeException>(
            () => SkillNameMatcher.FindBestMatch(
                "SQL Querying",
                [invalid]));
    }

    [Fact]
    public void FindBestMatch_NullRequiredSkill_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(
            () => SkillNameMatcher.FindBestMatch(
                null,
                []));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void FindBestMatch_BlankRequiredSkill_ThrowsArgumentException(
        string requiredSkillName)
    {
        Assert.Throws<ArgumentException>(
            () => SkillNameMatcher.FindBestMatch(
                requiredSkillName,
                []));
    }

    [Fact]
    public void FindBestMatch_NullSavedSkillCollection_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(
            () => SkillNameMatcher.FindBestMatch(
                "SQL Querying",
                null));
    }

    [Fact]
    public void FindBestMatch_EmptySavedSkillCollection_ReturnsNull()
    {
        Assert.Null(SkillNameMatcher.FindBestMatch(
            "SQL Querying",
            []));
    }

    [Fact]
    public void FindBestMatch_BlankSavedSkillNames_AreIgnored()
    {
        var match = SkillNameMatcher.FindBestMatch(
            "SQL Querying",
            [
                CreateSkill("", SkillProficiency.Expert),
                CreateSkill("   ", SkillProficiency.Expert)
            ]);

        Assert.Null(match);
    }

    [Fact]
    public void FindBestMatch_NullSavedSkillEntry_ThrowsArgumentException()
    {
        IEnumerable<StudentSkill?> skills = [null];

        Assert.Throws<ArgumentException>(
            () => SkillNameMatcher.FindBestMatch(
                "SQL Querying",
                skills));
    }

    private static StudentSkill CreateSkill(
        string name,
        SkillProficiency proficiency)
    {
        return new StudentSkill
        {
            SkillName = name,
            Proficiency = proficiency
        };
    }
}
