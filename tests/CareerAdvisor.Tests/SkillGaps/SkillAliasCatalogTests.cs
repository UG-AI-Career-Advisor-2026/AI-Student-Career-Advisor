using CareerAdvisor.Core.Enums;
using CareerAdvisor.Core.Models;
using CareerAdvisor.Core.SkillGaps;
using CareerAdvisor.Infrastructure.Repositories;
using System.Collections;

namespace CareerAdvisor.Tests.SkillGaps;

public class SkillAliasCatalogTests
{
    [Fact]
    public async Task CurrentCatalogue_RequiredSkillsAreValid()
    {
        var repository = new JsonCareerRepository(GetCatalogPath());
        var careers = (await repository.GetAllAsync()).ToList();

        Assert.Equal(8, careers.Count);

        foreach (var career in careers)
        {
            Assert.True(career.RequiredSkills.Count >= 6);
            Assert.All(career.RequiredSkills, skill =>
            {
                Assert.False(string.IsNullOrWhiteSpace(skill));
                Assert.Equal(skill.Trim(), skill);
            });

            Assert.Equal(
                career.RequiredSkills.Count,
                career.RequiredSkills
                    .Select(SkillNameNormalizer.Normalize)
                    .Distinct(StringComparer.Ordinal)
                    .Count());
        }
    }

    [Fact]
    public async Task AliasKeys_ReferToCurrentCatalogueRequirements()
    {
        var repository = new JsonCareerRepository(GetCatalogPath());
        var requiredSkills = (await repository.GetAllAsync())
            .SelectMany(career => career.RequiredSkills)
            .ToHashSet(StringComparer.Ordinal);

        Assert.All(
            SkillAliasCatalog.AliasesByRequiredSkill.Keys,
            key => Assert.Contains(key, requiredSkills));
    }

    [Fact]
    public async Task ApprovedPhrases_AreUnambiguousWithinEachCareer()
    {
        var repository = new JsonCareerRepository(GetCatalogPath());
        var careers = await repository.GetAllAsync();

        foreach (var career in careers)
        {
            foreach (var requiredSkill in career.RequiredSkills)
            {
                foreach (var phrase in GetAcceptedPhrases(requiredSkill))
                {
                    var candidate = new StudentSkill
                    {
                        SkillName = phrase,
                        Proficiency = SkillProficiency.Intermediate
                    };

                    var matchingRequirements = career.RequiredSkills
                        .Where(requirement =>
                            SkillNameMatcher.FindBestMatch(
                                requirement,
                                [candidate]) is not null)
                        .ToList();

                    Assert.True(
                        matchingRequirements.Count == 1 &&
                        matchingRequirements[0] == requiredSkill,
                        $"Career '{career.Code}' uses approved phrase " +
                        $"'{phrase}' ambiguously for: " +
                        $"{string.Join(", ", matchingRequirements)}.");
                }
            }
        }
    }

    [Fact]
    public void AliasDefinitions_AreNonBlankAndNormalizedUnique()
    {
        foreach (var entry in SkillAliasCatalog.AliasesByRequiredSkill)
        {
            Assert.NotEmpty(entry.Value);
            Assert.All(entry.Value, alias =>
                Assert.False(string.IsNullOrWhiteSpace(alias)));

            Assert.Equal(
                entry.Value.Count,
                entry.Value
                    .Select(SkillNameNormalizer.Normalize)
                    .Distinct(StringComparer.Ordinal)
                    .Count());
        }
    }

    [Fact]
    public void PublishedAliasDictionary_CannotBeMutated()
    {
        var aliases = SkillAliasCatalog.AliasesByRequiredSkill;

        Assert.IsNotType<Dictionary<string, IReadOnlyList<string>>>(aliases);

        if (aliases is IDictionary<string, IReadOnlyList<string>> dictionary)
        {
            Assert.Throws<NotSupportedException>(() =>
                dictionary.Add("Test Requirement", ["Test Alias"]));
        }

        if (aliases is IDictionary nonGenericDictionary)
        {
            Assert.Throws<NotSupportedException>(() =>
                nonGenericDictionary.Add(
                    "Test Requirement",
                    new[] { "Test Alias" }));
        }

        Assert.False(aliases.ContainsKey("Test Requirement"));
    }

    [Fact]
    public void PublishedAliasCollections_CannotBeMutated()
    {
        foreach (var aliases in
                 SkillAliasCatalog.AliasesByRequiredSkill.Values)
        {
            Assert.IsNotType<string[]>(aliases);

            if (aliases is IList<string> list)
            {
                Assert.Throws<NotSupportedException>(() =>
                    list.Add("Test Alias"));
            }

            if (aliases is IList nonGenericList)
            {
                Assert.Throws<NotSupportedException>(() =>
                    nonGenericList.Add("Test Alias"));
            }

            Assert.DoesNotContain("Test Alias", aliases);
        }
    }

    [Fact]
    public void ImmutableAliasCatalogue_PreservesMatchingBehavior()
    {
        var java = new StudentSkill
        {
            SkillName = "Java",
            Proficiency = SkillProficiency.Intermediate
        };

        var match = SkillNameMatcher.FindBestMatch(
            "C# or Java Programming",
            [java]);

        Assert.Same(java, match);
    }

    private static IReadOnlyList<string> GetAcceptedPhrases(
        string requiredSkill)
    {
        var phrases = new List<string>
        {
            SkillNameNormalizer.Normalize(requiredSkill)
        };

        if (SkillAliasCatalog.AliasesByRequiredSkill.TryGetValue(
                requiredSkill,
                out var aliases))
        {
            phrases.AddRange(aliases.Select(SkillNameNormalizer.Normalize));
        }

        return phrases.Distinct(StringComparer.Ordinal).ToList();
    }

    private static string GetCatalogPath()
    {
        return Path.GetFullPath(
            Path.Combine(
                AppContext.BaseDirectory,
                "..", "..", "..", "..", "..",
                "data",
                "career-catalog.json"));
    }
}
