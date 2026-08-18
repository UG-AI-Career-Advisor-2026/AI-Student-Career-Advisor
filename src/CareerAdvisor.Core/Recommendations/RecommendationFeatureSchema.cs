using CareerAdvisor.Core.Enums;

namespace CareerAdvisor.Core.Recommendations;

/// <summary>
/// Defines the stable input contract used by CareerIQ recommendation training data.
/// Question and option codes are used instead of editable display text.
/// </summary>
public static class RecommendationFeatureSchema
{
    public const int MinimumNumericValue = 1;
    public const int MaximumNumericValue = 5;
    public const int MinimumRecordsPerCareer = 10;

    public const int NoProfileEvidenceValue = 1;
public const int InterestOnlyValue = 3;

    public static IReadOnlyList<string> RequiredColumns { get; } =
    [
        "AcademicBackground",
        "AcademicLevel",
        "ProgrammingSkill",
        "DataSkill",
        "CybersecuritySkill",
        "CloudSkill",
        "NetworkingSkill",
        "DatabaseSkill",
        "DesignSkill",
        "AISkill",
        "TechnologyInterest",
        "DataInterest",
        "DesignInterest",
        "LeadershipInterest",
        "SocialImpactInterest",
        "ProgrammingSelfAssessment",
        "CommunicationSelfAssessment",
        "ProblemSolvingSelfAssessment",
        "CollaborationSelfAssessment",
        "LearningAgility",
        "PreferredEnvironment",
        "PreferredPace",
        "StabilityPreference",
        "CompensationPreference",
        "IndustryPreference",
        "CareerLabel"
    ];

    public static IReadOnlyList<string> NumericColumns { get; } =
    [
        "ProgrammingSkill",
        "DataSkill",
        "CybersecuritySkill",
        "CloudSkill",
        "NetworkingSkill",
        "DatabaseSkill",
        "DesignSkill",
        "AISkill",
        "TechnologyInterest",
        "DataInterest",
        "DesignInterest",
        "LeadershipInterest",
        "SocialImpactInterest",
        "ProgrammingSelfAssessment",
        "CommunicationSelfAssessment",
        "ProblemSolvingSelfAssessment",
        "CollaborationSelfAssessment",
        "LearningAgility"
    ];

