using CareerAdvisor.Core.Models;
using CareerAdvisor.Core.Recommendations;

namespace CareerAdvisor.Infrastructure.MachineLearning;

/// <summary>
/// Converts a saved student profile and completed assessment into
/// the exact feature input expected by the trained recommendation model.
/// </summary>
public sealed class RecommendationInputBuilder
{
    public CareerTrainingInput Build(
        StudentProfile profile,
        AssessmentSession assessment)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(assessment);

        ValidateProfile(profile);
        ValidateAssessment(profile, assessment);

        var input = new CareerTrainingInput
        {
            AcademicBackground =
                NormalizeAcademicBackground(profile.Programme),

            AcademicLevel = profile.AcademicLevel.ToString(),

            ProgrammingSkill = GetProfileEvidence(
                profile,
                "ProgrammingSkill"),

            DataSkill = GetProfileEvidence(
                profile,
                "DataSkill"),

            CybersecuritySkill = GetProfileEvidence(
                profile,
                "CybersecuritySkill"),

            CloudSkill = GetProfileEvidence(
                profile,
                "CloudSkill"),

            NetworkingSkill = GetProfileEvidence(
                profile,
                "NetworkingSkill"),

            DatabaseSkill = GetProfileEvidence(
                profile,
                "DatabaseSkill"),

            DesignSkill = GetProfileEvidence(
                profile,
                "DesignSkill"),

            AISkill = GetProfileEvidence(
                profile,
                "AISkill")
        };

        ApplyAssessmentResponses(input, assessment);

