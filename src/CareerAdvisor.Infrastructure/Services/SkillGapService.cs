using CareerAdvisor.Core.Enums;
using CareerAdvisor.Core.Interfaces;
using CareerAdvisor.Core.Models;
using CareerAdvisor.Core.SkillGaps;
using CareerAdvisor.Core.Validators;

namespace CareerAdvisor.Infrastructure.Services;

/// <summary>
/// Compares persisted profile skills with one supported career using the
/// deterministic Core skill-gap rules.
/// </summary>
public sealed class SkillGapService : ISkillGapService
{
    private readonly IStudentProfileRepository _profileRepository;
    private readonly ICareerRepository _careerRepository;
    private readonly SkillGapResultValidator _validator;

    public SkillGapService(
        IStudentProfileRepository profileRepository,
        ICareerRepository careerRepository,
        SkillGapResultValidator validator)
    {
        _profileRepository = profileRepository;
        _careerRepository = careerRepository;
        _validator = validator;
    }

    /// <inheritdoc />
    public async Task<SkillGapResult> AnalyzeAsync(
        Guid studentProfileId,
        Guid careerProfileId)
    {
        if (studentProfileId == Guid.Empty)
        {
            throw new InvalidOperationException(
                "A valid saved student profile is required for skill-gap " +
                "analysis.");
        }

        var profile = await _profileRepository.GetByIdAsync(studentProfileId);

        if (profile is null || profile.Id != studentProfileId)
        {
            throw new InvalidOperationException(
                "The requested student profile could not be found.");
        }

        if (careerProfileId == Guid.Empty)
        {
            throw new InvalidOperationException(
                "A valid supported career is required for skill-gap analysis.");
        }

        var career = await _careerRepository.GetByIdAsync(careerProfileId);

        if (career is null || career.Id != careerProfileId)
        {
            throw new InvalidOperationException(
                "The requested career could not be found in the supported " +
                "career catalogue.");
        }

        ValidateProfileSkills(profile.Skills);
        ValidateRequiredSkills(career.RequiredSkills);

        var items = new List<SkillGapItem>(career.RequiredSkills.Count);

        foreach (var requiredSkill in career.RequiredSkills)
        {
            var matchedSkill = SkillNameMatcher.FindBestMatch(
                requiredSkill,
                profile.Skills);

            var proficiency = matchedSkill?.Proficiency;

            items.Add(new SkillGapItem
            {
                RequiredSkillName = requiredSkill,
                Classification = SkillGapRules.Classify(proficiency),
                MatchedStudentSkillName = matchedSkill?.SkillName,
                CurrentProficiency = proficiency
            });
        }

        var result = new SkillGapResult
        {
            StudentProfileId = profile.Id,
            CareerProfileId = career.Id,
            CareerCode = career.Code,
            CareerTitle = career.Title,
            BaselineProficiency = SkillGapRules.BaselineProficiency,
            Items = items
                .OrderBy(item => GetClassificationRank(item.Classification))
                .ThenBy(
                    item => item.RequiredSkillName,
                    StringComparer.Ordinal)
                .ToList()
        };

        var validation = _validator.Validate(result);

        if (!validation.IsValid)
        {
            throw MalformedAnalysisData();
        }

        return result;
    }

    private static void ValidateProfileSkills(List<StudentSkill>? skills)
    {
        if (skills is null || skills.Any(skill => skill is null))
        {
            throw MalformedAnalysisData();
        }

        if (skills.Any(skill => !Enum.IsDefined(skill.Proficiency)))
        {
            throw MalformedAnalysisData();
        }
    }

    private static void ValidateRequiredSkills(List<string>? requiredSkills)
    {
        if (requiredSkills is null ||
            requiredSkills.Count < 6 ||
            requiredSkills.Any(string.IsNullOrWhiteSpace))
        {
            throw MalformedAnalysisData();
        }
    }

    private static int GetClassificationRank(
        SkillGapClassification classification)
    {
        return classification switch
        {
            SkillGapClassification.Missing => 0,
            SkillGapClassification.NeedsDevelopment => 1,
            SkillGapClassification.Matched => 2,
            _ => throw MalformedAnalysisData()
        };
    }

    private static InvalidOperationException MalformedAnalysisData()
    {
        return new InvalidOperationException(
            "Skill-gap analysis could not be completed because the saved " +
            "profile or career catalogue data is invalid.");
    }
}
