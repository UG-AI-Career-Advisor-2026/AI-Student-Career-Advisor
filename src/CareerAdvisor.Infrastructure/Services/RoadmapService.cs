using CareerAdvisor.Core.Enums;
using CareerAdvisor.Core.Interfaces;
using CareerAdvisor.Core.Models;
using CareerAdvisor.Core.SkillGaps;
using CareerAdvisor.Core.Validators;

namespace CareerAdvisor.Infrastructure.Services;

/// <summary>
/// Generates and maintains deterministic persisted learning roadmaps.
/// </summary>
public sealed class RoadmapService : IRoadmapService
{
    private const int MaximumSkillGapSteps = 6;
    private const int MaximumTopicSteps = 4;
    private const int MaximumCertificationSteps = 2;

    private readonly IStudentProfileRepository _profileRepository;
    private readonly ICareerRepository _careerRepository;
    private readonly IRecommendationRepository _recommendationRepository;
    private readonly IRoadmapRepository _roadmapRepository;
    private readonly ISkillGapService _skillGapService;
    private readonly SkillGapResultValidator _skillGapValidator;
    private readonly LearningRoadmapValidator _validator;
    private readonly TimeProvider _timeProvider;

    public RoadmapService(
        IStudentProfileRepository profileRepository,
        ICareerRepository careerRepository,
        IRecommendationRepository recommendationRepository,
        IRoadmapRepository roadmapRepository,
        ISkillGapService skillGapService,
        SkillGapResultValidator skillGapValidator,
        LearningRoadmapValidator validator,
        TimeProvider timeProvider)
    {
        _profileRepository = profileRepository;
        _careerRepository = careerRepository;
        _recommendationRepository = recommendationRepository;
        _roadmapRepository = roadmapRepository;
        _skillGapService = skillGapService;
        _skillGapValidator = skillGapValidator;
        _validator = validator;
        _timeProvider = timeProvider;
    }

    /// <inheritdoc />
    public async Task<LearningRoadmap> GenerateRoadmapAsync(
        Guid studentProfileId,
        Guid careerProfileId)
    {
        await EnsureSavedProfileAsync(studentProfileId);

        if (careerProfileId == Guid.Empty)
        {
            throw new InvalidOperationException(
                "A valid supported career is required for roadmap generation.");
        }

        var career = await _careerRepository.GetByIdAsync(careerProfileId);

        if (career is null || career.Id != careerProfileId)
        {
            throw new InvalidOperationException(
                "The requested career could not be found in the supported " +
                "career catalogue.");
        }

        var sessions = await _recommendationRepository
            .GetByStudentProfileIdAsync(studentProfileId);

        EnsureCareerWasRecommended(
            sessions,
            studentProfileId,
            careerProfileId);

        var existing = await _roadmapRepository
            .GetByProfileAndCareerAsync(studentProfileId, careerProfileId);

        if (existing is not null)
        {
            EnsureValidRoadmap(existing, studentProfileId, careerProfileId);
            return existing;
        }

        var skillGap = await _skillGapService.AnalyzeAsync(
            studentProfileId,
            careerProfileId);

        ValidateGenerationSources(skillGap, career, studentProfileId);

        var roadmapId = Guid.NewGuid();
        var steps = BuildSteps(roadmapId, career, skillGap);

        if (steps.Count == 0)
        {
            throw new InvalidOperationException(
                "A learning roadmap could not be generated from the saved " +
                "skill gaps and approved career catalogue guidance.");
        }

        var roadmap = new LearningRoadmap
        {
            Id = roadmapId,
            StudentProfileId = studentProfileId,
            CareerProfileId = careerProfileId,
            CreatedAt = _timeProvider.GetUtcNow().UtcDateTime,
            Steps = steps
        };

        EnsureValidRoadmap(roadmap, studentProfileId, careerProfileId);
        var added = await _roadmapRepository.TryAddAsync(roadmap);

        var persisted = added
            ? await _roadmapRepository.GetByIdForProfileAsync(
                studentProfileId,
                roadmap.Id)
            : await _roadmapRepository.GetByProfileAndCareerAsync(
                studentProfileId,
                careerProfileId);

        if (persisted is null)
        {
            throw MalformedRoadmapData();
        }

        EnsureValidRoadmap(persisted, studentProfileId, careerProfileId);
        return persisted;
    }

    /// <inheritdoc />
    public async Task<LearningRoadmap> GetRoadmapAsync(
        Guid studentProfileId,
        Guid roadmapId)
    {
        await EnsureSavedProfileAsync(studentProfileId);

        if (roadmapId == Guid.Empty)
        {
            throw RoadmapNotFound();
        }

        var roadmap = await _roadmapRepository.GetByIdForProfileAsync(
            studentProfileId,
            roadmapId);

        if (roadmap is null)
        {
            throw RoadmapNotFound();
        }

        EnsureValidRoadmap(roadmap, studentProfileId);
        return roadmap;
    }

