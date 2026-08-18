using CareerAdvisor.Core.Enums;
using CareerAdvisor.Core.Models;
using CareerAdvisor.Core.Recommendations;

namespace CareerAdvisor.Tests.Recommendations;

public class RecommendationFeatureSchemaTests
{
    [Fact]
    public void Schema_MapsEveryAssessmentQuestionAndOptionExactlyOnce()
    {
        var questions = AssessmentQuestionBank.GetAllQuestions();

        Assert.Equal(15, questions.Count);
        Assert.All(questions, question => Assert.True(question.IsRequired));

        var questionCodes = questions
            .Select(question => question.Code)
            .OrderBy(code => code)
            .ToArray();

        var mappedQuestionCodes = RecommendationFeatureSchema
            .QuestionColumnsByCode.Keys
            .OrderBy(code => code)
            .ToArray();

        Assert.Equal(questionCodes, mappedQuestionCodes);

        var optionCodes = questions
            .SelectMany(question => question.Options)
            .Select(option => option.Code)
            .OrderBy(code => code)
            .ToArray();

        Assert.Equal(60, optionCodes.Length);
        Assert.Equal(60, optionCodes.Distinct(StringComparer.Ordinal).Count());

        var mappedOptionCodes = RecommendationFeatureSchema
            .NumericOptionValues.Keys
            .Concat(RecommendationFeatureSchema.CategoricalOptionValues.Keys)
            .OrderBy(code => code)
            .ToArray();

        Assert.Equal(optionCodes, mappedOptionCodes);

        Assert.Empty(
            RecommendationFeatureSchema.NumericOptionValues.Keys.Intersect(
                RecommendationFeatureSchema.CategoricalOptionValues.Keys,
                StringComparer.Ordinal));
    }

    [Fact]
    public void NumericQuestions_UseValuesWithinDocumentedRange()
    {
        var numericQuestions = AssessmentQuestionBank
            .GetAllQuestions()
            .Where(question => question.DisplayOrder <= 10)
            .ToList();

        Assert.Equal(10, numericQuestions.Count);

        foreach (var question in numericQuestions)
        {
            var column = RecommendationFeatureSchema
                .QuestionColumnsByCode[question.Code];

            Assert.Contains(
                column,
                RecommendationFeatureSchema.NumericColumns);

            foreach (var option in question.Options)
            {
                Assert.True(
                    RecommendationFeatureSchema.NumericOptionValues.TryGetValue(
                        option.Code,
                        out var value),
                    $"Numeric mapping is missing for option '{option.Code}'.");

                Assert.InRange(
                    value,
                    RecommendationFeatureSchema.MinimumNumericValue,
                    RecommendationFeatureSchema.MaximumNumericValue);
            }
        }
    }

    [Fact]
    public void CategoricalQuestions_UseRecognizedValues()
    {
        var categoricalQuestions = AssessmentQuestionBank
            .GetAllQuestions()
            .Where(question => question.DisplayOrder >= 11)
            .ToList();

        Assert.Equal(5, categoricalQuestions.Count);

        foreach (var question in categoricalQuestions)
        {
            var column = RecommendationFeatureSchema
                .QuestionColumnsByCode[question.Code];

            Assert.True(
                RecommendationFeatureSchema
                    .AllowedCategoricalValuesByColumn
                    .TryGetValue(column, out var allowedValues),
                $"Allowed values are missing for column '{column}'.");

            foreach (var option in question.Options)
            {
                Assert.True(
                    RecommendationFeatureSchema.CategoricalOptionValues.TryGetValue(
                        option.Code,
                        out var mappedValue),
                    $"Categorical mapping is missing for option '{option.Code}'.");

                Assert.Contains(mappedValue, allowedValues!);
            }
        }
    }

