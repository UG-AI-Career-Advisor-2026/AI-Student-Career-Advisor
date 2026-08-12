using CareerAdvisor.Core.Models;

namespace CareerAdvisor.Core.Interfaces;

public interface IStudentProfileService
{
    Task<StudentProfile> CreateProfileAsync(StudentProfile profile);
    Task<StudentProfile?> UpdateProfileAsync(Guid id, StudentProfile profile);
    Task<StudentProfile?> GetProfileAsync(Guid id);
}

public interface ICareerService
{
    Task<IEnumerable<CareerProfile>> GetAllCareersAsync();
    Task<CareerProfile?> GetCareerByIdAsync(Guid id);
}

/// <summary>
/// Generates career recommendations for a student profile.
/// </summary>
public interface IRecommendationService
{
    Task<RecommendationSession> GenerateRecommendationsAsync(Guid studentProfileId);
}

/// <summary>
/// Distinct history contract (Issue #1): provides access to a student's
/// past recommendation sessions.
/// </summary>
public interface IRecommendationHistoryService
{
    Task<IEnumerable<RecommendationSession>> GetHistoryAsync(Guid studentProfileId);
    Task<RecommendationSession?> GetSessionAsync(Guid sessionId);
}

public interface IRoadmapService
{
    Task<LearningRoadmap> GenerateRoadmapAsync(Guid studentProfileId, Guid careerProfileId);
    Task UpdateRoadmapProgressAsync(Guid roadmapId, Guid stepId);
}