    public static IReadOnlyDictionary<string, IReadOnlyList<string>>
    ProfileDomainKeywordsByColumn { get; } =
    new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal)
    {
        ["ProgrammingSkill"] =
        [
            "programming",
            "software development",
            "coding",
            "c#",
            "java",
            "python",
            "javascript"
        ],
        ["DataSkill"] =
        [
            "data analysis",
            "data analytics",
            "data science",
            "statistics",
            "excel",
            "power bi"
        ],
        ["CybersecuritySkill"] =
        [
            "cybersecurity",
            "cyber security",
            "information security",
            "network security"
        ],
        ["CloudSkill"] =
        [
            "cloud",
            "cloud computing",
            "aws",
            "azure",
            "gcp"
        ],
        ["NetworkingSkill"] =
        [
            "networking",
            "network administration",
            "tcp/ip",
            "routing"
        ],
        ["DatabaseSkill"] =
        [
            "database",
            "database administration",
            "sql",
            "mysql",
            "postgresql",
            "oracle"
        ],
        ["DesignSkill"] =
        [
            "ui design",
            "ux design",
            "user interface",
            "user experience",
            "graphic design",
            "figma"
        ],
        ["AISkill"] =
        [
            "artificial intelligence",
            "machine learning",
            "deep learning",
            "ai",
            "ml"
        ]
    };
    public static IReadOnlyDictionary<string, string> QuestionColumnsByCode { get; } =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["Q1_INT_TECH"] = "TechnologyInterest",
            ["Q2_INT_DATA"] = "DataInterest",
            ["Q3_INT_CREA"] = "DesignInterest",
            ["Q4_INT_MGMT"] = "LeadershipInterest",
            ["Q5_INT_SOCIAL"] = "SocialImpactInterest",
            ["Q6_SKILL_PROG"] = "ProgrammingSelfAssessment",
            ["Q7_SKILL_COMM"] = "CommunicationSelfAssessment",
            ["Q8_SKILL_PROB"] = "ProblemSolvingSelfAssessment",
            ["Q9_SKILL_COLL"] = "CollaborationSelfAssessment",
            ["Q10_SKILL_LEARN"] = "LearningAgility",
            ["Q11_WORK_ENV"] = "PreferredEnvironment",
            ["Q12_WORK_PACE"] = "PreferredPace",
            ["Q13_WORK_STABIL"] = "StabilityPreference",
            ["Q14_WORK_COMP"] = "CompensationPreference",
            ["Q15_WORK_INDUS"] = "IndustryPreference"
        };

    public static IReadOnlyDictionary<string, int> NumericOptionValues { get; } =
        new Dictionary<string, int>(StringComparer.Ordinal)
        {
            ["Q1_OPT_A"] = 5,
            ["Q1_OPT_B"] = 4,
            ["Q1_OPT_C"] = 3,
            ["Q1_OPT_D"] = 1,

            ["Q2_OPT_A"] = 5,
            ["Q2_OPT_B"] = 4,
            ["Q2_OPT_C"] = 3,
            ["Q2_OPT_D"] = 1,

            ["Q3_OPT_A"] = 5,
            ["Q3_OPT_B"] = 4,
            ["Q3_OPT_C"] = 3,
            ["Q3_OPT_D"] = 1,

            ["Q4_OPT_A"] = 5,
            ["Q4_OPT_B"] = 4,
            ["Q4_OPT_C"] = 3,
            ["Q4_OPT_D"] = 1,

            ["Q5_OPT_A"] = 5,
            ["Q5_OPT_B"] = 4,
            ["Q5_OPT_C"] = 3,
            ["Q5_OPT_D"] = 1,

            ["Q6_OPT_A"] = 5,
            ["Q6_OPT_B"] = 4,
            ["Q6_OPT_C"] = 2,
            ["Q6_OPT_D"] = 1,

            ["Q7_OPT_A"] = 5,
            ["Q7_OPT_B"] = 4,
            ["Q7_OPT_C"] = 2,
            ["Q7_OPT_D"] = 1,

            ["Q8_OPT_A"] = 5,
            ["Q8_OPT_B"] = 4,
            ["Q8_OPT_C"] = 2,
            ["Q8_OPT_D"] = 1,

            ["Q9_OPT_A"] = 5,
            ["Q9_OPT_B"] = 4,
            ["Q9_OPT_C"] = 2,
            ["Q9_OPT_D"] = 1,

            ["Q10_OPT_A"] = 5,
            ["Q10_OPT_B"] = 4,
            ["Q10_OPT_C"] = 3,
            ["Q10_OPT_D"] = 2
        };

    public static IReadOnlyDictionary<string, string> CategoricalOptionValues { get; } =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["Q11_OPT_A"] = "RemoteHybrid",
            ["Q11_OPT_B"] = "OfficeBased",
            ["Q11_OPT_C"] = "Flexible",
            ["Q11_OPT_D"] = "NoPreference",

            ["Q12_OPT_A"] = "Fast",
            ["Q12_OPT_B"] = "Moderate",
            ["Q12_OPT_C"] = "Flexible",
            ["Q12_OPT_D"] = "Slow",

            ["Q13_OPT_A"] = "Growth",
            ["Q13_OPT_B"] = "Stability",
            ["Q13_OPT_C"] = "Balanced",
            ["Q13_OPT_D"] = "Situational",

            ["Q14_OPT_A"] = "SalaryBenefits",
            ["Q14_OPT_B"] = "SalaryEquity",
            ["Q14_OPT_C"] = "FreelanceContract",
            ["Q14_OPT_D"] = "NoPreference",

            ["Q15_OPT_A"] = "Technology",
            ["Q15_OPT_B"] = "Finance",
            ["Q15_OPT_C"] = "Healthcare",
            ["Q15_OPT_D"] = "Other"
        };

    public static IReadOnlyDictionary<string, IReadOnlyList<string>>
        AllowedCategoricalValuesByColumn { get; } =
        new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal)
        {
            ["AcademicLevel"] = Enum.GetNames<AcademicLevel>(),
            ["PreferredEnvironment"] =
                ["RemoteHybrid", "OfficeBased", "Flexible", "NoPreference"],
            ["PreferredPace"] =
                ["Fast", "Moderate", "Flexible", "Slow"],
            ["StabilityPreference"] =
                ["Growth", "Stability", "Balanced", "Situational"],
            ["CompensationPreference"] =
                ["SalaryBenefits", "SalaryEquity", "FreelanceContract", "NoPreference"],
            ["IndustryPreference"] =
                ["Technology", "Finance", "Healthcare", "Other"]
        };

    public static IReadOnlyDictionary<SkillProficiency, int>
        SkillProficiencyValues { get; } =
        new Dictionary<SkillProficiency, int>
        {
            [SkillProficiency.Beginner] = 2,
            [SkillProficiency.Intermediate] = 3,
            [SkillProficiency.Advanced] = 4,
            [SkillProficiency.Expert] = 5
        };

    public static IReadOnlyDictionary<string, string> CareerLabelsByCode { get; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["SD-001"] = "Software Developer",
            ["DA-002"] = "Data Analyst",
            ["CS-003"] = "Cybersecurity Analyst",
            ["CE-004"] = "Cloud Engineer",
            ["NA-005"] = "Network Administrator",
            ["DBA-006"] = "Database Administrator",
            ["UX-007"] = "UI/UX Designer",
            ["AI-008"] = "AI/ML Engineer"
        };
}