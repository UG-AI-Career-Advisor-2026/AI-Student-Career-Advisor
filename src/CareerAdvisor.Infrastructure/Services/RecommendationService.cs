using CareerAdvisor.Core.Interfaces;
using CareerAdvisor.Core.Models;
using CareerAdvisor.Core.Recommendations;
using CareerAdvisor.Infrastructure.MachineLearning;

namespace CareerAdvisor.Infrastructure.Services;

/// <summary>
/// Generates, explains and persists model-backed career recommendations.
/// </summary>
public sealed class RecommendationService : IRecommendationService
{
    private readonly IStudentProfileRepository _profileRepository;
    private readonly IAssessmentService _assessmentService;
    private readonly ICareerRepository _careerRepository;
    private readonly IRecommendationRepository
        _recommendationRepository;

    private readonly RecommendationInputBuilder _inputBuilder;
    private readonly ICareerModelPredictor _modelPredictor;

    public RecommendationService(
        IStudentProfileRepository profileRepository,
        IAssessmentService assessmentService,
        ICareerRepository careerRepository,
        IRecommendationRepository recommendationRepository,
        RecommendationInputBuilder inputBuilder,
        ICareerModelPredictor modelPredictor)
    {
        _profileRepository = profileRepository;
        _assessmentService = assessmentService;
        _careerRepository = careerRepository;
        _recommendationRepository = recommendationRepository;
        _inputBuilder = inputBuilder;
        _modelPredictor = modelPredictor;
    }

    public async Task<RecommendationSession>
        GenerateRecommendationsAsync(Guid studentProfileId)
    {
        if (studentProfileId == Guid.Empty)
        {
            throw new InvalidOperationException(
                "A valid saved student profile is required before " +
                "recommendations can be generated.");
        }

        var profile = await _profileRepository.GetByIdAsync(
            studentProfileId);

        if (profile is null)
        {
            throw new InvalidOperationException(
                "The requested student profile could not be found.");
        }

        var assessment =
            await _assessmentService
                .GetLatestCompletedAssessmentAsync(
                    studentProfileId);

        if (assessment is null)
        {
            throw new InvalidOperationException(
                "A completed career assessment is required before " +
                "recommendations can be generated.");
        }

        var input = _inputBuilder.Build(
            profile,
            assessment);

        var rawScores = _modelPredictor.Predict(input);

        var rankedScores = ValidateAndNormalizeScores(rawScores)
            .OrderByDescending(score => score.MatchScore)
            .ThenBy(score =>
                score.CareerLabel,
                StringComparer.Ordinal)
            .Take(3)
            .ToList();

        var recommendations =
            new List<CareerRecommendation>();

        foreach (var rankedScore in rankedScores)
        {
            var careerCode = GetCareerCode(
                rankedScore.CareerLabel);

            var career =
                await _careerRepository.GetByCodeAsync(
                    careerCode);

            if (career is null)
            {
                throw new InvalidOperationException(
                    $"Career catalogue entry '{careerCode}' " +
                    "could not be found.");
            }

            recommendations.Add(
                new CareerRecommendation
                {
                    CareerProfileId = career.Id,
                    Career = career,
                    MatchScore = rankedScore.MatchScore,
                    Reasoning = CreateExplanation(
                        career,
                        input)
                });
        }

        ValidateTopRecommendations(recommendations);

        var session = new RecommendationSession
        {
            StudentProfileId = profile.Id,
            GeneratedAt = DateTime.UtcNow,
            Recommendations = recommendations
        };

        await _recommendationRepository.AddAsync(session);

        var savedSession =
            await _recommendationRepository.GetByIdAsync(
                session.Id);

        if (savedSession is null)
        {
            throw new InvalidOperationException(
                "The generated recommendation session could not " +
                "be reopened after it was saved.");
        }

        savedSession.Recommendations = savedSession
            .Recommendations
            .OrderByDescending(recommendation =>
                recommendation.MatchScore)
            .ToList();

        return savedSession;
    }

    private static IReadOnlyList<NormalizedCareerScore>
        ValidateAndNormalizeScores(
            IReadOnlyList<CareerModelScore> scores)
    {
        ArgumentNullException.ThrowIfNull(scores);

        var expectedLabels = RecommendationFeatureSchema
            .CareerLabelsByCode
            .Values
            .ToHashSet(StringComparer.Ordinal);

        var actualLabels = scores
            .Select(score => score.CareerLabel)
            .ToHashSet(StringComparer.Ordinal);

        if (scores.Count != expectedLabels.Count ||
            actualLabels.Count != scores.Count ||
            !expectedLabels.SetEquals(actualLabels))
        {
            throw new InvalidOperationException(
                "The model prediction must contain exactly one " +
                "score for each of the eight supported careers.");
        }

        if (scores.Any(score =>
                !float.IsFinite(score.Score) ||
                score.Score < 0))
        {
            throw new InvalidOperationException(
                "The model prediction contains an invalid " +
                "career score.");
        }

        var totalScore = scores.Sum(score =>
            (double)score.Score);

        if (!double.IsFinite(totalScore) ||
            totalScore <= 0)
        {
            throw new InvalidOperationException(
                "The model prediction cannot be converted into " +
                "percentage-style match values.");
        }

        return scores
            .Select(score =>
                new NormalizedCareerScore(
                    score.CareerLabel,
                    Math.Round(
                        score.Score / totalScore,
                        4)))
            .ToList();
    }

