using CareerAdvisor.Core.Enums;
using CareerAdvisor.Core.Models;
using CareerAdvisor.Infrastructure.MachineLearning;

namespace CareerAdvisor.Tests.MachineLearning;

public sealed class RecommendationInputBuilderTests
{
    private readonly RecommendationInputBuilder _builder = new();

    [Fact]
    public void Build_MapsProfileAndAllAssessmentResponses()
    {
        var profile = CreateProfile();
        var assessment = CreateCompletedAssessment(profile.Id);

        var input = _builder.Build(profile, assessment);

        Assert.Equal("ComputerScience", input.AcademicBackground);
        Assert.Equal("Undergraduate", input.AcademicLevel);

        Assert.Equal(5, input.ProgrammingSkill);
        Assert.Equal(3, input.DataSkill);
        Assert.Equal(3, input.CloudSkill);

        Assert.Equal(1, input.CybersecuritySkill);
        Assert.Equal(1, input.NetworkingSkill);
        Assert.Equal(1, input.DatabaseSkill);
        Assert.Equal(1, input.DesignSkill);
        Assert.Equal(1, input.AISkill);

        Assert.Equal(5, input.TechnologyInterest);
        Assert.Equal(5, input.DataInterest);
        Assert.Equal(5, input.DesignInterest);
        Assert.Equal(5, input.LeadershipInterest);
        Assert.Equal(5, input.SocialImpactInterest);

        Assert.Equal(5, input.ProgrammingSelfAssessment);
        Assert.Equal(5, input.CommunicationSelfAssessment);
        Assert.Equal(5, input.ProblemSolvingSelfAssessment);
        Assert.Equal(5, input.CollaborationSelfAssessment);
        Assert.Equal(5, input.LearningAgility);

        Assert.Equal(
            "RemoteHybrid",
            input.PreferredEnvironment);

        Assert.Equal("Fast", input.PreferredPace);
        Assert.Equal("Growth", input.StabilityPreference);

        Assert.Equal(
            "SalaryBenefits",
            input.CompensationPreference);

        Assert.Equal("Technology", input.IndustryPreference);
        Assert.Equal(string.Empty, input.CareerLabel);
    }

    [Fact]
    public void Build_UsesHighestMatchingSkillProficiency()
    {
        var profile = CreateProfile();

        profile.Skills.Add(
            new StudentSkill
            {
                SkillName = "Java Programming",
                Proficiency = SkillProficiency.Beginner
            });

        profile.Skills.Add(
            new StudentSkill
            {
                SkillName = "C#",
                Proficiency = SkillProficiency.Advanced
            });

        var assessment = CreateCompletedAssessment(profile.Id);

        var input = _builder.Build(profile, assessment);

        Assert.Equal(5, input.ProgrammingSkill);
    }

    [Fact]
    public void Build_PreservesUnrecognizedProgrammeWithoutForcingCategory()
    {
        var profile = CreateProfile();
        profile.Programme = "Economics and Finance";

        var assessment = CreateCompletedAssessment(profile.Id);

        var input = _builder.Build(profile, assessment);

        Assert.Equal(
            "EconomicsandFinance",
            input.AcademicBackground);
    }

    [Fact]
    public void Build_RejectsIncompleteAssessment()
    {
        var profile = CreateProfile();
        var assessment = CreateCompletedAssessment(profile.Id);

        assessment.Responses.RemoveAt(
            assessment.Responses.Count - 1);

        var exception = Assert.Throws<InvalidOperationException>(
            () => _builder.Build(profile, assessment));

        Assert.Contains(
            "all 15 questions",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Build_RejectsAssessmentForDifferentProfile()
    {
        var profile = CreateProfile();

        var assessment =
            CreateCompletedAssessment(Guid.NewGuid());

        var exception = Assert.Throws<InvalidOperationException>(
            () => _builder.Build(profile, assessment));

        Assert.Contains(
            "does not belong",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Build_RejectsInProgressAssessment()
    {
        var profile = CreateProfile();
        var assessment = CreateCompletedAssessment(profile.Id);

        assessment.Status = "InProgress";
        assessment.CompletedAt = null;

        var exception = Assert.Throws<InvalidOperationException>(
            () => _builder.Build(profile, assessment));

        Assert.Contains(
            "completed career assessment",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Build_RejectsInvalidAssessmentOption()
    {
        var profile = CreateProfile();
        var assessment = CreateCompletedAssessment(profile.Id);

        assessment.Responses[0].OptionId = Guid.NewGuid();

        var exception = Assert.Throws<InvalidOperationException>(
            () => _builder.Build(profile, assessment));

        Assert.Contains(
            "invalid option",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);
    }

    private static StudentProfile CreateProfile()
    {
        return new StudentProfile
        {
            Id = Guid.NewGuid(),
            Name = "Ama Mensah",
            Programme = "Computer Science",
            AcademicLevel = AcademicLevel.Undergraduate,
            Interests = ["Cloud Computing"],
            Skills =
            [
                new StudentSkill
                {
                    SkillName = "Python",
                    Proficiency = SkillProficiency.Expert
                },
                new StudentSkill
                {
                    SkillName = "Data Analysis",
                    Proficiency = SkillProficiency.Intermediate
                }
            ]
        };
    }

    private static AssessmentSession CreateCompletedAssessment(
        Guid studentProfileId)
    {
        var session = new AssessmentSession
        {
            Id = Guid.NewGuid(),
            StudentProfileId = studentProfileId,
            Status = "Completed",
            CompletedAt = DateTime.UtcNow
        };

        foreach (var question in
                 AssessmentQuestionBank.GetAllQuestions())
        {
            session.Responses.Add(
                new AssessmentResponse
                {
                    AssessmentSessionId = session.Id,
                    QuestionId = question.Id,
                    OptionId = question.Options[0].Id
                });
        }

        return session;
    }
}