using CareerAdvisor.Core.Models;

namespace CareerAdvisor.Core.Validators;

public class StudentProfileValidator
{
    public ValidationResult Validate(StudentProfile profile)
    {
        var result = new ValidationResult { IsValid = true };

        if (string.IsNullOrWhiteSpace(profile.Name))
            result.Errors.Add("Name is required.");

        if (string.IsNullOrWhiteSpace(profile.Programme))
            result.Errors.Add("Programme is required.");

        if (profile.Interests == null || profile.Interests.Count == 0)
            result.Errors.Add("At least one interest is required.");

        if (profile.Skills == null || profile.Skills.Count == 0)
            result.Errors.Add("At least one skill is required.");

        if (profile.Skills != null)
        {
            if (profile.Skills.Any(s => string.IsNullOrWhiteSpace(s.SkillName)))
                result.Errors.Add("Skill names cannot be empty.");

            // Check for duplicate skills ignoring case
            var duplicateSkills = profile.Skills.GroupBy(s => s.SkillName, StringComparer.OrdinalIgnoreCase)
                                                .Any(g => g.Count() > 1);
            if (duplicateSkills)
                result.Errors.Add("Duplicate skills are not allowed.");
        }

        if (profile.Interests != null)
        {
            // Check for duplicate interests ignoring case
            var duplicateInterests = profile.Interests.GroupBy(i => i, StringComparer.OrdinalIgnoreCase)
                                                      .Any(g => g.Count() > 1);
            if (duplicateInterests)
                result.Errors.Add("Duplicate interests are not allowed.");
        }

        if (profile.UpdatedAt < profile.CreatedAt)
            result.Errors.Add("Updated date cannot be earlier than created date.");

        result.IsValid = !result.Errors.Any();
        return result;
    }
}