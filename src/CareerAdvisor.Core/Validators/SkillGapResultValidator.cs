using CareerAdvisor.Core.Enums;
using CareerAdvisor.Core.Models;
using CareerAdvisor.Core.SkillGaps;

namespace CareerAdvisor.Core.Validators;

/// <summary>
/// Validates skill-gap result contracts using the repository's accumulated
/// <see cref="ValidationResult"/> error pattern.
/// </summary>
public sealed class SkillGapResultValidator
{
    /// <summary>Validates identity, baseline, item uniqueness, and item states.</summary>
    /// <param name="result">The skill-gap result to validate.</param>
    /// <returns>A validation result containing all ordinary domain errors.</returns>
    public ValidationResult Validate(SkillGapResult? result)
    {
        var validation = new ValidationResult { IsValid = true };

        if (result is null)
        {
            validation.Errors.Add("Skill-gap result is required.");
            validation.IsValid = false;
            return validation;
        }

        if (result.StudentProfileId == Guid.Empty)
        {
            validation.Errors.Add("StudentProfileId cannot be empty.");
        }

        if (result.CareerProfileId == Guid.Empty)
        {
            validation.Errors.Add("CareerProfileId cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(result.CareerCode))
        {
            validation.Errors.Add("Career code is required.");
        }

        if (string.IsNullOrWhiteSpace(result.CareerTitle))
        {
            validation.Errors.Add("Career title is required.");
        }

        if (!Enum.IsDefined(result.BaselineProficiency) ||
            result.BaselineProficiency != SkillGapRules.BaselineProficiency)
        {
            validation.Errors.Add(
                "The skill-gap baseline must be Intermediate.");
        }

        if (result.Items is null || result.Items.Count == 0)
        {
            validation.Errors.Add(
                "At least one required-skill result item is required.");
            validation.IsValid = false;
            return validation;
        }

        ValidateItems(result.Items, validation);

        validation.IsValid = validation.Errors.Count == 0;
        return validation;
    }

    private static void ValidateItems(
        IReadOnlyList<SkillGapItem> items,
        ValidationResult validation)
    {
        for (var index = 0; index < items.Count; index++)
        {
            var item = items[index];
            var label = $"Skill-gap item {index + 1}";

            if (item is null)
            {
                validation.Errors.Add($"{label} is required.");
                continue;
            }

            if (string.IsNullOrWhiteSpace(item.RequiredSkillName))
            {
                validation.Errors.Add(
                    $"{label} must include a required skill name.");
            }

            if (!Enum.IsDefined(item.Classification))
            {
                validation.Errors.Add(
                    $"{label} has an invalid classification.");
                continue;
            }

            if (item.CurrentProficiency is not null &&
                !Enum.IsDefined(item.CurrentProficiency.Value))
            {
                validation.Errors.Add(
                    $"{label} has an invalid current proficiency.");
                continue;
            }

            ValidateItemState(item, label, validation);
        }

        var duplicateRequiredSkills = items
            .Where(item => item is not null)
            .Select(item => SkillNameNormalizer.Normalize(
                item.RequiredSkillName))
            .Where(name => name.Length > 0)
            .GroupBy(name => name, StringComparer.Ordinal)
            .Any(group => group.Count() > 1);

        if (duplicateRequiredSkills)
        {
            validation.Errors.Add(
                "Duplicate normalized required skills are not allowed.");
        }
    }

    private static void ValidateItemState(
        SkillGapItem item,
        string label,
        ValidationResult validation)
    {
        var hasMatchedName =
            !string.IsNullOrWhiteSpace(item.MatchedStudentSkillName);

        switch (item.Classification)
        {
            case SkillGapClassification.Missing:
                if (hasMatchedName || item.CurrentProficiency is not null)
                {
                    validation.Errors.Add(
                        $"{label} classified as Missing cannot include " +
                        "a matched skill or proficiency.");
                }
                break;

            case SkillGapClassification.NeedsDevelopment:
                if (!hasMatchedName ||
                    item.CurrentProficiency != SkillProficiency.Beginner)
                {
                    validation.Errors.Add(
                        $"{label} classified as NeedsDevelopment must include " +
                        "one Beginner matched skill.");
                }
                break;

            case SkillGapClassification.Matched:
                if (!hasMatchedName ||
                    item.CurrentProficiency is null ||
                    item.CurrentProficiency < SkillGapRules.BaselineProficiency)
                {
                    validation.Errors.Add(
                        $"{label} classified as Matched must include one " +
                        "matched skill at Intermediate proficiency or higher.");
                }
                break;
        }
    }
}
