using CareerAdvisor.Core.Models;

namespace CareerAdvisor.Core.Validators;

/// <summary>
/// Validator for assessment sessions and responses.
/// Enforces business rules for assessment completion and data integrity.
/// </summary>
public class AssessmentValidator
{
    /// <summary>
    /// Validates that a response can be added to an assessment session.
    /// Checks:
    /// - Question exists and is valid
    /// - Option exists and belongs to the question
    /// - Option value is not empty
    /// - Response is not a duplicate
    /// </summary>
    /// <param name="session">The assessment session containing responses.</param>
    /// <param name="questionId">The ID of the question being answered.</param>
    /// <param name="optionId">The ID of the selected option.</param>
    /// <param name="allQuestions">All available questions for validation.</param>
    /// <returns>ValidationResult with any errors found.</returns>
    public ValidationResult ValidateResponse(
        AssessmentSession session,
        Guid questionId,
        Guid optionId,
        List<AssessmentQuestion> allQuestions)
    {
        var result = new ValidationResult { IsValid = true };

        // Validate question exists
        var question = allQuestions.FirstOrDefault(q => q.Id == questionId);
        if (question == null)
        {
            result.Errors.Add($"Question with ID '{questionId}' does not exist.");
            result.IsValid = false;
            return result;
        }

        // Validate option exists and belongs to the question
        var option = question.Options.FirstOrDefault(o => o.Id == optionId);
        if (option == null)
        {
            result.Errors.Add($"Option with ID '{optionId}' does not exist for question '{question.Code}'.");
            result.IsValid = false;
            return result;
        }

        // Validate option value is not empty
        if (string.IsNullOrWhiteSpace(option.Value))
        {
            result.Errors.Add($"Option value for question '{question.Code}' cannot be empty.");
            result.IsValid = false;
            return result;
        }

        // Check for duplicate responses to the same question
        if (session.Responses.Any(r => r.QuestionId == questionId))
        {
            result.Errors.Add($"Duplicate response detected for question '{question.Code}'. Each question can only be answered once.");
            result.IsValid = false;
        }

        return result;
    }

    /// <summary>
    /// Validates that an assessment session is complete.
    /// Checks:
    /// - All required questions have been answered
    /// - Session status is InProgress
    /// - Session has responses
    /// </summary>
    /// <param name="session">The assessment session to validate.</param>
    /// <param name="allQuestions">All available questions to check for required ones.</param>
    /// <returns>ValidationResult with any errors found.</returns>
    public ValidationResult ValidateSessionCompletion(
        AssessmentSession session,
        List<AssessmentQuestion> allQuestions)
    {
        var result = new ValidationResult { IsValid = true };

        // Check session status
        if (session.Status != "InProgress")
        {
            result.Errors.Add($"Assessment session is not in progress (current status: '{session.Status}').");
            result.IsValid = false;
            return result;
        }

        // Check for at least one response
        if (session.Responses == null || session.Responses.Count == 0)
        {
            result.Errors.Add("Assessment session has no responses. At least one question must be answered.");
            result.IsValid = false;
            return result;
        }

        // Check all required questions have responses
        var requiredQuestions = allQuestions.Where(q => q.IsRequired).ToList();
        var answeredQuestionIds = session.Responses.Select(r => r.QuestionId).Distinct().ToList();

        var missingAnswers = requiredQuestions
            .Where(q => !answeredQuestionIds.Contains(q.Id))
            .ToList();

        if (missingAnswers.Any())
        {
            var missingCodes = string.Join(", ", missingAnswers.Select(q => $"'{q.Code}'"));
            result.Errors.Add($"The following required questions are not answered: {missingCodes}");
            result.IsValid = false;
        }

        return result;
    }

    /// <summary>
    /// Validates the integrity of an assessment response.
    /// Checks:
    /// - AssessmentSessionId is not empty
    /// - QuestionId is not empty
    /// - OptionId is not empty
    /// </summary>
    /// <param name="response">The assessment response to validate.</param>
    /// <returns>ValidationResult with any errors found.</returns>
    public ValidationResult ValidateResponseIntegrity(AssessmentResponse response)
    {
        var result = new ValidationResult { IsValid = true };

        if (response.AssessmentSessionId == Guid.Empty)
        {
            result.Errors.Add("AssessmentSessionId cannot be empty.");
        }

        if (response.QuestionId == Guid.Empty)
        {
            result.Errors.Add("QuestionId cannot be empty.");
        }

        if (response.OptionId == Guid.Empty)
        {
            result.Errors.Add("OptionId cannot be empty.");
        }

        result.IsValid = !result.Errors.Any();
        return result;
    }
}