    private static string GetCareerCode(
        string careerLabel)
    {
        var match = RecommendationFeatureSchema
            .CareerLabelsByCode
            .SingleOrDefault(pair =>
                string.Equals(
                    pair.Value,
                    careerLabel,
                    StringComparison.Ordinal));

        if (string.IsNullOrWhiteSpace(match.Key))
        {
            throw new InvalidOperationException(
                $"Model label '{careerLabel}' does not map to " +
                "a supported career.");
        }

        return match.Key;
    }

    private static void ValidateTopRecommendations(
        IReadOnlyCollection<CareerRecommendation>
            recommendations)
    {
        if (recommendations.Count != 3)
        {
            throw new InvalidOperationException(
                "The recommendation engine must return exactly " +
                "three careers.");
        }

        if (recommendations
                .Select(recommendation =>
                    recommendation.CareerProfileId)
                .Distinct()
                .Count() != 3)
        {
            throw new InvalidOperationException(
                "The recommendation engine returned duplicate careers.");
        }

        if (recommendations.Any(recommendation =>
                !double.IsFinite(recommendation.MatchScore) ||
                recommendation.MatchScore < 0 ||
                recommendation.MatchScore > 1))
        {
            throw new InvalidOperationException(
                "The recommendation engine produced an invalid " +
                "percentage-style match value.");
        }
    }

    private static string CreateExplanation(
        CareerProfile career,
        CareerTrainingInput input)
    {
        var evidence = GetCareerEvidence(
                career.Code,
                input)
            .OrderByDescending(item => item.Value)
            .ThenBy(item => item.Description)
            .Take(3)
            .Select(item =>
                $"{item.Description} ({item.Value:0}/5)")
            .ToList();

        var evidenceText = string.Join(
            ", ",
            evidence);

        return
            $"The model identified alignment with {career.Title} " +
            $"from your {evidenceText}. " +
            RecommendationDisclaimer.Text;
    }

    private static IReadOnlyList<FeatureEvidence>
        GetCareerEvidence(
            string careerCode,
            CareerTrainingInput input)
    {
        return careerCode switch
        {
            "SD-001" =>
            [
                Evidence(
                    "technology interest",
                    input.TechnologyInterest),
                Evidence(
                    "programming self-assessment",
                    input.ProgrammingSelfAssessment),
                Evidence(
                    "saved programming skills or interests",
                    input.ProgrammingSkill),
                Evidence(
                    "problem-solving self-assessment",
                    input.ProblemSolvingSelfAssessment)
            ],

            "DA-002" =>
            [
                Evidence(
                    "data interest",
                    input.DataInterest),
                Evidence(
                    "saved data skills or interests",
                    input.DataSkill),
                Evidence(
                    "problem-solving self-assessment",
                    input.ProblemSolvingSelfAssessment),
                Evidence(
                    "communication self-assessment",
                    input.CommunicationSelfAssessment)
            ],

            "CS-003" =>
            [
                Evidence(
                    "saved cybersecurity skills or interests",
                    input.CybersecuritySkill),
                Evidence(
                    "technology interest",
                    input.TechnologyInterest),
                Evidence(
                    "problem-solving self-assessment",
                    input.ProblemSolvingSelfAssessment),
                Evidence(
                    "learning agility",
                    input.LearningAgility)
            ],

            "CE-004" =>
            [
                Evidence(
                    "saved cloud skills or interests",
                    input.CloudSkill),
                Evidence(
                    "technology interest",
                    input.TechnologyInterest),
                Evidence(
                    "learning agility",
                    input.LearningAgility),
                Evidence(
                    "problem-solving self-assessment",
                    input.ProblemSolvingSelfAssessment)
            ],

            "NA-005" =>
            [
                Evidence(
                    "saved networking skills or interests",
                    input.NetworkingSkill),
                Evidence(
                    "problem-solving self-assessment",
                    input.ProblemSolvingSelfAssessment),
                Evidence(
                    "learning agility",
                    input.LearningAgility),
                Evidence(
                    "collaboration self-assessment",
                    input.CollaborationSelfAssessment)
            ],

            "DBA-006" =>
            [
                Evidence(
                    "saved database skills or interests",
                    input.DatabaseSkill),
                Evidence(
                    "data interest",
                    input.DataInterest),
                Evidence(
                    "problem-solving self-assessment",
                    input.ProblemSolvingSelfAssessment),
                Evidence(
                    "learning agility",
                    input.LearningAgility)
            ],

            "UX-007" =>
            [
                Evidence(
                    "design interest",
                    input.DesignInterest),
                Evidence(
                    "saved design skills or interests",
                    input.DesignSkill),
                Evidence(
                    "communication self-assessment",
                    input.CommunicationSelfAssessment),
                Evidence(
                    "collaboration self-assessment",
                    input.CollaborationSelfAssessment)
            ],

            "AI-008" =>
            [
                Evidence(
                    "saved AI skills or interests",
                    input.AISkill),
                Evidence(
                    "data interest",
                    input.DataInterest),
                Evidence(
                    "saved programming skills or interests",
                    input.ProgrammingSkill),
                Evidence(
                    "learning agility",
                    input.LearningAgility)
            ],

            _ => throw new InvalidOperationException(
                $"Career code '{careerCode}' is not supported by " +
                "the explanation engine.")
        };
    }

    private static FeatureEvidence Evidence(
        string description,
        float value)
    {
        return new FeatureEvidence(
            description,
            value);
    }

    private sealed record NormalizedCareerScore(
        string CareerLabel,
        double MatchScore);

    private sealed record FeatureEvidence(
        string Description,
        float Value);
}