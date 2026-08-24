using CareerAdvisor.Core.Models;
using CareerAdvisor.Core.SkillGaps;

namespace CareerAdvisor.Core.Validators;

/// <summary>
/// Validates persisted learning-roadmap aggregates using accumulated domain
/// validation errors.
/// </summary>
public sealed class LearningRoadmapValidator
{
    /// <summary>The academic MVP maximum number of roadmap steps.</summary>
    public const int MaximumStepCount = 12;

    /// <summary>The maximum persisted roadmap-step title length.</summary>
    public const int MaximumTitleLength = 200;

    /// <summary>The maximum persisted roadmap-step description length.</summary>
    public const int MaximumDescriptionLength = 1000;

    /// <summary>
    /// Validates roadmap identity, ownership, ordering, content, and completion
    /// state.
    /// </summary>
    public ValidationResult Validate(LearningRoadmap? roadmap)
    {
        var validation = new ValidationResult { IsValid = true };

        if (roadmap is null)
        {
            validation.Errors.Add("Learning roadmap is required.");
            validation.IsValid = false;
            return validation;
        }

        if (roadmap.Id == Guid.Empty)
        {
            validation.Errors.Add("Roadmap ID cannot be empty.");
        }

        if (roadmap.StudentProfileId == Guid.Empty)
        {
            validation.Errors.Add("StudentProfileId cannot be empty.");
        }

        if (roadmap.CareerProfileId == Guid.Empty)
        {
            validation.Errors.Add("CareerProfileId cannot be empty.");
        }

        if (roadmap.CreatedAt == default ||
            roadmap.CreatedAt.Kind != DateTimeKind.Utc)
        {
            validation.Errors.Add("CreatedAt must be a nondefault UTC value.");
        }

        if (roadmap.Steps is null ||
            roadmap.Steps.Count is < 1 or > MaximumStepCount)
        {
            validation.Errors.Add(
                $"A roadmap must contain between 1 and {MaximumStepCount} steps.");
            validation.IsValid = false;
            return validation;
        }

        ValidateSteps(roadmap, validation);
        validation.IsValid = validation.Errors.Count == 0;
        return validation;
    }

    private static void ValidateSteps(
        LearningRoadmap roadmap,
        ValidationResult validation)
    {
        var expectedOrders = Enumerable.Range(1, roadmap.Steps.Count);

        for (var index = 0; index < roadmap.Steps.Count; index++)
        {
            var step = roadmap.Steps[index];
            var label = $"Roadmap step {index + 1}";

            if (step is null)
            {
                validation.Errors.Add($"{label} is required.");
                continue;
            }

            if (step.Id == Guid.Empty)
            {
                validation.Errors.Add($"{label} ID cannot be empty.");
            }

            if (step.LearningRoadmapId == Guid.Empty ||
                step.LearningRoadmapId != roadmap.Id)
            {
                validation.Errors.Add($"{label} must belong to its roadmap.");
            }

            if (string.IsNullOrWhiteSpace(step.Title) ||
                step.Title.Length > MaximumTitleLength)
            {
                validation.Errors.Add(
                    $"{label} title is required and must not exceed " +
                    $"{MaximumTitleLength} characters.");
            }

            if (string.IsNullOrWhiteSpace(step.Description) ||
                step.Description.Length > MaximumDescriptionLength)
            {
                validation.Errors.Add(
                    $"{label} description is required and must not exceed " +
                    $"{MaximumDescriptionLength} characters.");
            }

            if (step.ResourceLink != string.Empty)
            {
                validation.Errors.Add(
                    $"{label} cannot include a generated resource link.");
            }

            if (step.IsCompleted)
            {
                if (step.CompletedAt is null ||
                    step.CompletedAt.Value == default ||
                    step.CompletedAt.Value.Kind != DateTimeKind.Utc)
                {
                    validation.Errors.Add(
                        $"{label} must include a UTC completion time.");
                }
            }
            else if (step.CompletedAt is not null)
            {
                validation.Errors.Add(
                    $"{label} cannot include a completion time while incomplete.");
            }
        }

        if (roadmap.Steps
                .Where(step => step is not null)
                .Select(step => step.Id)
                .Distinct()
                .Count() != roadmap.Steps.Count)
        {
            validation.Errors.Add("Roadmap step IDs must be unique.");
        }

        if (!roadmap.Steps
                .Where(step => step is not null)
                .Select(step => step.Order)
                .Order()
                .SequenceEqual(expectedOrders))
        {
            validation.Errors.Add(
                "Roadmap step orders must be unique and contiguous from 1.");
        }

        var normalizedTitles = roadmap.Steps
            .Where(step => step is not null)
            .Select(step => SkillNameNormalizer.Normalize(step.Title))
            .ToList();

        if (normalizedTitles.Any(string.IsNullOrWhiteSpace) ||
            normalizedTitles.Distinct(StringComparer.Ordinal).Count() !=
            normalizedTitles.Count)
        {
            validation.Errors.Add(
                "Roadmap step titles must be unique after normalization.");
        }
    }
}