        return input;
    }

    private static void ValidateProfile(StudentProfile profile)
    {
        if (profile.Id == Guid.Empty)
        {
            throw new InvalidOperationException(
                "A valid saved student profile is required.");
        }

        if (string.IsNullOrWhiteSpace(profile.Programme))
        {
            throw new InvalidOperationException(
                "The student profile must include a programme.");
        }

        if (!Enum.IsDefined(profile.AcademicLevel))
        {
            throw new InvalidOperationException(
                "The student profile contains an invalid academic level.");
        }
    }

    private static void ValidateAssessment(
        StudentProfile profile,
        AssessmentSession assessment)
    {
        if (assessment.StudentProfileId != profile.Id)
        {
            throw new InvalidOperationException(
                "The completed assessment does not belong to " +
                "the requested student profile.");
        }

        if (!string.Equals(
                assessment.Status,
                "Completed",
                StringComparison.Ordinal) ||
            assessment.CompletedAt is null)
        {
            throw new InvalidOperationException(
                "A completed career assessment is required before " +
                "recommendations can be generated.");
        }

        var questions = AssessmentQuestionBank.GetAllQuestions();

        if (assessment.Responses.Count != questions.Count)
        {
            throw new InvalidOperationException(
                "The completed assessment must contain answers to " +
                "all 15 questions.");
        }

        if (assessment.Responses
                .GroupBy(response => response.QuestionId)
                .Any(group => group.Count() != 1))
        {
            throw new InvalidOperationException(
                "The assessment contains duplicate question responses.");
        }
    }

    private static void ApplyAssessmentResponses(
        CareerTrainingInput input,
        AssessmentSession assessment)
    {
        var questions = AssessmentQuestionBank.GetAllQuestions();

        var responsesByQuestion = assessment.Responses
            .ToDictionary(
                response => response.QuestionId,
                response => response);

        foreach (var question in questions)
        {
            if (!responsesByQuestion.TryGetValue(
                    question.Id,
                    out var response))
            {
                throw new InvalidOperationException(
                    $"Assessment response for '{question.Code}' " +
                    "was not found.");
            }

            var option = question.Options.SingleOrDefault(
                item => item.Id == response.OptionId);

            if (option is null)
            {
                throw new InvalidOperationException(
                    $"Assessment response for '{question.Code}' " +
                    "contains an invalid option.");
            }

            if (!RecommendationFeatureSchema
                    .QuestionColumnsByCode
                    .TryGetValue(
                        question.Code,
                        out var featureColumn))
            {
                throw new InvalidOperationException(
                    $"Question '{question.Code}' is not mapped to " +
                    "the recommendation feature schema.");
            }

            if (RecommendationFeatureSchema.NumericOptionValues
                .TryGetValue(option.Code, out var numericValue))
            {
                SetNumericFeature(
                    input,
                    featureColumn,
                    numericValue);

                continue;
            }

            if (RecommendationFeatureSchema.CategoricalOptionValues
                .TryGetValue(option.Code, out var categoricalValue))
            {
                SetCategoricalFeature(
                    input,
                    featureColumn,
                    categoricalValue);

                continue;
            }

            throw new InvalidOperationException(
                $"Option '{option.Code}' is not mapped to " +
                "the recommendation feature schema.");
        }
    }

    private static float GetProfileEvidence(
        StudentProfile profile,
        string featureColumn)
    {
        var keywords =
            RecommendationFeatureSchema
                .ProfileDomainKeywordsByColumn[featureColumn];

        var matchingSkillValues = profile.Skills
            .Where(skill =>
                MatchesAnyKeyword(skill.SkillName, keywords))
            .Select(skill =>
                RecommendationFeatureSchema
                    .SkillProficiencyValues[skill.Proficiency])
            .ToList();

        if (matchingSkillValues.Count > 0)
        {
            return matchingSkillValues.Max();
        }

        var hasMatchingInterest = profile.Interests.Any(
            interest => MatchesAnyKeyword(interest, keywords));

        return hasMatchingInterest
            ? RecommendationFeatureSchema.InterestOnlyValue
            : RecommendationFeatureSchema.NoProfileEvidenceValue;
    }

    private static bool MatchesAnyKeyword(
        string value,
        IEnumerable<string> keywords)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return keywords.Any(keyword =>
            ContainsKeyword(value, keyword));
    }

    private static bool ContainsKeyword(
        string value,
        string keyword)
    {
        var normalizedValue = value.Trim().ToLowerInvariant();
        var normalizedKeyword = keyword.Trim().ToLowerInvariant();

        if (normalizedKeyword.Any(character =>
                !char.IsLetterOrDigit(character)))
        {
            return normalizedValue.Contains(
                normalizedKeyword,
                StringComparison.Ordinal);
        }

        if (normalizedKeyword.Length > 2)
        {
            return normalizedValue.Contains(
                normalizedKeyword,
                StringComparison.Ordinal);
        }

        var tokens = normalizedValue
            .Split(
                value
                    .Where(character =>
                        !char.IsLetterOrDigit(character))
                    .Distinct()
                    .ToArray(),
                StringSplitOptions.RemoveEmptyEntries);

        return tokens.Contains(
            normalizedKeyword,
            StringComparer.Ordinal);
    }

    private static string NormalizeAcademicBackground(
        string programme)
    {
        var normalized = new string(
            programme
                .Where(char.IsLetterOrDigit)
                .Select(char.ToLowerInvariant)
                .ToArray());

        if (normalized.Contains("cybersecurity"))
        {
            return "Cybersecurity";
        }

        if (normalized.Contains("computernetwork") ||
            normalized.Contains("networkadministration"))
        {
            return "ComputerNetworking";
        }

        if (normalized.Contains("computerengineering"))
        {
            return "ComputerEngineering";
        }

        if (normalized.Contains("computerscience"))
        {
            return "ComputerScience";
        }

        if (normalized.Contains("informationsystems"))
        {
            return "InformationSystems";
        }

        if (normalized.Contains("informationtechnology"))
        {
            return "InformationTechnology";
        }

        if (normalized.Contains("graphicdesign") ||
            normalized.Contains("uidesign") ||
            normalized.Contains("uxdesign"))
        {
            return "GraphicDesign";
        }

        if (normalized.Contains("statistics"))
        {
            return "Statistics";
        }

        if (normalized.Contains("mathematics"))
        {
            return "Mathematics";
        }

        return new string(
            programme
                .Where(char.IsLetterOrDigit)
                .ToArray());
    }

    private static void SetNumericFeature(
        CareerTrainingInput input,
        string featureColumn,
        float value)
    {
        switch (featureColumn)
        {
            case "TechnologyInterest":
                input.TechnologyInterest = value;
                break;

            case "DataInterest":
                input.DataInterest = value;
                break;

            case "DesignInterest":
                input.DesignInterest = value;
                break;

            case "LeadershipInterest":
                input.LeadershipInterest = value;
                break;

            case "SocialImpactInterest":
                input.SocialImpactInterest = value;
                break;

            case "ProgrammingSelfAssessment":
                input.ProgrammingSelfAssessment = value;
                break;

            case "CommunicationSelfAssessment":
                input.CommunicationSelfAssessment = value;
                break;

            case "ProblemSolvingSelfAssessment":
                input.ProblemSolvingSelfAssessment = value;
                break;

            case "CollaborationSelfAssessment":
                input.CollaborationSelfAssessment = value;
                break;

            case "LearningAgility":
                input.LearningAgility = value;
                break;

            default:
                throw new InvalidOperationException(
                    $"Numeric feature '{featureColumn}' is not supported.");
        }
    }

    private static void SetCategoricalFeature(
        CareerTrainingInput input,
        string featureColumn,
        string value)
    {
        switch (featureColumn)
        {
            case "PreferredEnvironment":
                input.PreferredEnvironment = value;
                break;

            case "PreferredPace":
                input.PreferredPace = value;
                break;

            case "StabilityPreference":
                input.StabilityPreference = value;
                break;

            case "CompensationPreference":
                input.CompensationPreference = value;
                break;

            case "IndustryPreference":
                input.IndustryPreference = value;
                break;

            default:
                throw new InvalidOperationException(
                    $"Categorical feature '{featureColumn}' " +
                    "is not supported.");
        }
    }
}