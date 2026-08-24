using CareerAdvisor.Core.Enums;
using System.Data;
using CareerAdvisor.Core.Interfaces;
using CareerAdvisor.Core.Models;
using CareerAdvisor.Core.SkillGaps;
using CareerAdvisor.Core.Validators;
using CareerAdvisor.Infrastructure.Services;

namespace CareerAdvisor.Tests.Services;

public sealed class SkillGapServiceTests
{
    [Fact]
    public async Task AnalyzeAsync_EmptyProfileId_IsRejectedBeforeLookup()
    {
        var repositories = CreateRepositories();
        var service = CreateService(repositories);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.AnalyzeAsync(Guid.Empty, repositories.Career.Entity!.Id));

        Assert.Equal(0, repositories.Profile.GetByIdCalls);
        Assert.Equal(0, repositories.Career.GetByIdCalls);
    }

    [Fact]
    public async Task AnalyzeAsync_UnknownProfile_IsRejectedBeforeCareerLookup()
    {
        var repositories = CreateRepositories();
        repositories.Profile.Entity = null;
        var service = CreateService(repositories);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.AnalyzeAsync(
                Guid.NewGuid(),
                repositories.Career.Entity!.Id));

        Assert.Contains("profile", exception.Message,
            StringComparison.OrdinalIgnoreCase);
        Assert.Equal(1, repositories.Profile.GetByIdCalls);
        Assert.Equal(0, repositories.Career.GetByIdCalls);
    }

    [Fact]
    public async Task AnalyzeAsync_MismatchedProfile_IsRejectedBeforeCareerLookup()
    {
        var repositories = CreateRepositories();
        var requestedProfileId = Guid.NewGuid();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => CreateService(repositories).AnalyzeAsync(
                requestedProfileId,
                repositories.Career.Entity!.Id));

        Assert.Equal(
            "The requested student profile could not be found.",
            exception.Message);
        Assert.NotEqual(requestedProfileId, repositories.Profile.Entity!.Id);
        Assert.Equal(1, repositories.Profile.GetByIdCalls);
        Assert.Equal(0, repositories.Career.GetByIdCalls);
    }

    [Fact]
    public async Task AnalyzeAsync_EmptyCareerId_IsRejectedBeforeCareerLookup()
    {
        var repositories = CreateRepositories();
        var service = CreateService(repositories);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.AnalyzeAsync(repositories.Profile.Entity!.Id, Guid.Empty));

        Assert.Equal(0, repositories.Career.GetByIdCalls);
    }

    [Fact]
    public async Task AnalyzeAsync_UnknownCareer_IsRejected()
    {
        var repositories = CreateRepositories();
        repositories.Career.Entity = null;
        var service = CreateService(repositories);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.AnalyzeAsync(
                repositories.Profile.Entity!.Id,
                Guid.NewGuid()));

        Assert.Contains("career", exception.Message,
            StringComparison.OrdinalIgnoreCase);
        Assert.Equal(1, repositories.Career.GetByIdCalls);
    }

    [Fact]
    public async Task AnalyzeAsync_MismatchedCareer_IsRejected()
    {
        var repositories = CreateRepositories();
        var requestedCareerId = Guid.NewGuid();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => CreateService(repositories).AnalyzeAsync(
                repositories.Profile.Entity!.Id,
                requestedCareerId));

        Assert.Equal(
            "The requested career could not be found in the supported " +
            "career catalogue.",
            exception.Message);
        Assert.NotEqual(requestedCareerId, repositories.Career.Entity!.Id);
        Assert.Equal(1, repositories.Career.GetByIdCalls);
    }

    [Fact]
    public async Task AnalyzeAsync_NoSavedSkills_ReturnsAllMissing()
    {
        var repositories = CreateRepositories(
            skills: [],
            requiredSkills: ["SQL Querying", "Python Scripting"]);
        var result = await CreateService(repositories).AnalyzeAsync(
            repositories.Profile.Entity!.Id,
            repositories.Career.Entity!.Id);

        Assert.Equal(6, result.Items.Count);
        Assert.All(result.Items, item =>
        {
            Assert.Equal(SkillGapClassification.Missing, item.Classification);
            Assert.Null(item.MatchedStudentSkillName);
            Assert.Null(item.CurrentProficiency);
        });
        Assert.Equal(6, result.MissingCount);
        Assert.Equal(0, result.NeedsDevelopmentCount);
        Assert.Equal(0, result.MatchedCount);
    }

    [Fact]
    public async Task AnalyzeAsync_PartialProfile_ClassifiesPreservesAndOrders()
    {
        var skills = new List<StudentSkill>
        {
            Skill("SQL", SkillProficiency.Beginner),
            Skill("  PYTHON  ", SkillProficiency.Advanced)
        };
        var required = new List<string>
        {
            "Python Scripting",
            "Version Control",
            "SQL Querying",
            "Communication"
        };
        var repositories = CreateRepositories(skills, required);

        var result = await CreateService(repositories).AnalyzeAsync(
            repositories.Profile.Entity!.Id,
            repositories.Career.Entity!.Id);

        Assert.Equal(
            [
                "Communication",
                "Problem Solving",
                "Technical Documentation",
                "Version Control",
                "SQL Querying",
                "Python Scripting"
            ],
            result.Items.Select(item => item.RequiredSkillName));
        Assert.Equal(4, result.MissingCount);
        Assert.Equal(1, result.NeedsDevelopmentCount);
        Assert.Equal(1, result.MatchedCount);
        Assert.Equal(result.Items.Count,
            result.MissingCount + result.NeedsDevelopmentCount +
            result.MatchedCount);

        var sql = result.Items.Single(item =>
            item.RequiredSkillName == "SQL Querying");
        Assert.Equal("SQL", sql.MatchedStudentSkillName);
        Assert.Equal(SkillProficiency.Beginner, sql.CurrentProficiency);
        Assert.Equal(
            SkillGapClassification.NeedsDevelopment,
            sql.Classification);

        var python = result.Items.Single(item =>
            item.RequiredSkillName == "Python Scripting");
        Assert.Equal("  PYTHON  ", python.MatchedStudentSkillName);
        Assert.Equal(SkillProficiency.Advanced, python.CurrentProficiency);
        Assert.Equal(SkillGapClassification.Matched, python.Classification);
    }

    [Theory]
    [InlineData(SkillProficiency.Beginner,
        SkillGapClassification.NeedsDevelopment)]
    [InlineData(SkillProficiency.Intermediate,
        SkillGapClassification.Matched)]
    [InlineData(SkillProficiency.Advanced,
        SkillGapClassification.Matched)]
    [InlineData(SkillProficiency.Expert,
        SkillGapClassification.Matched)]
    public async Task AnalyzeAsync_PreservesProficiencyAndClassification(
        SkillProficiency proficiency,
        SkillGapClassification expectedClassification)
    {
        var repositories = CreateRepositories(
            [Skill("SQL", proficiency)],
            ["SQL Querying"]);

        var result = await CreateService(repositories).AnalyzeAsync(
            repositories.Profile.Entity!.Id,
            repositories.Career.Entity!.Id);

        var item = result.Items.Single(candidate =>
            candidate.RequiredSkillName == "SQL Querying");
        Assert.Equal(expectedClassification, item.Classification);
        Assert.Equal(proficiency, item.CurrentProficiency);
        Assert.Equal("SQL", item.MatchedStudentSkillName);
    }

    [Fact]
    public async Task AnalyzeAsync_ApprovedAliasMatches_UnapprovedSynonymDoesNot()
    {
        var repositories = CreateRepositories(
            [
                Skill("Java", SkillProficiency.Intermediate),
                Skill("Source Tracking", SkillProficiency.Expert)
            ],
            ["C# or Java Programming", "Git Version Control"]);

        var result = await CreateService(repositories).AnalyzeAsync(
            repositories.Profile.Entity!.Id,
            repositories.Career.Entity!.Id);

        var alias = result.Items.Single(item =>
            item.RequiredSkillName == "C# or Java Programming");
        Assert.Equal(SkillGapClassification.Matched, alias.Classification);
        Assert.Equal("Java", alias.MatchedStudentSkillName);

        var synonym = result.Items.Single(item =>
            item.RequiredSkillName == "Git Version Control");
        Assert.Equal(SkillGapClassification.Missing, synonym.Classification);
    }

    [Fact]
    public async Task AnalyzeAsync_EvaluatesEveryUniqueRequirementOnce()
    {
        var required = new List<string>
        {
            "SQL Querying",
            "Python Scripting",
            "Git Version Control"
        };
        var repositories = CreateRepositories([], required);

        var result = await CreateService(repositories).AnalyzeAsync(
            repositories.Profile.Entity!.Id,
            repositories.Career.Entity!.Id);

        Assert.Equal(
            repositories.Career.Entity!.RequiredSkills.Count,
            result.Items.Count);
        Assert.Equal(
            repositories.Career.Entity.RequiredSkills
                .OrderBy(name => name, StringComparer.Ordinal),
            result.Items.Select(item => item.RequiredSkillName));
        Assert.Equal(
            result.Items.Count,
            result.Items.Select(item =>
                    SkillNameNormalizer.Normalize(item.RequiredSkillName))
                .Distinct(StringComparer.Ordinal)
                .Count());
    }

    [Fact]
    public async Task AnalyzeAsync_SameSkillMayMatchDifferentRequirements()
    {
        var repositories = CreateRepositories(
            [Skill("Python", SkillProficiency.Intermediate)],
            ["Python Scripting", "Scripting (Python/Bash)"]);

        var result = await CreateService(repositories).AnalyzeAsync(
            repositories.Profile.Entity!.Id,
            repositories.Career.Entity!.Id);

        Assert.Equal(2, result.MatchedCount);
        Assert.Equal(4, result.MissingCount);
        Assert.All(
            result.Items.Where(item =>
                item.Classification == SkillGapClassification.Matched),
            item => Assert.Equal("Python", item.MatchedStudentSkillName));
    }

    [Fact]
    public async Task AnalyzeAsync_SavedSkillOrderDoesNotAffectResult()
    {
        var skills = new List<StudentSkill>
        {
            Skill("SQL", SkillProficiency.Beginner),
            Skill("Advanced SQL reporting", SkillProficiency.Expert),
            Skill("Python", SkillProficiency.Intermediate)
        };
        var required = new List<string> { "SQL Querying", "Python Scripting" };
        var forward = CreateRepositories(skills, required);
        var reverse = CreateRepositories(
            skills.AsEnumerable().Reverse().ToList(),
            required);

        var first = await CreateService(forward).AnalyzeAsync(
            forward.Profile.Entity!.Id,
            forward.Career.Entity!.Id);
        var second = await CreateService(reverse).AnalyzeAsync(
            reverse.Profile.Entity!.Id,
            reverse.Career.Entity!.Id);

        Assert.Equal(ItemValues(first), ItemValues(second));
    }

    [Fact]
    public async Task AnalyzeAsync_RepeatedCallsAreEquivalentSeparateAndNonMutating()
    {
        var skills = new List<StudentSkill>
        {
            Skill("Python", SkillProficiency.Advanced),
            Skill("SQL", SkillProficiency.Beginner)
        };
        var required = new List<string>
        {
            "Python Scripting",
            "SQL Querying",
            "Communication"
        };
        var repositories = CreateRepositories(skills, required);
        var profileSnapshot = ProfileValues(repositories.Profile.Entity!);
        var careerSnapshot = CareerValues(repositories.Career.Entity!);
        var service = CreateService(repositories);

        var first = await service.AnalyzeAsync(
            repositories.Profile.Entity!.Id,
            repositories.Career.Entity!.Id);
        var second = await service.AnalyzeAsync(
            repositories.Profile.Entity!.Id,
            repositories.Career.Entity!.Id);

        Assert.NotSame(first, second);
        Assert.NotSame(first.Items, second.Items);
        Assert.Equal(ItemValues(first), ItemValues(second));
        Assert.Equal(
            (first.StudentProfileId, first.CareerProfileId, first.CareerCode,
                first.CareerTitle, first.BaselineProficiency,
                first.MissingCount, first.NeedsDevelopmentCount,
                first.MatchedCount),
            (second.StudentProfileId, second.CareerProfileId, second.CareerCode,
                second.CareerTitle, second.BaselineProficiency,
                second.MissingCount, second.NeedsDevelopmentCount,
                second.MatchedCount));
        Assert.All(first.Items.Zip(second.Items), pair =>
            Assert.NotSame(pair.First, pair.Second));
        Assert.Equal(profileSnapshot,
            ProfileValues(repositories.Profile.Entity!));
        Assert.Equal(careerSnapshot,
            CareerValues(repositories.Career.Entity!));
    }

    [Fact]
    public void SummaryCounts_AreDerivedNullSafeAndTrackChanges()
    {
        var result = new SkillGapResult
        {
            Items =
            [
                new SkillGapItem
                {
                    Classification = SkillGapClassification.Missing
                },
                new SkillGapItem
                {
                    Classification = SkillGapClassification.Matched
                },
                null!
            ]
        };

        Assert.Equal(1, result.MissingCount);
        Assert.Equal(0, result.NeedsDevelopmentCount);
        Assert.Equal(1, result.MatchedCount);

        result.Items[0].Classification =
            SkillGapClassification.NeedsDevelopment;

        Assert.Equal(0, result.MissingCount);
        Assert.Equal(1, result.NeedsDevelopmentCount);

        result.Items = null!;
        Assert.Equal(0, result.MissingCount);
        Assert.Equal(0, result.NeedsDevelopmentCount);
        Assert.Equal(0, result.MatchedCount);
    }

    [Fact]
    public async Task AnalyzeAsync_DuplicateNormalizedRequirements_IsRejected()
    {
        var repositories = CreateRepositories(
            [],
            ["SQL Querying", " sql   querying "]);

        await AssertMalformedAsync(repositories);
    }

    [Fact]
    public async Task AnalyzeAsync_NullSkillCollection_IsRejected()
    {
        var repositories = CreateRepositories();
        repositories.Profile.Entity!.Skills = null!;

        await AssertMalformedAsync(repositories);
    }

    [Fact]
    public async Task AnalyzeAsync_NullSkillItem_IsRejected()
    {
        var repositories = CreateRepositories();
        repositories.Profile.Entity!.Skills[0] = null!;

        await AssertMalformedAsync(repositories);
    }

    [Fact]
    public async Task AnalyzeAsync_UndefinedProficiency_IsRejected()
    {
        var repositories = CreateRepositories();
        repositories.Profile.Entity!.Skills[0].Proficiency =
            (SkillProficiency)999;

        await AssertMalformedAsync(repositories);
    }

    [Fact]
    public async Task AnalyzeAsync_BlankSavedSkillName_IsIgnored()
    {
        var repositories = CreateRepositories(
            [Skill("   ", SkillProficiency.Expert)],
            ["SQL Querying"]);

        var result = await CreateService(repositories).AnalyzeAsync(
            repositories.Profile.Entity!.Id,
            repositories.Career.Entity!.Id);

        var item = result.Items.Single(candidate =>
            candidate.RequiredSkillName == "SQL Querying");
        Assert.Equal(SkillGapClassification.Missing, item.Classification);
    }

    [Fact]
    public async Task AnalyzeAsync_NullRequiredSkills_IsRejected()
    {
        var repositories = CreateRepositories();
        repositories.Career.Entity!.RequiredSkills = null!;

        await AssertMalformedAsync(repositories);
    }

    [Fact]
    public async Task AnalyzeAsync_EmptyRequiredSkills_IsRejected()
    {
        await AssertMalformedAsync(CreateRepositories(
            requiredSkills: [],
            ensureMinimumRequiredSkills: false));
    }

    [Fact]
    public async Task AnalyzeAsync_FiveRequiredSkills_IsRejected()
    {
        var repositories = CreateRepositories(
            requiredSkills:
            [
                "Skill One",
                "Skill Two",
                "Skill Three",
                "Skill Four",
                "Skill Five"
            ],
            ensureMinimumRequiredSkills: false);

        await AssertMalformedAsync(repositories);
    }

    [Fact]
    public async Task AnalyzeAsync_SixRequiredSkills_ReturnsSixItems()
    {
        var repositories = CreateRepositories(
            skills: [],
            requiredSkills:
            [
                "Skill One",
                "Skill Two",
                "Skill Three",
                "Skill Four",
                "Skill Five",
                "Skill Six"
            ]);

        var result = await CreateService(repositories).AnalyzeAsync(
            repositories.Profile.Entity!.Id,
            repositories.Career.Entity!.Id);

        Assert.Equal(6, result.Items.Count);
        Assert.Equal(6, result.MissingCount);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task AnalyzeAsync_NullOrBlankRequiredSkill_IsRejected(
        string? requiredSkill)
    {
        var repositories = CreateRepositories(
            requiredSkills: [requiredSkill!]);

        await AssertMalformedAsync(repositories);
    }

    [Fact]
    public async Task AnalyzeAsync_NeverInvokesRepositoryWrites()
    {
        var repositories = CreateRepositories();

        await CreateService(repositories).AnalyzeAsync(
            repositories.Profile.Entity!.Id,
            repositories.Career.Entity!.Id);

        Assert.Equal(0, repositories.Profile.WriteCalls);
        Assert.Equal(0, repositories.Career.WriteCalls);
    }

    [Fact]
    public async Task AnalyzeAsync_ProfileRepositoryFailure_PropagatesUnchanged()
    {
        var repositories = CreateRepositories();
        var expected = new DataException("Distinct profile repository failure.");
        repositories.Profile.ExceptionToThrow = expected;

        var actual = await Assert.ThrowsAsync<DataException>(() =>
            CreateService(repositories).AnalyzeAsync(
                repositories.Profile.Entity!.Id,
                repositories.Career.Entity!.Id));

        Assert.Same(expected, actual);
        Assert.Equal(0, repositories.Career.GetByIdCalls);
    }

    [Fact]
    public async Task AnalyzeAsync_CareerRepositoryFailure_PropagatesUnchanged()
    {
        var repositories = CreateRepositories();
        var expected = new DataException("Distinct career repository failure.");
        repositories.Career.ExceptionToThrow = expected;

        var actual = await Assert.ThrowsAsync<DataException>(() =>
            CreateService(repositories).AnalyzeAsync(
                repositories.Profile.Entity!.Id,
                repositories.Career.Entity!.Id));

        Assert.Same(expected, actual);
    }

    [Fact]
    public void Constructor_HasOnlyApprovedDependencies()
    {
        var constructor = Assert.Single(
            typeof(SkillGapService).GetConstructors());

        Assert.Equal(
            [
                typeof(IStudentProfileRepository),
                typeof(ICareerRepository),
                typeof(SkillGapResultValidator)
            ],
            constructor.GetParameters()
                .Select(parameter => parameter.ParameterType));
    }

    private static SkillGapService CreateService(
        RepositoryPair repositories)
    {
        return new SkillGapService(
            repositories.Profile,
            repositories.Career,
            new SkillGapResultValidator());
    }

    private static RepositoryPair CreateRepositories(
        List<StudentSkill>? skills = null,
        List<string>? requiredSkills = null,
        bool ensureMinimumRequiredSkills = true)
    {
        var profile = new StudentProfile
        {
            Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            Name = "Ama Mensah",
            Programme = "Computer Science",
            Skills = skills ?? [Skill("SQL", SkillProficiency.Beginner)]
        };
        var career = new CareerProfile
        {
            Id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            Code = "TEST-001",
            Title = "Test Career",
            Description = "A test career.",
            RequiredSkills = ensureMinimumRequiredSkills
                ? CreateValidRequiredSkills(
                    requiredSkills ?? ["SQL Querying"])
                : requiredSkills ?? []
        };

        return new RepositoryPair(
            new ProfileRepositoryDouble(profile),
            new CareerRepositoryDouble(career));
    }

    private static List<string> CreateValidRequiredSkills(
        IEnumerable<string> requiredSkills)
    {
        var result = requiredSkills.ToList();
        var normalizedNames = result
            .Select(SkillNameNormalizer.Normalize)
            .ToHashSet(StringComparer.Ordinal);

        string[] fillers =
        [
            "Communication",
            "Problem Solving",
            "Technical Documentation",
            "Testing Practices",
            "Security Awareness",
            "Team Collaboration"
        ];

        foreach (var filler in fillers)
        {
            if (result.Count >= 6)
            {
                break;
            }

            if (normalizedNames.Add(SkillNameNormalizer.Normalize(filler)))
            {
                result.Add(filler);
            }
        }

        return result;
    }

    private static StudentSkill Skill(
        string name,
        SkillProficiency proficiency)
    {
        return new StudentSkill
        {
            Id = Guid.NewGuid(),
            StudentProfileId =
                Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            SkillName = name,
            Proficiency = proficiency
        };
    }

    private static async Task AssertMalformedAsync(
        RepositoryPair repositories)
    {
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => CreateService(repositories).AnalyzeAsync(
                repositories.Profile.Entity!.Id,
                repositories.Career.Entity!.Id));

        Assert.Equal(
            "Skill-gap analysis could not be completed because the saved " +
            "profile or career catalogue data is invalid.",
            exception.Message);
    }

    private static string[] ItemValues(SkillGapResult result)
    {
        return result.Items.Select(item => string.Join(
            "|",
            item.RequiredSkillName,
            item.Classification,
            item.MatchedStudentSkillName,
            item.CurrentProficiency)).ToArray();
    }

    private static string[] ProfileValues(StudentProfile profile)
    {
        return profile.Skills.Select(skill => string.Join(
            "|",
            skill.Id,
            skill.StudentProfileId,
            skill.SkillName,
            skill.Proficiency)).ToArray();
    }

    private static string[] CareerValues(CareerProfile career)
    {
        return career.RequiredSkills.ToArray();
    }

    private sealed record RepositoryPair(
        ProfileRepositoryDouble Profile,
        CareerRepositoryDouble Career);

    private class RepositoryDouble<T>(T? entity) : IRepository<T>
        where T : class
    {
        public T? Entity { get; set; } = entity;
        public int GetByIdCalls { get; private set; }
        public int WriteCalls { get; private set; }
        public Exception? ExceptionToThrow { get; set; }

        public Task<T?> GetByIdAsync(Guid id)
        {
            GetByIdCalls++;

            if (ExceptionToThrow is not null)
            {
                return Task.FromException<T?>(ExceptionToThrow);
            }

            return Task.FromResult(Entity);
        }

        public Task<IEnumerable<T>> GetAllAsync() =>
            Task.FromResult<IEnumerable<T>>(
                Entity is null ? [] : [Entity]);

        public Task AddAsync(T entity)
        {
            WriteCalls++;
            return Task.CompletedTask;
        }

        public Task UpdateAsync(T entity)
        {
            WriteCalls++;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Guid id)
        {
            WriteCalls++;
            return Task.CompletedTask;
        }
    }

    private sealed class ProfileRepositoryDouble(StudentProfile? profile)
        : RepositoryDouble<StudentProfile>(profile),
            IStudentProfileRepository
    {
    }

    private sealed class CareerRepositoryDouble(CareerProfile? career)
        : RepositoryDouble<CareerProfile>(career), ICareerRepository
    {
        public Task<CareerProfile?> GetByCodeAsync(string code) =>
            Task.FromResult(Entity);
    }
}
