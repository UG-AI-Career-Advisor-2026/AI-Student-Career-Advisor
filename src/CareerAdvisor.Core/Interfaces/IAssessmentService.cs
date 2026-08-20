using CareerAdvisor.Core.Models;
using CareerAdvisor.Core.Validators;

namespace CareerAdvisor.Core.Interfaces;

/// <summary>
/// Interface for managing assessment sessions, questions and responses.
/// Handles assessment creation, submission and validation.
/// </summary>
public interface IAssessmentService
{
    /// <summary>
    /// Creates a new assessment session for a student.
    /// </summary>
    /// <param name="studentProfileId">
    /// The ID of the student profile starting the assessment.
    /// </param>
    /// <returns>
    /// A new assessment session with a unique ID.
    /// </returns>
    AssessmentSession CreateAssessmentSession(Guid studentProfileId);

    /// <summary>
    /// Gets the most recently updated student profile available
    /// for the single-user MVP.
    /// </summary>
    Guid? GetAvailableStudentProfileId();

    /// <summary>
    /// Gets all available assessment questions.
    /// </summary>
    /// <returns>
    /// A list of all assessment questions.
    /// </returns>
    List<AssessmentQuestion> GetAllQuestions();

    /// <summary>
    /// Gets a specific assessment question by its ID.
    /// </summary>
    /// <param name="questionId">
    /// The ID of the question to retrieve.
    /// </param>
    /// <returns>
    /// The assessment question if found; otherwise, null.
    /// </returns>
    AssessmentQuestion? GetQuestion(Guid questionId);

    /// <summary>
    /// Gets an assessment session by its ID.
    /// </summary>
    /// <param name="sessionId">
    /// The ID of the session to retrieve.
    /// </param>
    /// <returns>
    /// The assessment session if found; otherwise, null.
    /// </returns>
    AssessmentSession? GetAssessmentSession(Guid sessionId);

    /// <summary>
    /// Gets the most recently completed assessment for a student profile,
    /// including all persisted responses.
    /// </summary>
    /// <param name="studentProfileId">
    /// The student profile whose assessment should be retrieved.
    /// </param>
    /// <returns>
    /// The latest completed assessment, or null when none exists.
    /// </returns>
    Task<AssessmentSession?> GetLatestCompletedAssessmentAsync(
        Guid studentProfileId);

    /// <summary>
    /// Submits a response to an assessment question.
    /// </summary>
    /// <param name="session">
    /// The assessment session receiving the response.
    /// </param>
    /// <param name="questionId">
    /// The ID of the question being answered.
    /// </param>
    /// <param name="optionId">
    /// The ID of the selected option.
    /// </param>
    /// <returns>
    /// A validation result indicating success or failure.
    /// </returns>
    ValidationResult SubmitResponse(
        AssessmentSession session,
        Guid questionId,
        Guid optionId);

    /// <summary>
    /// Completes an assessment after all required questions
    /// have been answered.
    /// </summary>
    /// <param name="session">
    /// The assessment session to complete.
    /// </param>
    /// <returns>
    /// A validation result indicating whether completion succeeded.
    /// </returns>
    ValidationResult CompleteAssessmentSession(
        AssessmentSession session);

    /// <summary>
    /// Gets all responses for a specific assessment session.
    /// </summary>
    /// <param name="sessionId">
    /// The ID of the assessment session.
    /// </param>
    /// <returns>
    /// The responses recorded for the session.
    /// </returns>
    List<AssessmentResponse> GetSessionResponses(Guid sessionId);
}