    [Fact]
    public void RequiredColumns_AreUniqueAndContainEveryFeature()
    {
        var requiredColumns = RecommendationFeatureSchema.RequiredColumns;

        Assert.Equal(
            requiredColumns.Count,
            requiredColumns.Distinct(StringComparer.Ordinal).Count());

        Assert.Contains("AcademicBackground", requiredColumns);
        Assert.Contains("AcademicLevel", requiredColumns);
        Assert.Contains("CareerLabel", requiredColumns);

        Assert.All(
            RecommendationFeatureSchema.NumericColumns,
            column => Assert.Contains(column, requiredColumns));

        Assert.All(
            RecommendationFeatureSchema.QuestionColumnsByCode.Values,
            column => Assert.Contains(column, requiredColumns));

        Assert.All(
            RecommendationFeatureSchema.AllowedCategoricalValuesByColumn.Keys,
            column => Assert.Contains(column, requiredColumns));
    }

    [Fact]
    public void CareerLabels_ContainEightUniqueCatalogueMappings()
    {
        var mappings = RecommendationFeatureSchema.CareerLabelsByCode;

        Assert.Equal(8, mappings.Count);
        Assert.Equal(
            8,
            mappings.Keys.Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.Equal(
            8,
            mappings.Values.Distinct(StringComparer.OrdinalIgnoreCase).Count());

        Assert.Equal("Software Developer", mappings["SD-001"]);
        Assert.Equal("Data Analyst", mappings["DA-002"]);
        Assert.Equal("Cybersecurity Analyst", mappings["CS-003"]);
        Assert.Equal("Cloud Engineer", mappings["CE-004"]);
        Assert.Equal("Network Administrator", mappings["NA-005"]);
        Assert.Equal("Database Administrator", mappings["DBA-006"]);
        Assert.Equal("UI/UX Designer", mappings["UX-007"]);
        Assert.Equal("AI/ML Engineer", mappings["AI-008"]);
    }

    [Fact]
    public void ProfileSkillProficiencies_HaveValidUniqueValues()
    {
        var mappings = RecommendationFeatureSchema.SkillProficiencyValues;

        Assert.Equal(Enum.GetValues<SkillProficiency>().Length, mappings.Count);

        Assert.All(
            mappings.Values,
            value => Assert.InRange(
                value,
                RecommendationFeatureSchema.MinimumNumericValue,
                RecommendationFeatureSchema.MaximumNumericValue));

        Assert.Equal(mappings.Count, mappings.Values.Distinct().Count());
    }
    [Fact]
public void ProfileDomainMappings_CoverAllEightProfileColumns()
{
    var expectedColumns = new[]
    {
        "ProgrammingSkill",
        "DataSkill",
        "CybersecuritySkill",
        "CloudSkill",
        "NetworkingSkill",
        "DatabaseSkill",
        "DesignSkill",
        "AISkill"
    };

    Assert.Equal(
        expectedColumns.OrderBy(column => column),
        RecommendationFeatureSchema.ProfileDomainKeywordsByColumn.Keys
            .OrderBy(column => column));

    foreach (var keywords in
             RecommendationFeatureSchema.ProfileDomainKeywordsByColumn.Values)
    {
        Assert.NotEmpty(keywords);

        Assert.Equal(
            keywords.Count,
            keywords.Distinct(StringComparer.OrdinalIgnoreCase).Count());

        Assert.All(keywords, keyword =>
        {
            Assert.False(string.IsNullOrWhiteSpace(keyword));
            Assert.Equal(keyword.Trim(), keyword);
            Assert.Equal(keyword.ToLowerInvariant(), keyword);
        });
    }

    Assert.Equal(
        RecommendationFeatureSchema.MinimumNumericValue,
        RecommendationFeatureSchema.NoProfileEvidenceValue);

    Assert.InRange(
        RecommendationFeatureSchema.InterestOnlyValue,
        RecommendationFeatureSchema.MinimumNumericValue,
        RecommendationFeatureSchema.MaximumNumericValue);

    Assert.True(
        RecommendationFeatureSchema.InterestOnlyValue >
        RecommendationFeatureSchema.NoProfileEvidenceValue);
}
}