    /// <inheritdoc />
    public async Task<LearningRoadmap> UpdateRoadmapProgressAsync(
        Guid studentProfileId,
        Guid roadmapId,
        Guid stepId,
        bool isCompleted)
    {
        await EnsureSavedProfileAsync(studentProfileId);

        if (roadmapId == Guid.Empty)
        {
            throw RoadmapNotFound();
        }

        var roadmap = await _roadmapRepository.GetByIdForProfileAsync(
            studentProfileId,
            roadmapId);

        if (roadmap is null)
        {
            throw RoadmapNotFound();
        }

        EnsureValidRoadmap(roadmap, studentProfileId);

        if (stepId == Guid.Empty)
        {
            throw StepNotFound();
        }

        var step = roadmap.Steps.SingleOrDefault(candidate =>
            candidate.Id == stepId);

        if (step is null)
        {
            throw StepNotFound();
        }

        if (step.IsCompleted != isCompleted)
        {
            DateTime? completedAt = isCompleted
                ? _timeProvider.GetUtcNow().UtcDateTime
                : null;

            var updated = await _roadmapRepository.SetStepCompletionAsync(
                studentProfileId,
                roadmapId,
                stepId,
                isCompleted,
                completedAt);

            if (!updated)
            {
                throw StepNotFound();
            }
        }

        var persisted = await _roadmapRepository.GetByIdForProfileAsync(
            studentProfileId,
            roadmapId);

        if (persisted is null)
        {
            throw RoadmapNotFound();
        }

        EnsureValidRoadmap(persisted, studentProfileId);

        var persistedStep = persisted.Steps.SingleOrDefault(candidate =>
            candidate.Id == stepId);

        if (persistedStep is null || persistedStep.IsCompleted != isCompleted)
        {
            throw MalformedRoadmapData();
        }

        return persisted;
    }

    private async Task EnsureSavedProfileAsync(Guid studentProfileId)
    {
        if (studentProfileId == Guid.Empty)
        {
            throw new InvalidOperationException(
                "A valid saved student profile is required for a learning " +
                "roadmap.");
        }

        var profile = await _profileRepository.GetByIdAsync(studentProfileId);

        if (profile is null || profile.Id != studentProfileId)
        {
            throw new InvalidOperationException(
                "The requested student profile could not be found.");
        }
    }

    private static void EnsureCareerWasRecommended(
        IEnumerable<RecommendationSession>? sessions,
        Guid studentProfileId,
        Guid careerProfileId)
    {
        if (sessions is null)
        {
            throw MalformedRecommendationData();
        }

        var savedSessions = sessions.ToList();

        if (savedSessions.Any(session => session is null))
        {
            throw MalformedRecommendationData();
        }

        foreach (var session in savedSessions)
        {
            if (session.Id == Guid.Empty ||
                session.StudentProfileId != studentProfileId ||
                session.Recommendations is null ||
                session.Recommendations.Count != 3 ||
                session.Recommendations.Any(recommendation =>
                    recommendation is null ||
                    recommendation.Id == Guid.Empty ||
                    recommendation.RecommendationSessionId != session.Id ||
                    recommendation.CareerProfileId == Guid.Empty) ||
                session.Recommendations
                    .Select(recommendation => recommendation.CareerProfileId)
                    .Distinct()
                    .Count() != session.Recommendations.Count)
            {
                throw MalformedRecommendationData();
            }
        }

        if (!savedSessions.SelectMany(session => session.Recommendations)
                .Any(recommendation =>
                    recommendation.CareerProfileId == careerProfileId))
        {
            throw new InvalidOperationException(
                "The selected career is not available from this profile's " +
                "saved recommendations.");
        }
    }

    private void ValidateGenerationSources(
        SkillGapResult? skillGap,
        CareerProfile career,
        Guid studentProfileId)
    {
        if (skillGap is null ||
            skillGap.StudentProfileId != studentProfileId ||
            skillGap.CareerProfileId != career.Id ||
            string.IsNullOrWhiteSpace(career.Title) ||
            !_skillGapValidator.Validate(skillGap).IsValid)
        {
            throw MalformedGenerationData();
        }

        ValidateCatalogueCollection(career.RequiredSkills, minimumCount: 6);
        ValidateCatalogueCollection(career.SuggestedLearningTopics);
        ValidateCatalogueCollection(career.RecommendedCertifications);

        var requiredNames = career.RequiredSkills
            .Order(StringComparer.Ordinal)
            .ToList();

        var resultNames = skillGap.Items
            .Select(item => item.RequiredSkillName)
            .Order(StringComparer.Ordinal)
            .ToList();

        if (!requiredNames.SequenceEqual(resultNames, StringComparer.Ordinal))
        {
            throw MalformedGenerationData();
        }
    }

