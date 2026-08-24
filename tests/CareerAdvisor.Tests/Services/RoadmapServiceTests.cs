using CareerAdvisor.Core.Enums;
using CareerAdvisor.Core.Interfaces;
using CareerAdvisor.Core.Models;
using CareerAdvisor.Core.Validators;
using CareerAdvisor.Infrastructure.Services;
using System.Text.Json;

namespace CareerAdvisor.Tests.Services;

public sealed class RoadmapServiceTests
{
    private static readonly DateTime FixedUtc =
        new(2026, 8, 24, 12, 30, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Generate_EmptyProfile_IsRejectedBeforeAnyLookup()
    {
        var state = CreateState();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CreateService(state).GenerateRoadmapAsync(
                Guid.Empty,
                state.Career.Entity!.Id));

        Assert.Equal(0, state.Profile.GetByIdCalls);
        Assert.Equal(0, state.Career.GetByIdCalls);
        Assert.Equal(0, state.Roadmap.AddCalls);
    }

    [Fact]
    public async Task Generate_UnknownOrMismatchedProfile_StopsBeforeCareerLookup()
    {
        foreach (var profile in new StudentProfile?[]
                 {
                     null,
                     CreateProfile(Guid.NewGuid())
                 })
        {
            var state = CreateState();
            state.Profile.Entity = profile;

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                CreateService(state).GenerateRoadmapAsync(
                    Guid.NewGuid(),
                    state.Career.Entity!.Id));

            Assert.Equal(0, state.Career.GetByIdCalls);
            Assert.Equal(0, state.Roadmap.AddCalls);
        }
    }

    [Fact]
    public async Task Generate_EmptyUnknownOrMismatchedCareer_IsRejected()
    {
        var empty = CreateState();
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CreateService(empty).GenerateRoadmapAsync(
                empty.Profile.Entity!.Id,
                Guid.Empty));
        Assert.Equal(0, empty.Career.GetByIdCalls);

        foreach (var career in new CareerProfile?[]
                 {
                     null,
                     CreateCareer(Guid.NewGuid())
                 })
        {
            var state = CreateState();
            state.Career.Entity = career;

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                CreateService(state).GenerateRoadmapAsync(
                    state.Profile.Entity!.Id,
                    Guid.NewGuid()));

            Assert.Equal(0, state.Recommendations.GetByProfileCalls);
            Assert.Equal(0, state.Roadmap.AddCalls);
        }
    }

    [Fact]
    public async Task Generate_AnyOwnedHistoricalRecommendationQualifies()
    {
        var state = CreateState();
        var reverseState = CreateState(
            state.Profile.Entity!.Id,
            state.Career.Entity!.Id);
        var unrelatedCareerId = Guid.NewGuid();
        var newest = CreateSession(
            state.Profile.Entity.Id,
            unrelatedCareerId);
        newest.GeneratedAt = FixedUtc;
        var older = CreateSession(
            state.Profile.Entity.Id,
            state.Career.Entity.Id);
        older.GeneratedAt = FixedUtc.AddDays(-1);
        state.Recommendations.Sessions = [newest, older];
        reverseState.Recommendations.Sessions = [older, newest];

        var forward = await CreateService(state).GenerateRoadmapAsync(
            state.Profile.Entity.Id,
            state.Career.Entity.Id);
        var reverse = await CreateService(reverseState).GenerateRoadmapAsync(
            reverseState.Profile.Entity!.Id,
            reverseState.Career.Entity!.Id);

        Assert.Equal(state.Career.Entity.Id, forward.CareerProfileId);
        Assert.Equal(reverseState.Career.Entity.Id, reverse.CareerProfileId);
        Assert.Equal(
            forward.Steps.Select(StepValue),
            reverse.Steps.Select(StepValue));
        Assert.Equal(1, state.SkillGap.Calls);
        Assert.Equal(1, reverseState.SkillGap.Calls);
        Assert.Equal(1, state.Roadmap.AddCalls);
        Assert.Equal(1, reverseState.Roadmap.AddCalls);
    }

    [Fact]
    public async Task Generate_RecommendationOrderDoesNotAffectAuthorization()
    {
        var state = CreateState();
        List<RecommendationSession> sessions =
        [
            CreateSession(state.Profile.Entity!.Id, Guid.NewGuid()),
            CreateSession(state.Profile.Entity.Id, state.Career.Entity!.Id)
        ];
        state.Recommendations.Sessions = sessions.AsEnumerable()
            .Reverse()
            .ToList();

        var roadmap = await CreateService(state).GenerateRoadmapAsync(
            state.Profile.Entity.Id,
            state.Career.Entity.Id);

        Assert.Equal(state.Career.Entity.Id, roadmap.CareerProfileId);
        Assert.Equal(1, state.SkillGap.Calls);
        Assert.Equal(1, state.Roadmap.AddCalls);
    }

    [Fact]
    public async Task Generate_NoSessionAbsentCareerOrCrossProfileData_IsRejectedWithoutWrite()
    {
        var cases = new Action<TestState>[]
        {
            state => state.Recommendations.Sessions = [],
            state => state.Recommendations.Sessions =
                [CreateSession(state.Profile.Entity!.Id, Guid.NewGuid())],
            state => state.Recommendations.Sessions =
                [CreateSession(Guid.NewGuid(), state.Career.Entity!.Id)]
        };

        foreach (var arrange in cases)
        {
            var state = CreateState();
            arrange(state);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                CreateService(state).GenerateRoadmapAsync(
                    state.Profile.Entity!.Id,
                    state.Career.Entity!.Id));

            Assert.Equal(0, state.SkillGap.Calls);
            Assert.Equal(0, state.Roadmap.AddCalls);
        }
    }

    [Fact]
    public async Task Generate_MalformedRecommendationCollection_IsRejected()
    {
        var state = CreateState();
        state.Recommendations.Sessions[0].Recommendations = null!;

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => CreateService(state).GenerateRoadmapAsync(
                state.Profile.Entity!.Id,
                state.Career.Entity!.Id));

        Assert.Contains("recommendation data", exception.Message);
        Assert.Equal(0, state.Roadmap.AddCalls);
    }

    [Fact]
    public async Task Generate_ExistingValidRoadmap_ReturnsUnchangedWithoutAnalysisOrInsert()
    {
        var state = CreateState();
        var existing = CreateRoadmap(
            state.Profile.Entity!.Id,
            state.Career.Entity!.Id);
        existing.Steps[0].IsCompleted = true;
        existing.Steps[0].CompletedAt = FixedUtc.AddDays(-1);
        state.Roadmap.Stored = existing;

        var result = await CreateService(state).GenerateRoadmapAsync(
            state.Profile.Entity.Id,
            state.Career.Entity.Id);

        Assert.Same(existing, result);
        Assert.Equal(FixedUtc.AddDays(-1), result.Steps[0].CompletedAt);
        Assert.Equal(0, state.SkillGap.Calls);
        Assert.Equal(0, state.Roadmap.AddCalls);
    }

    [Fact]
    public async Task Generate_MissingDevelopingTopicsAndCertifications_UsesExactPriorityTemplatesAndCaps()
    {
        var state = CreateState();
        state.SkillGap.Result = CreateGap(
            state,
            ("Skill F", SkillGapClassification.Missing),
            ("Skill E", SkillGapClassification.Missing),
            ("Skill D", SkillGapClassification.Missing),
            ("Skill C", SkillGapClassification.Missing),
            ("Skill B", SkillGapClassification.NeedsDevelopment),
            ("Skill A", SkillGapClassification.Matched));

        var roadmap = await CreateService(state).GenerateRoadmapAsync(
            state.Profile.Entity!.Id,
            state.Career.Entity!.Id);

        Assert.Equal(11, roadmap.Steps.Count);
        Assert.Equal(Enumerable.Range(1, 11),
            roadmap.Steps.Select(step => step.Order));
        Assert.Equal(
            [
                "Build foundation: Skill C",
                "Build foundation: Skill D",
                "Build foundation: Skill E",
                "Build foundation: Skill F",
                "Develop further: Skill B"
            ],
            roadmap.Steps.Take(5).Select(step => step.Title));
        Assert.DoesNotContain(roadmap.Steps,
            step => step.Title.Contains("Skill A"));
        Assert.Equal(
            "Develop foundational academic knowledge and practice in Skill C " +
            $"for {state.Career.Entity.Title}.",
            roadmap.Steps[0].Description);
        Assert.Equal(
            "Strengthen your current Beginner proficiency in Skill B toward " +
            "the Intermediate academic MVP baseline.",
            roadmap.Steps[4].Description);
        var topic = roadmap.Steps.Single(step =>
            step.Title == "Study topic: Topic A");
        Assert.Equal(
            "Study and practise Topic A as a suggested learning topic for " +
            $"{state.Career.Entity!.Title}.",
            topic.Description);
        var certification = roadmap.Steps.Single(step =>
            step.Title == "Explore certification: Certification A");
        Assert.Equal(
            "Review the published requirements and syllabus for " +
            "Certification A. This is exploration guidance, not enrolment " +
            "or a guarantee of certification or employment.",
            certification.Description);
        Assert.All(roadmap.Steps,
            step => Assert.Equal(string.Empty, step.ResourceLink));
        Assert.All(roadmap.Steps, step => Assert.False(step.IsCompleted));
        Assert.Equal(FixedUtc, roadmap.CreatedAt);
    }

    [Fact]
    public async Task Generate_SixMissingPlusFourTopicsPlusTwoCertifications_IsBoundedAtTwelve()
    {
        var state = CreateState();
        state.SkillGap.Result = CreateGap(state,
            state.Career.Entity!.RequiredSkills
                .Select(name => (name, SkillGapClassification.Missing))
                .ToArray());

        var roadmap = await CreateService(state).GenerateRoadmapAsync(
            state.Profile.Entity!.Id,
            state.Career.Entity.Id);

        Assert.Equal(12, roadmap.Steps.Count);
        Assert.Equal(6, roadmap.Steps.Count(step =>
            step.Title.StartsWith("Build foundation:",
                StringComparison.Ordinal)));
        Assert.Equal(4, roadmap.Steps.Count(step =>
            step.Title.StartsWith("Study topic:",
                StringComparison.Ordinal)));
        Assert.Equal(2, roadmap.Steps.Count(step =>
            step.Title.StartsWith("Explore certification:",
                StringComparison.Ordinal)));
    }

    [Fact]
    public async Task Generate_MissingAndDevelopingShareSixSkillStepCap()
    {
        var state = CreateState();
        var values = new (string, SkillGapClassification)[]
        {
            ("Develop Echo", SkillGapClassification.NeedsDevelopment),
            ("Missing Delta", SkillGapClassification.Missing),
            ("Develop Bravo", SkillGapClassification.NeedsDevelopment),
            ("Missing Alpha", SkillGapClassification.Missing),
            ("Develop Delta", SkillGapClassification.NeedsDevelopment),
            ("Missing Charlie", SkillGapClassification.Missing),
            ("Develop Alpha", SkillGapClassification.NeedsDevelopment),
            ("Missing Bravo", SkillGapClassification.Missing),
            ("Develop Charlie", SkillGapClassification.NeedsDevelopment)
        };
        state.Career.Entity!.RequiredSkills = values
            .Select(value => value.Item1)
            .ToList();
        state.SkillGap.Result = CreateGap(state, values);

        var roadmap = await CreateService(state).GenerateRoadmapAsync(
            state.Profile.Entity!.Id,
            state.Career.Entity.Id);

        Assert.Equal(
            [
                "Build foundation: Missing Alpha",
                "Build foundation: Missing Bravo",
                "Build foundation: Missing Charlie",
                "Build foundation: Missing Delta",
                "Develop further: Develop Alpha",
                "Develop further: Develop Bravo"
            ],
            roadmap.Steps.Take(6).Select(step => step.Title));
        Assert.Equal(6, roadmap.Steps.Count(step =>
            step.Title.StartsWith("Build foundation:", StringComparison.Ordinal) ||
            step.Title.StartsWith("Develop further:", StringComparison.Ordinal)));
        Assert.Equal(Enumerable.Range(1, roadmap.Steps.Count),
            roadmap.Steps.Select(step => step.Order));
    }

    [Fact]
    public async Task Generate_CrossCategoryDuplicate_HighestPriorityWins()
    {
        var state = CreateState();
        state.Career.Entity!.SuggestedLearningTopics[0] = "Skill A";
        state.SkillGap.Result = CreateGap(
            state,
            ("Skill A", SkillGapClassification.Missing),
            ("Skill B", SkillGapClassification.Matched),
            ("Skill C", SkillGapClassification.Matched),
            ("Skill D", SkillGapClassification.Matched),
            ("Skill E", SkillGapClassification.Matched),
            ("Skill F", SkillGapClassification.Matched));

        var roadmap = await CreateService(state).GenerateRoadmapAsync(
            state.Profile.Entity!.Id,
            state.Career.Entity.Id);

        Assert.Contains(roadmap.Steps,
            step => step.Title == "Build foundation: Skill A");
        Assert.DoesNotContain(roadmap.Steps,
            step => step.Title == "Study topic: Skill A");
    }

    [Fact]
    public async Task Generate_DuplicateNormalizedCatalogueSource_IsRejectedWithoutWrite()
    {
        var mutations = new Action<CareerProfile>[]
        {
            career => career.RequiredSkills[1] = " skill   a ",
            career => career.SuggestedLearningTopics =
                ["Data Ethics", " data   ethics "],
            career => career.RecommendedCertifications =
                ["Cloud Certificate", " cloud   certificate "]
        };

        foreach (var mutate in mutations)
        {
            var state = CreateState();
            mutate(state.Career.Entity!);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                CreateService(state).GenerateRoadmapAsync(
                    state.Profile.Entity!.Id,
                    state.Career.Entity!.Id));

            Assert.Equal(0, state.Roadmap.AddCalls);
        }
    }

    [Fact]
    public async Task Generate_DuplicatesBeyondCategoryCaps_AreRejectedBeforeWrite()
    {
        var mutations = new Action<CareerProfile>[]
        {
            career => career.SuggestedLearningTopics =
            [
                "Topic Alpha",
                "Topic Bravo",
                "Topic Charlie",
                "Topic Delta",
                "Topic Echo",
                "TOPIC ALPHA"
            ],
            career => career.RecommendedCertifications =
            [
                "Certification Alpha",
                "Certification Bravo",
                "Certification Charlie",
                "CERTIFICATION ALPHA"
            ]
        };

        foreach (var mutate in mutations)
        {
            var state = CreateState();
            mutate(state.Career.Entity!);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                CreateService(state).GenerateRoadmapAsync(
                    state.Profile.Entity!.Id,
                    state.Career.Entity!.Id));

            Assert.Equal(0, state.Roadmap.AddCalls);
        }
    }

    [Fact]
    public async Task Generate_FewerThanSixRequiredSkills_IsRejectedWithoutWrite()
    {
        var state = CreateState();
        state.Career.Entity!.RequiredSkills.RemoveAt(5);
        state.SkillGap.Result!.Items.RemoveAt(5);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CreateService(state).GenerateRoadmapAsync(
                state.Profile.Entity!.Id,
                state.Career.Entity.Id));

        Assert.Equal(0, state.Roadmap.AddCalls);
    }

    [Fact]
    public async Task Generate_InconsistentSkillGapClassification_IsRejected()
    {
        var state = CreateState();
        var item = state.SkillGap.Result!.Items[0];
        item.Classification = SkillGapClassification.NeedsDevelopment;
        item.MatchedStudentSkillName = "Skill F";
        item.CurrentProficiency = SkillProficiency.Advanced;

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CreateService(state).GenerateRoadmapAsync(
                state.Profile.Entity!.Id,
                state.Career.Entity!.Id));

        Assert.Equal(0, state.Roadmap.AddCalls);
    }

    [Fact]
    public async Task Generate_ForwardAndReverseInputs_ProduceEquivalentOrderedContent()
    {
        var forward = CreateState();
        var reverse = CreateState(
            forward.Profile.Entity!.Id,
            forward.Career.Entity!.Id);

        reverse.Career.Entity!.RequiredSkills.Reverse();
        reverse.Career.Entity.SuggestedLearningTopics.Reverse();
        reverse.Career.Entity.RecommendedCertifications.Reverse();
        reverse.SkillGap.Result!.Items.Reverse();

        var first = await CreateService(forward).GenerateRoadmapAsync(
            forward.Profile.Entity.Id,
            forward.Career.Entity.Id);
        var second = await CreateService(reverse).GenerateRoadmapAsync(
            reverse.Profile.Entity!.Id,
            reverse.Career.Entity.Id);

        Assert.Equal(
            first.Steps.Select(StepValue),
            second.Steps.Select(StepValue));
    }

    [Fact]
    public async Task Generate_AllMatchedWithNoGuidance_IsRejectedWithoutWrite()
    {
        var state = CreateState();
        state.Career.Entity!.SuggestedLearningTopics = [];
        state.Career.Entity.RecommendedCertifications = [];
        state.SkillGap.Result = CreateGap(state,
            state.Career.Entity.RequiredSkills
                .Select(name => (name, SkillGapClassification.Matched))
                .ToArray());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CreateService(state).GenerateRoadmapAsync(
                state.Profile.Entity!.Id,
                state.Career.Entity.Id));

        Assert.Equal(0, state.Roadmap.AddCalls);
    }

    [Fact]
    public async Task Generate_MismatchedSkillGapIdentity_IsRejectedWithoutWrite()
    {
        var state = CreateState();
        state.SkillGap.Result!.CareerProfileId = Guid.NewGuid();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CreateService(state).GenerateRoadmapAsync(
                state.Profile.Entity!.Id,
                state.Career.Entity!.Id));

        Assert.Equal(0, state.Roadmap.AddCalls);
    }

    [Fact]
    public async Task Generate_ChangedSkillGapDisplayName_IsRejectedWithoutWrite()
    {
        var state = CreateState();
        state.SkillGap.Result!.Items[0].RequiredSkillName = " skill   f ";

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CreateService(state).GenerateRoadmapAsync(
                state.Profile.Entity!.Id,
                state.Career.Entity!.Id));

        Assert.Equal(0, state.Roadmap.AddCalls);
    }

    [Fact]
    public async Task Generate_SuccessAndFailure_DoNotMutateAnySourceData()
    {
        var success = CreateState();
        success.Profile.Entity!.Skills =
        [
            new StudentSkill
            {
                Id = Guid.NewGuid(),
                StudentProfileId = success.Profile.Entity.Id,
                SkillName = "Skill B",
                Proficiency = SkillProficiency.Beginner
            },
            new StudentSkill
            {
                Id = Guid.NewGuid(),
                StudentProfileId = success.Profile.Entity.Id,
                SkillName = "Skill A",
                Proficiency = SkillProficiency.Advanced
            }
        ];
        var successSnapshot = SnapshotSources(success);

        await CreateService(success).GenerateRoadmapAsync(
            success.Profile.Entity.Id,
            success.Career.Entity!.Id);

        Assert.Equal(successSnapshot, SnapshotSources(success));

        var failure = CreateState();
        failure.Career.Entity!.SuggestedLearningTopics =
            ["Topic Alpha", "TOPIC ALPHA"];
        var failureSnapshot = SnapshotSources(failure);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CreateService(failure).GenerateRoadmapAsync(
                failure.Profile.Entity!.Id,
                failure.Career.Entity.Id));

        Assert.Equal(failureSnapshot, SnapshotSources(failure));
        Assert.Equal(0, failure.Roadmap.AddCalls);
    }

    [Fact]
    public async Task Get_CrossProfileAndUnknownRoadmapHaveSameOutwardResult()
    {
        var state = CreateState();
        state.Roadmap.Stored = CreateRoadmap(
            Guid.NewGuid(),
            state.Career.Entity!.Id);
        var service = CreateService(state);

        var crossProfile = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.GetRoadmapAsync(
                state.Profile.Entity!.Id,
                state.Roadmap.Stored.Id));
        var unknown = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.GetRoadmapAsync(
                state.Profile.Entity!.Id,
                Guid.NewGuid()));

        Assert.Equal(unknown.Message, crossProfile.Message);
    }

    [Fact]
    public async Task Update_CompletionIsIdempotentAndUncompletionClearsTimestamp()
    {
        var state = CreateState();
        state.Roadmap.Stored = CreateRoadmap(
            state.Profile.Entity!.Id,
            state.Career.Entity!.Id);
        var service = CreateService(state);
        var stepId = state.Roadmap.Stored.Steps[0].Id;

        var completed = await service.UpdateRoadmapProgressAsync(
            state.Profile.Entity.Id,
            state.Roadmap.Stored.Id,
            stepId,
            true);
        Assert.True(completed.Steps[0].IsCompleted);
        Assert.Equal(FixedUtc, completed.Steps[0].CompletedAt);
        Assert.Equal(1, state.Roadmap.UpdateCalls);

        var completedAgain = await service.UpdateRoadmapProgressAsync(
            state.Profile.Entity.Id,
            state.Roadmap.Stored.Id,
            stepId,
            true);
        Assert.Equal(FixedUtc, completedAgain.Steps[0].CompletedAt);
        Assert.Equal(1, state.Roadmap.UpdateCalls);

        var incomplete = await service.UpdateRoadmapProgressAsync(
            state.Profile.Entity.Id,
            state.Roadmap.Stored.Id,
            stepId,
            false);
        Assert.False(incomplete.Steps[0].IsCompleted);
        Assert.Null(incomplete.Steps[0].CompletedAt);
        Assert.Equal(2, state.Roadmap.UpdateCalls);

        await service.UpdateRoadmapProgressAsync(
            state.Profile.Entity.Id,
            state.Roadmap.Stored.Id,
            stepId,
            false);
        Assert.Equal(2, state.Roadmap.UpdateCalls);
    }

    [Fact]
    public async Task Update_UnknownAndCrossRoadmapStepsHaveSameOutwardResult()
    {
        var state = CreateState();
        state.Roadmap.Stored = CreateRoadmap(
            state.Profile.Entity!.Id,
            state.Career.Entity!.Id);
        var service = CreateService(state);

        var unknown = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.UpdateRoadmapProgressAsync(
                state.Profile.Entity.Id,
                state.Roadmap.Stored.Id,
                Guid.NewGuid(),
                true));

        var otherRoadmapStep = CreateRoadmap(
            state.Profile.Entity.Id,
            state.Career.Entity.Id).Steps[0].Id;
        var cross = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.UpdateRoadmapProgressAsync(
                state.Profile.Entity.Id,
                state.Roadmap.Stored.Id,
                otherRoadmapStep,
                true));

        Assert.Equal(unknown.Message, cross.Message);
        Assert.Equal(0, state.Roadmap.UpdateCalls);
    }

    [Fact]
    public async Task UnexpectedDependencyExceptionsPropagateUnchanged()
    {
        var expected = new TestDependencyException();
        var state = CreateState();
        state.Profile.Exception = expected;

        var actual = await Assert.ThrowsAsync<TestDependencyException>(() =>
            CreateService(state).GenerateRoadmapAsync(
                state.Profile.Entity!.Id,
                state.Career.Entity!.Id));

        Assert.Same(expected, actual);

        state = CreateState();
        state.Career.Exception = expected;
        actual = await Assert.ThrowsAsync<TestDependencyException>(() =>
            CreateService(state).GenerateRoadmapAsync(
                state.Profile.Entity!.Id,
                state.Career.Entity!.Id));
        Assert.Same(expected, actual);

        state = CreateState();
        state.Recommendations.Exception = expected;
        actual = await Assert.ThrowsAsync<TestDependencyException>(() =>
            CreateService(state).GenerateRoadmapAsync(
                state.Profile.Entity!.Id,
                state.Career.Entity!.Id));
        Assert.Same(expected, actual);

        state = CreateState();
        state.Roadmap.Exception = expected;
        actual = await Assert.ThrowsAsync<TestDependencyException>(() =>
            CreateService(state).GenerateRoadmapAsync(
                state.Profile.Entity!.Id,
                state.Career.Entity!.Id));
        Assert.Same(expected, actual);

        state = CreateState();
        state.SkillGap.Exception = expected;
        actual = await Assert.ThrowsAsync<TestDependencyException>(() =>
            CreateService(state).GenerateRoadmapAsync(
                state.Profile.Entity!.Id,
                state.Career.Entity!.Id));
        Assert.Same(expected, actual);
    }

    private static RoadmapService CreateService(TestState state)
    {
        return new RoadmapService(
            state.Profile,
            state.Career,
            state.Recommendations,
            state.Roadmap,
            state.SkillGap,
            new SkillGapResultValidator(),
            new LearningRoadmapValidator(),
            new FixedTimeProvider(FixedUtc));
    }

    private static TestState CreateState(
        Guid? profileId = null,
        Guid? careerId = null)
    {
        var profile = CreateProfile(profileId ?? Guid.NewGuid());
        var career = CreateCareer(careerId ?? Guid.NewGuid());
        var state = new TestState
        {
            Profile = new ProfileRepositoryDouble { Entity = profile },
            Career = new CareerRepositoryDouble { Entity = career },
            Recommendations = new RecommendationRepositoryDouble
            {
                Sessions = [CreateSession(profile.Id, career.Id)]
            },
            Roadmap = new RoadmapRepositoryDouble()
        };

        state.SkillGap = new SkillGapServiceDouble
        {
            Result = CreateGap(state,
                career.RequiredSkills
                    .Select(name => (name, SkillGapClassification.Missing))
                    .ToArray())
        };
        return state;
    }

    private static StudentProfile CreateProfile(Guid id) => new()
    {
        Id = id,
        Name = "Ama Mensah",
        Programme = "Computer Science"
    };

    private static CareerProfile CreateCareer(Guid id) => new()
    {
        Id = id,
        Code = "TEST",
        Title = "Test Engineer",
        Description = "A supported test career.",
        RequiredSkills =
            ["Skill F", "Skill E", "Skill D", "Skill C", "Skill B", "Skill A"],
        SuggestedLearningTopics =
            ["Topic D", "Topic C", "Topic B", "Topic A"],
        RecommendedCertifications = ["Certification B", "Certification A"]
    };

    private static RecommendationSession CreateSession(
        Guid profileId,
        Guid selectedCareerId)
    {
        var sessionId = Guid.NewGuid();
        return new RecommendationSession
        {
            Id = sessionId,
            StudentProfileId = profileId,
            Recommendations =
            [
                CreateRecommendation(sessionId, selectedCareerId),
                CreateRecommendation(sessionId, Guid.NewGuid()),
                CreateRecommendation(sessionId, Guid.NewGuid())
            ]
        };
    }

    private static CareerRecommendation CreateRecommendation(
        Guid sessionId,
        Guid careerId) => new()
        {
            Id = Guid.NewGuid(),
            RecommendationSessionId = sessionId,
            CareerProfileId = careerId,
            MatchScore = 0.8,
            Reasoning = "Persisted explanation."
        };

    private static SkillGapResult CreateGap(
        TestState state,
        params (string Name, SkillGapClassification Classification)[] values)
    {
        return new SkillGapResult
        {
            StudentProfileId = state.Profile.Entity!.Id,
            CareerProfileId = state.Career.Entity!.Id,
            CareerCode = state.Career.Entity.Code,
            CareerTitle = state.Career.Entity.Title,
            BaselineProficiency = SkillProficiency.Intermediate,
            Items = values.Select(value => new SkillGapItem
            {
                RequiredSkillName = value.Name,
                Classification = value.Classification,
                MatchedStudentSkillName = value.Classification ==
                    SkillGapClassification.Missing
                        ? null
                        : value.Name,
                CurrentProficiency = value.Classification switch
                {
                    SkillGapClassification.Missing => null,
                    SkillGapClassification.NeedsDevelopment =>
                        SkillProficiency.Beginner,
                    _ => SkillProficiency.Intermediate
                }
            }).ToList()
        };
    }

    private static LearningRoadmap CreateRoadmap(
        Guid profileId,
        Guid careerId)
    {
        var roadmap = new LearningRoadmap
        {
            Id = Guid.NewGuid(),
            StudentProfileId = profileId,
            CareerProfileId = careerId,
            CreatedAt = FixedUtc
        };
        roadmap.Steps =
        [
            new RoadmapStep
            {
                Id = Guid.NewGuid(),
                LearningRoadmapId = roadmap.Id,
                Order = 1,
                Title = "Step one",
                Description = "First valid roadmap step.",
                ResourceLink = string.Empty
            }
        ];
        return roadmap;
    }

    private static (int, string, string, string) StepValue(RoadmapStep step) =>
        (step.Order, step.Title, step.Description, step.ResourceLink);

    private static string SnapshotSources(TestState state)
    {
        return JsonSerializer.Serialize(new
        {
            Profile = state.Profile.Entity,
            Career = state.Career.Entity,
            SkillGap = state.SkillGap.Result,
            RecommendationSessions = state.Recommendations.Sessions
        });
    }

    private sealed class TestState
    {
        public required ProfileRepositoryDouble Profile { get; init; }
        public required CareerRepositoryDouble Career { get; init; }
        public required RecommendationRepositoryDouble Recommendations { get; init; }
        public required RoadmapRepositoryDouble Roadmap { get; init; }
        public SkillGapServiceDouble SkillGap { get; set; } = null!;
    }

    private sealed class ProfileRepositoryDouble : IStudentProfileRepository
    {
        public StudentProfile? Entity { get; set; }
        public Exception? Exception { get; set; }
        public int GetByIdCalls { get; private set; }
        public Task<StudentProfile?> GetByIdAsync(Guid id)
        {
            GetByIdCalls++;
            if (Exception is not null) throw Exception;
            return Task.FromResult(Entity);
        }
        public Task<IEnumerable<StudentProfile>> GetAllAsync() =>
            Task.FromResult<IEnumerable<StudentProfile>>([]);
        public Task AddAsync(StudentProfile entity) => throw new NotSupportedException();
        public Task UpdateAsync(StudentProfile entity) => throw new NotSupportedException();
        public Task DeleteAsync(Guid id) => throw new NotSupportedException();
    }

    private sealed class CareerRepositoryDouble : ICareerRepository
    {
        public CareerProfile? Entity { get; set; }
        public Exception? Exception { get; set; }
        public int GetByIdCalls { get; private set; }
        public Task<CareerProfile?> GetByIdAsync(Guid id)
        {
            GetByIdCalls++;
            if (Exception is not null) throw Exception;
            return Task.FromResult(Entity);
        }
        public Task<CareerProfile?> GetByCodeAsync(string code) =>
            Task.FromResult(Entity);
        public Task<IEnumerable<CareerProfile>> GetAllAsync() =>
            Task.FromResult<IEnumerable<CareerProfile>>([]);
        public Task AddAsync(CareerProfile entity) => throw new NotSupportedException();
        public Task UpdateAsync(CareerProfile entity) => throw new NotSupportedException();
        public Task DeleteAsync(Guid id) => throw new NotSupportedException();
    }

    private sealed class RecommendationRepositoryDouble
        : IRecommendationRepository
    {
        public List<RecommendationSession> Sessions { get; set; } = [];
        public Exception? Exception { get; set; }
        public int GetByProfileCalls { get; private set; }
        public Task<IEnumerable<RecommendationSession>>
            GetByStudentProfileIdAsync(Guid studentProfileId)
        {
            GetByProfileCalls++;
            if (Exception is not null) throw Exception;
            return Task.FromResult<IEnumerable<RecommendationSession>>(Sessions);
        }
        public Task<RecommendationSession?> GetByIdAsync(Guid id) =>
            Task.FromResult<RecommendationSession?>(null);
        public Task<IEnumerable<RecommendationSession>> GetAllAsync() =>
            Task.FromResult<IEnumerable<RecommendationSession>>([]);
        public Task AddAsync(RecommendationSession entity) => throw new NotSupportedException();
        public Task UpdateAsync(RecommendationSession entity) => throw new NotSupportedException();
        public Task DeleteAsync(Guid id) => throw new NotSupportedException();
    }

    private sealed class RoadmapRepositoryDouble : IRoadmapRepository
    {
        public LearningRoadmap? Stored { get; set; }
        public Exception? Exception { get; set; }
        public int AddCalls { get; private set; }
        public int UpdateCalls { get; private set; }
        public Task<LearningRoadmap?> GetByIdForProfileAsync(
            Guid studentProfileId, Guid roadmapId) => Task.FromResult(
            Stored is not null && Stored.StudentProfileId == studentProfileId &&
            Stored.Id == roadmapId ? Stored : null);
        public Task<LearningRoadmap?> GetByProfileAndCareerAsync(
            Guid studentProfileId, Guid careerProfileId)
        {
            if (Exception is not null) throw Exception;
            return Task.FromResult(
                Stored is not null &&
                Stored.StudentProfileId == studentProfileId &&
                Stored.CareerProfileId == careerProfileId ? Stored : null);
        }
        public Task<bool> TryAddAsync(LearningRoadmap roadmap)
        {
            AddCalls++;
            Stored = roadmap;
            return Task.FromResult(true);
        }
        public Task<bool> SetStepCompletionAsync(
            Guid studentProfileId, Guid roadmapId, Guid stepId,
            bool isCompleted, DateTime? completedAt)
        {
            UpdateCalls++;
            var step = Stored?.Steps.SingleOrDefault(candidate =>
                Stored.StudentProfileId == studentProfileId &&
                Stored.Id == roadmapId && candidate.Id == stepId);
            if (step is null) return Task.FromResult(false);
            step.IsCompleted = isCompleted;
            step.CompletedAt = completedAt;
            return Task.FromResult(true);
        }
    }

    private sealed class SkillGapServiceDouble : ISkillGapService
    {
        public SkillGapResult? Result { get; set; }
        public Exception? Exception { get; set; }
        public int Calls { get; private set; }
        public Task<SkillGapResult> AnalyzeAsync(
            Guid studentProfileId, Guid careerProfileId)
        {
            Calls++;
            if (Exception is not null) throw Exception;
            return Task.FromResult(Result!);
        }
    }

    private sealed class FixedTimeProvider(DateTime utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(utcNow);
    }

    private sealed class TestDependencyException : Exception;
}
