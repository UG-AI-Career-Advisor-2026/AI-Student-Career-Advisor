using CareerAdvisor.Core.Recommendations;
using CareerAdvisor.Core.Models;

namespace CareerAdvisor.Tests.Recommendations;

public sealed class RecommendationDisclaimerTests
{
    [Theory]
    [MemberData(nameof(ExtractionCases))]
    public void RemoveExactOccurrences_RemovesOnlyCanonicalOccurrences(
        string reasoning,
        string expected)
    {
        var persistedReasoning = reasoning;

        var explanation =
            RecommendationDisclaimer.RemoveExactOccurrences(reasoning);

        Assert.Equal(expected, explanation);
        Assert.Equal(persistedReasoning, reasoning);
        Assert.Equal(
            1,
            CountExactOccurrences(
                explanation + RecommendationDisclaimer.Text,
                RecommendationDisclaimer.Text));
    }

    [Theory]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData("   ", "   ")]
    public void RemoveExactOccurrences_HandlesNullEmptyAndWhitespace(
        string? reasoning,
        string expected)
    {
        Assert.Equal(
            expected,
            RecommendationDisclaimer.RemoveExactOccurrences(reasoning));
    }

    [Fact]
    public void RemoveExactOccurrences_DoesNotModifyPersistedReasoning()
    {
        var recommendation = new CareerRecommendation
        {
            Reasoning = "Before." + RecommendationDisclaimer.Text + "After."
        };
        var persistedReasoning = recommendation.Reasoning;

        var explanation = RecommendationDisclaimer.RemoveExactOccurrences(
            recommendation.Reasoning);

        Assert.Equal("Before.After.", explanation);
        Assert.Equal(persistedReasoning, recommendation.Reasoning);
    }

    public static TheoryData<string, string> ExtractionCases => new()
    {
        {
            RecommendationDisclaimer.Text + "Opening explanation.",
            "Opening explanation."
        },
        {
            "Before." + RecommendationDisclaimer.Text + "After.",
            "Before.After."
        },
        {
            "Closing explanation." + RecommendationDisclaimer.Text,
            "Closing explanation."
        },
        {
            "First." + RecommendationDisclaimer.Text + "Second." +
            RecommendationDisclaimer.Text + "Third.",
            "First.Second.Third."
        },
        { "No disclaimer is present.", "No disclaimer is present." },
        {
            RecommendationDisclaimer.Text.ToUpperInvariant(),
            RecommendationDisclaimer.Text.ToUpperInvariant()
        },
        {
            RecommendationDisclaimer.Text.Replace("Career ", "Career  "),
            RecommendationDisclaimer.Text.Replace("Career ", "Career  ")
        }
    };

    private static int CountExactOccurrences(string value, string expected)
    {
        var count = 0;
        var startIndex = 0;

        while ((startIndex = value.IndexOf(
                   expected,
                   startIndex,
                   StringComparison.Ordinal)) >= 0)
        {
            count++;
            startIndex += expected.Length;
        }

        return count;
    }
}
