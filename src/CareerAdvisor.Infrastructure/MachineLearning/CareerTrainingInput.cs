using Microsoft.ML.Data;

namespace CareerAdvisor.Infrastructure.MachineLearning;

/// <summary>
/// Represents one row from the approved synthetic career-training dataset.
/// Column positions must remain aligned with the CSV schema.
/// </summary>
public sealed class CareerTrainingInput
{
    [LoadColumn(0)]
    public string AcademicBackground { get; set; } = string.Empty;

    [LoadColumn(1)]
    public string AcademicLevel { get; set; } = string.Empty;

    [LoadColumn(2)]
    public float ProgrammingSkill { get; set; }

    [LoadColumn(3)]
    public float DataSkill { get; set; }

    [LoadColumn(4)]
    public float CybersecuritySkill { get; set; }

    [LoadColumn(5)]
    public float CloudSkill { get; set; }

    [LoadColumn(6)]
    public float NetworkingSkill { get; set; }

    [LoadColumn(7)]
    public float DatabaseSkill { get; set; }

    [LoadColumn(8)]
    public float DesignSkill { get; set; }

    [LoadColumn(9)]
    public float AISkill { get; set; }

    [LoadColumn(10)]
    public float TechnologyInterest { get; set; }

    [LoadColumn(11)]
    public float DataInterest { get; set; }

    [LoadColumn(12)]
    public float DesignInterest { get; set; }

    [LoadColumn(13)]
    public float LeadershipInterest { get; set; }

    [LoadColumn(14)]
    public float SocialImpactInterest { get; set; }

    [LoadColumn(15)]
    public float ProgrammingSelfAssessment { get; set; }

    [LoadColumn(16)]
    public float CommunicationSelfAssessment { get; set; }

    [LoadColumn(17)]
    public float ProblemSolvingSelfAssessment { get; set; }

    [LoadColumn(18)]
    public float CollaborationSelfAssessment { get; set; }

    [LoadColumn(19)]
    public float LearningAgility { get; set; }

    [LoadColumn(20)]
    public string PreferredEnvironment { get; set; } = string.Empty;

    [LoadColumn(21)]
    public string PreferredPace { get; set; } = string.Empty;

    [LoadColumn(22)]
    public string StabilityPreference { get; set; } = string.Empty;

    [LoadColumn(23)]
    public string CompensationPreference { get; set; } = string.Empty;

    [LoadColumn(24)]
    public string IndustryPreference { get; set; } = string.Empty;

    [LoadColumn(25)]
    public string CareerLabel { get; set; } = string.Empty;
}