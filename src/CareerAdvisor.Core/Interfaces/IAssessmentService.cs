using CareerAdvisor.Core.Models;
using CareerAdvisor.Core.Validators;

namespace CareerAdvisor.Core.Interfaces;

/// <summary>
/// Interface for managing assessment sessions, questions, and responses.
/// Handles assessment creation, submission, and validation.
/// </summary>
public interface IAssessmentService
{
    /// <summary>
    /// Creates a new assessment session for a student.
    /// </summary>
    /// <param name="studentProfileId">The ID of the student profile starting the assessment.</param>
    /// <returns>A new AssessmentSession with a unique ID.</returns>
    AssessmentSession CreateAssessmentSession(Guid studentProfileId);

    /// <summary>
/// Gets the most recently updated student profile available
/// for the single-user MVP.
/// </summary>
Guid? GetAvailableStudentProfileId();

    /// <summary>
    /// Gets all available assessment questions.
    /// </summary>
    /// <returns>A list of all AssessmentQuestion objects.</returns>
    List<AssessmentQuestion> GetAllQuestions();

    /// <summary>
    /// Gets a specific assessment question by its ID.
    /// </summary>
    /// <param name="questionId">The ID of the question to retrieve.</param>
    /// <returns>The AssessmentQuestion if found, otherwise null.</returns>
    AssessmentQuestion? GetQuestion(Guid questionId);

    /// <summary>
    /// Gets an assessment session by its ID.
    /// </summary>
    /// <param name="sessionId">The ID of the session to retrieve.</param>
    /// <returns>The AssessmentSession if found, otherwise null.</returns>
    AssessmentSession? GetAssessmentSession(Guid sessionId);

    /// <summary>
    /// Submits a response to an assessment question.
    /// Validates that the response is valid before adding it to the session.
    /// </summary>
    /// <param name="session">The assessment session to add the response to.</param>
    /// <param name="questionId">The ID of the question being answered.</param>
    /// <param name="optionId">The ID of the selected option.</param>
    /// <returns>A ValidationResult indicating success or failure with error messages.</returns>
    ValidationResult SubmitResponse(AssessmentSession session, Guid questionId, Guid optionId);

    /// <summary>
    /// Completes an assessment session after all questions have been answered.
    /// </summary>
    /// <param name="session">The assessment session to complete.</param>
    /// <returns>A ValidationResult indicating if the session was successfully completed.</returns>
    ValidationResult CompleteAssessmentSession(AssessmentSession session);

    /// <summary>
    /// Gets all responses for a specific assessment session.
    /// </summary>
    /// <param name="sessionId">The ID of the assessment session.</param>
    /// <returns>A list of AssessmentResponse objects for the session.</returns>
    List<AssessmentResponse> GetSessionResponses(Guid sessionId);
}
