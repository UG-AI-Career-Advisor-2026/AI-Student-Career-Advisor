using CareerAdvisor.Core.SkillGaps;

namespace CareerAdvisor.Tests.SkillGaps;

public class SkillNameNormalizerTests
{
    [Theory]
    [InlineData("  POWER   BI  ", "power bi")]
    [InlineData("data-visualization", "data visualization")]
    [InlineData("Data Visualization (Tableau/Power BI)",
        "data visualization tableau power bi")]
    [InlineData("C#", "c sharp")]
    [InlineData("C++", "c plus plus")]
    [InlineData("CI/CD", "ci cd")]
    [InlineData("TCP/IP", "tcp ip")]
    [InlineData("UI/UX", "ui ux")]
    [InlineData("AI/ML", "ai ml")]
    public void Normalize_CasingWhitespaceAndPunctuation_ReturnsTokens(
        string value,
        string expected)
    {
        Assert.Equal(expected, SkillNameNormalizer.Normalize(value));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Normalize_BlankValue_ReturnsEmpty(string? value)
    {
        Assert.Equal(string.Empty, SkillNameNormalizer.Normalize(value));
    }

    [Fact]
    public void Normalize_IsIdempotent()
    {
        var normalized = SkillNameNormalizer.Normalize(
            "  Infrastructure-as-Code (Terraform) ");

        Assert.Equal(
            normalized,
            SkillNameNormalizer.Normalize(normalized));
    }
}
