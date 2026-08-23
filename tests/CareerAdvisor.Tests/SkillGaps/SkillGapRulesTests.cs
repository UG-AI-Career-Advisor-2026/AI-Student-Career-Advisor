using CareerAdvisor.Core.Enums;
using CareerAdvisor.Core.SkillGaps;

namespace CareerAdvisor.Tests.SkillGaps;

public class SkillGapRulesTests
{
    [Fact]
    public void Classify_NoRecognizedSkill_ReturnsMissing()
    {
        Assert.Equal(
            SkillGapClassification.Missing,
            SkillGapRules.Classify(null));
    }

    [Fact]
    public void Classify_Beginner_ReturnsNeedsDevelopment()
    {
        Assert.Equal(
            SkillGapClassification.NeedsDevelopment,
            SkillGapRules.Classify(SkillProficiency.Beginner));
    }

    [Theory]
    [InlineData(SkillProficiency.Intermediate)]
    [InlineData(SkillProficiency.Advanced)]
    [InlineData(SkillProficiency.Expert)]
    public void Classify_IntermediateOrHigher_ReturnsMatched(
        SkillProficiency proficiency)
    {
        Assert.Equal(
            SkillGapClassification.Matched,
            SkillGapRules.Classify(proficiency));
    }

    [Fact]
    public void Classify_InvalidProficiency_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => SkillGapRules.Classify((SkillProficiency)999));
    }

    [Fact]
    public void Baseline_IsIntermediate()
    {
        Assert.Equal(
            SkillProficiency.Intermediate,
            SkillGapRules.BaselineProficiency);
    }
}
