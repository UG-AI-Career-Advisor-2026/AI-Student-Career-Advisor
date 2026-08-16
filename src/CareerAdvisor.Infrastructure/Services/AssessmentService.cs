using CareerAdvisor.Core.Interfaces;
using CareerAdvisor.Core.Models;
using CareerAdvisor.Core.Validators;
using CareerAdvisor.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CareerAdvisor.Infrastructure.Services;

/// <summary>
/// EF Core / SQLite-backed implementation of IAssessmentService.
/// </summary>
public class AssessmentService : IAssessmentService
{
    private readonly CareerAdvisorDbContext _db;
    private readonly AssessmentValidator _validator = new();

    public AssessmentService(CareerAdvisorDbContext db) => _db = db;

    public AssessmentSession CreateAssessmentSession(Guid studentProfileId)
    {
        var session = new AssessmentSession { StudentProfileId = studentProfileId };
        _db.AssessmentSessions.Add(session);
        _db.SaveChanges();
        return session;
    }

    public List<AssessmentQuestion> GetAllQuestions() =>
        AssessmentQuestionBank.GetAllQuestions();

    public AssessmentQuestion? GetQuestion(Guid questionId) =>
        AssessmentQuestionBank.GetAllQuestions().FirstOrDefault(q => q.Id == questionId);

    public AssessmentSession? GetAssessmentSession(Guid sessionId) =>
        _db.AssessmentSessions
            .Include(s => s.Responses)
            .FirstOrDefault(s => s.Id == sessionId);

    public ValidationResult SubmitResponse(AssessmentSession session, Guid questionId, Guid optionId)
    {
        var result = _validator.ValidateResponse(session, questionId, optionId, GetAllQuestions());
        if (!result.IsValid) return result;

        var existing = _db.AssessmentResponses
            .FirstOrDefault(r => r.AssessmentSessionId == session.Id && r.QuestionId == questionId);

        if (existing is null)
        {
            _db.AssessmentResponses.Add(new AssessmentResponse
            {
                AssessmentSessionId = session.Id,
                QuestionId = questionId,
                OptionId = optionId,
                RespondedAt = DateTime.UtcNow
            });
        }
        else
        {
            existing.OptionId = optionId;
            existing.RespondedAt = DateTime.UtcNow;
        }

        _db.SaveChanges();
        return result;
    }

    public ValidationResult CompleteAssessmentSession(AssessmentSession session)
    {
        var result = _validator.ValidateSessionCompletion(session, GetAllQuestions());
        if (!result.IsValid) return result;

        var entity = _db.AssessmentSessions
            .FirstOrDefault(s => s.Id == session.Id);

        if (entity is null)
        {
            _db.AssessmentSessions.Add(session);
        }
        else
        {
            entity.Status = "Completed";
            entity.CompletedAt = DateTime.UtcNow;
            entity.StudentProfileId = session.StudentProfileId;
        }

        _db.SaveChanges();
        return result;
    }

    public List<AssessmentResponse> GetSessionResponses(Guid sessionId) =>
        _db.AssessmentResponses.Where(r => r.AssessmentSessionId == sessionId).ToList();
}