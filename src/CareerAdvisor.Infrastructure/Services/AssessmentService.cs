using CareerAdvisor.Core.Interfaces;
using CareerAdvisor.Core.Models;
using CareerAdvisor.Core.Validators;
using CareerAdvisor.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CareerAdvisor.Infrastructure.Services;

/// <summary>
/// SQLite-backed implementation of the assessment workflow.
/// </summary>
public sealed class AssessmentService : IAssessmentService
{
    private readonly CareerAdvisorDbContext _db;
    private readonly AssessmentValidator _validator = new();

    public AssessmentService(CareerAdvisorDbContext db)
    {
        _db = db;
    }

    public AssessmentSession CreateAssessmentSession(
        Guid studentProfileId)
    {
        if (studentProfileId == Guid.Empty ||
            !_db.StudentProfiles.Any(profile =>
                profile.Id == studentProfileId))
        {
            throw new InvalidOperationException(
                "A valid student profile is required before starting " +
                "an assessment.");
        }

        var session = new AssessmentSession
        {
            StudentProfileId = studentProfileId
        };

        _db.AssessmentSessions.Add(session);
        _db.SaveChanges();

        return session;
    }

    public Guid? GetAvailableStudentProfileId()
    {
        return _db.StudentProfiles
            .AsNoTracking()
            .OrderByDescending(profile => profile.UpdatedAt)
            .Select(profile => (Guid?)profile.Id)
            .FirstOrDefault();
    }

    public List<AssessmentQuestion> GetAllQuestions()
    {
        return AssessmentQuestionBank.GetAllQuestions();
    }

    public AssessmentQuestion? GetQuestion(Guid questionId)
    {
        return AssessmentQuestionBank.GetAllQuestions()
            .FirstOrDefault(question => question.Id == questionId);
    }

    public AssessmentSession? GetAssessmentSession(Guid sessionId)
    {
        return _db.AssessmentSessions
            .AsNoTracking()
            .Include(session => session.Responses)
            .SingleOrDefault(session => session.Id == sessionId);
    }

    public ValidationResult SubmitResponse(
        AssessmentSession session,
        Guid questionId,
        Guid optionId)
    {
        ArgumentNullException.ThrowIfNull(session);

        var persistedSession = _db.AssessmentSessions
            .Include(item => item.Responses)
            .SingleOrDefault(item => item.Id == session.Id);

        if (persistedSession is null)
        {
            return Invalid(
                "The assessment session could not be found.");
        }

        if (!string.Equals(
                persistedSession.Status,
                "InProgress",
                StringComparison.Ordinal))
        {
            return Invalid(
                "Responses cannot be changed after the assessment " +
                "has been completed.");
        }

        var existingResponse = persistedSession.Responses
            .SingleOrDefault(response =>
                response.QuestionId == questionId);

        // Exclude the response being edited so the core validator can
        // still reject genuine duplicate responses while allowing an
        // existing answer to be changed.
        var validationSession = new AssessmentSession
        {
            Id = persistedSession.Id,
            StudentProfileId = persistedSession.StudentProfileId,
            Status = persistedSession.Status,
            StartedAt = persistedSession.StartedAt,
            CompletedAt = persistedSession.CompletedAt,
            Responses = persistedSession.Responses
                .Where(response =>
                    response.QuestionId != questionId)
                .ToList()
        };

        var result = _validator.ValidateResponse(
            validationSession,
            questionId,
            optionId,
            GetAllQuestions());

        if (!result.IsValid)
        {
            return result;
        }

        if (existingResponse is null)
        {
            var response = new AssessmentResponse
            {
                AssessmentSessionId = persistedSession.Id,
                QuestionId = questionId,
                OptionId = optionId,
                RespondedAt = DateTime.UtcNow
            };

            _db.AssessmentResponses.Add(response);
        }
        else
        {
            existingResponse.OptionId = optionId;
            existingResponse.RespondedAt = DateTime.UtcNow;
        }

        _db.SaveChanges();
        return result;
    }

    public ValidationResult CompleteAssessmentSession(
        AssessmentSession session)
    {
        ArgumentNullException.ThrowIfNull(session);

        var persistedSession = _db.AssessmentSessions
            .Include(item => item.Responses)
            .SingleOrDefault(item => item.Id == session.Id);

        if (persistedSession is null)
        {
            return Invalid(
                "The assessment session could not be found.");
        }

        var result = _validator.ValidateSessionCompletion(
            persistedSession,
            GetAllQuestions());

        if (!result.IsValid)
        {
            return result;
        }

        var completedAt = DateTime.UtcNow;

        persistedSession.Status = "Completed";
        persistedSession.CompletedAt = completedAt;

        _db.SaveChanges();

        session.Status = "Completed";
        session.CompletedAt = completedAt;

        return result;
    }

    public List<AssessmentResponse> GetSessionResponses(
        Guid sessionId)
    {
        return _db.AssessmentResponses
            .AsNoTracking()
            .Where(response =>
                response.AssessmentSessionId == sessionId)
            .OrderBy(response => response.RespondedAt)
            .ToList();
    }

    private static ValidationResult Invalid(string error)
    {
        var result = new ValidationResult
        {
            IsValid = false
        };

        result.Errors.Add(error);
        return result;
    }
}