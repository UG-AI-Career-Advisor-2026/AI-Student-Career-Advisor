using CareerAdvisor.Core.Interfaces;
using CareerAdvisor.Core.Models;

namespace CareerAdvisor.Infrastructure.Services;

/// <summary>
/// Reads and validates persisted recommendation history for a saved profile.
/// </summary>
public sealed class RecommendationHistoryService
    : IRecommendationHistoryService
{
    private readonly IStudentProfileRepository _profileRepository;
    private readonly IRecommendationRepository _recommendationRepository;

    public RecommendationHistoryService(
        IStudentProfileRepository profileRepository,
        IRecommendationRepository recommendationRepository)
    {
        _profileRepository = profileRepository;
        _recommendationRepository = recommendationRepository;
    }

    public async Task<IEnumerable<RecommendationSession>> GetHistoryAsync(
        Guid studentProfileId)
    {
        await EnsureSavedProfileAsync(studentProfileId);

        var sessions = (await _recommendationRepository
                .GetByStudentProfileIdAsync(studentProfileId))
            .ToList();

        foreach (var session in sessions)
        {
            ValidateAndOrderSession(session);
        }

        return sessions
            .OrderByDescending(session => session.GeneratedAt)
            .ThenBy(session => session.Id)
            .ToList();
    }

    public async Task<RecommendationSession?> GetSessionAsync(
        Guid studentProfileId,
        Guid sessionId)
    {
        await EnsureSavedProfileAsync(studentProfileId);

        if (sessionId == Guid.Empty)
        {
            throw new InvalidOperationException(
                "A valid recommendation session is required.");
        }

        var session = await _recommendationRepository.GetByIdAsync(
            sessionId);

        if (session is null ||
            session.StudentProfileId != studentProfileId)
        {
            return null;
        }

        ValidateAndOrderSession(session);
        return session;
    }

    private async Task EnsureSavedProfileAsync(Guid studentProfileId)
    {
        if (studentProfileId == Guid.Empty)
        {
            throw new InvalidOperationException(
                "A valid saved student profile is required.");
        }

        var profile = await _profileRepository.GetByIdAsync(
            studentProfileId);

        if (profile is null)
        {
            throw new InvalidOperationException(
                "The requested student profile could not be found.");
        }
    }

    private static void ValidateAndOrderSession(
        RecommendationSession? session)
    {
        if (session is null)
        {
            throw InvalidSession("the saved session is unavailable");
        }

        var recommendations = session.Recommendations;

        if (recommendations is null)
        {
            throw InvalidSession("its recommendations are unavailable");
        }

        if (recommendations.Any(recommendation => recommendation is null))
        {
            throw InvalidSession(
                "one or more saved recommendations are unavailable");
        }

        if (recommendations.Count != 3)
        {
            throw InvalidSession(
                "it does not contain exactly three recommendations");
        }

        if (recommendations
                .Select(recommendation =>
                    recommendation.CareerProfileId)
                .Distinct()
                .Count() != 3)
        {
            throw InvalidSession(
                "it contains duplicate careers");
        }

        if (recommendations.Any(recommendation =>
                recommendation.Career is null ||
                string.IsNullOrWhiteSpace(
                    recommendation.Career.Title) ||
                string.IsNullOrWhiteSpace(
                    recommendation.Career.Description)))
        {
            throw InvalidSession(
                "one or more career details are unavailable");
        }

        if (recommendations.Any(recommendation =>
                !double.IsFinite(recommendation.MatchScore) ||
                recommendation.MatchScore < 0 ||
                recommendation.MatchScore > 1))
        {
            throw InvalidSession(
                "one or more saved match scores are invalid");
        }

        if (recommendations.Any(recommendation =>
                string.IsNullOrWhiteSpace(
                    recommendation.Reasoning)))
        {
            throw InvalidSession(
                "one or more saved explanations are unavailable");
        }

        session.Recommendations = recommendations
            .OrderByDescending(recommendation =>
                recommendation.MatchScore)
            .ThenBy(
                recommendation => recommendation.Career!.Title,
                StringComparer.Ordinal)
            .ThenBy(recommendation =>
                recommendation.CareerProfileId)
            .ToList();
    }

    private static InvalidOperationException InvalidSession(
        string reason)
    {
        return new InvalidOperationException(
            "The saved recommendation session cannot be displayed because " +
            $"{reason}.");
    }
}