    private static void ValidateCatalogueCollection(
        List<string>? values,
        int minimumCount = 0)
    {
        if (values is null || values.Count < minimumCount || values.Any(value =>
                string.IsNullOrWhiteSpace(value) ||
                !string.Equals(value, value.Trim(), StringComparison.Ordinal)))
        {
            throw MalformedGenerationData();
        }

        var normalized = values
            .Select(SkillNameNormalizer.Normalize)
            .ToList();

        if (normalized.Distinct(StringComparer.Ordinal).Count() !=
            normalized.Count)
        {
            throw MalformedGenerationData();
        }
    }

    private static List<RoadmapStep> BuildSteps(
        Guid roadmapId,
        CareerProfile career,
        SkillGapResult skillGap)
    {
        var candidates = new List<StepCandidate>();
        var compositionKeys = new HashSet<string>(StringComparer.Ordinal);

        var missing = skillGap.Items
            .Where(item =>
                item.Classification == SkillGapClassification.Missing)
            .Select(item => new StepCandidate(
                item.RequiredSkillName,
                $"Build foundation: {item.RequiredSkillName}",
                "Develop foundational academic knowledge and practice in " +
                $"{item.RequiredSkillName} for {career.Title}."));

        var developing = skillGap.Items
            .Where(item =>
                item.Classification ==
                SkillGapClassification.NeedsDevelopment)
            .Select(item => new StepCandidate(
                item.RequiredSkillName,
                $"Develop further: {item.RequiredSkillName}",
                "Strengthen your current Beginner proficiency in " +
                $"{item.RequiredSkillName} toward the Intermediate academic " +
                "MVP baseline."));

        AddCandidates(
            candidates,
            compositionKeys,
            missing,
            MaximumSkillGapSteps);

        AddCandidates(
            candidates,
            compositionKeys,
            developing,
            MaximumSkillGapSteps - candidates.Count);

        var topics = career.SuggestedLearningTopics.Select(topic =>
            new StepCandidate(
                topic,
                $"Study topic: {topic}",
                $"Study and practise {topic} as a suggested learning topic " +
                $"for {career.Title}."));

        AddCandidates(
            candidates,
            compositionKeys,
            topics,
            MaximumTopicSteps);

        var certifications = career.RecommendedCertifications.Select(
            certification => new StepCandidate(
                certification,
                $"Explore certification: {certification}",
                "Review the published requirements and syllabus for " +
                $"{certification}. This is exploration guidance, not " +
                "enrolment or a guarantee of certification or employment."));

        AddCandidates(
            candidates,
            compositionKeys,
            certifications,
            MaximumCertificationSteps);

        return candidates
            .Take(LearningRoadmapValidator.MaximumStepCount)
            .Select((candidate, index) => new RoadmapStep
            {
                Id = Guid.NewGuid(),
                LearningRoadmapId = roadmapId,
                Order = index + 1,
                Title = candidate.Title,
                Description = candidate.Description,
                ResourceLink = string.Empty,
                IsCompleted = false,
                CompletedAt = null
            })
            .ToList();
    }

    private static void AddCandidates(
        List<StepCandidate> destination,
        HashSet<string> compositionKeys,
        IEnumerable<StepCandidate> source,
        int maximum)
    {
        if (maximum <= 0)
        {
            return;
        }

        var added = 0;

        foreach (var candidate in source
                     .OrderBy(
                         candidate => SkillNameNormalizer.Normalize(
                             candidate.SourceLabel),
                         StringComparer.Ordinal)
                     .ThenBy(
                         candidate => candidate.SourceLabel,
                         StringComparer.Ordinal))
        {
            var key = SkillNameNormalizer.Normalize(candidate.SourceLabel);

            if (!compositionKeys.Add(key))
            {
                continue;
            }

            destination.Add(candidate);
            added++;

            if (added == maximum)
            {
                break;
            }
        }
    }

    private void EnsureValidRoadmap(
        LearningRoadmap roadmap,
        Guid studentProfileId,
        Guid? careerProfileId = null)
    {
        if (roadmap.StudentProfileId != studentProfileId ||
            careerProfileId is not null &&
            roadmap.CareerProfileId != careerProfileId.Value ||
            !_validator.Validate(roadmap).IsValid)
        {
            throw MalformedRoadmapData();
        }
    }

    private static InvalidOperationException RoadmapNotFound()
    {
        return new InvalidOperationException(
            "The requested learning roadmap could not be found.");
    }

    private static InvalidOperationException StepNotFound()
    {
        return new InvalidOperationException(
            "The requested roadmap step could not be found.");
    }

    private static InvalidOperationException MalformedRecommendationData()
    {
        return new InvalidOperationException(
            "Roadmap generation could not be completed because the saved " +
            "recommendation data is invalid.");
    }

    private static InvalidOperationException MalformedGenerationData()
    {
        return new InvalidOperationException(
            "Roadmap generation could not be completed because the saved " +
            "skill-gap or career catalogue data is invalid.");
    }

    private static InvalidOperationException MalformedRoadmapData()
    {
        return new InvalidOperationException(
            "The saved learning roadmap is invalid and cannot be used.");
    }

    private sealed record StepCandidate(
        string SourceLabel,
        string Title,
        string Description);
}
