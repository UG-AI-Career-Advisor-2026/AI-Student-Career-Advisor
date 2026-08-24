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
    Task<CareerProfile?> GetCareerByCodeAsync(string code);
}

/// <summary>
/// Generates career recommendations for a student profile.
/// </summary>
public interface IRecommendationService
{
    Task<RecommendationSession> GenerateRecommendationsAsync(Guid studentProfileId);
}

/// <summary>
/// Performs a deterministic comparison between a saved profile and a supported
/// career's required skills.
/// </summary>
public interface ISkillGapService
{
    /// <summary>Analyzes the saved skills for one profile and career.</summary>
    /// <param name="studentProfileId">The saved student profile identifier.</param>
    /// <param name="careerProfileId">The supported career identifier.</param>
    /// <returns>A validated deterministic skill-gap result.</returns>
    Task<SkillGapResult> AnalyzeAsync(
        Guid studentProfileId,
        Guid careerProfileId);
}

/// <summary>
/// Distinct history contract: provides access to a student's
/// past recommendation sessions.
/// </summary>
public interface IRecommendationHistoryService
{
    Task<IEnumerable<RecommendationSession>> GetHistoryAsync(Guid studentProfileId);
    Task<RecommendationSession?> GetSessionAsync(
        Guid studentProfileId,
        Guid sessionId);
}

/// <summary>
/// Generates, reads, and updates persisted profile-scoped learning roadmaps.
/// </summary>
public interface IRoadmapService
{
    Task<LearningRoadmap> GenerateRoadmapAsync(
        Guid studentProfileId,
        Guid careerProfileId);

    Task<LearningRoadmap> GetRoadmapAsync(
        Guid studentProfileId,
        Guid roadmapId);

    Task<LearningRoadmap> UpdateRoadmapProgressAsync(
        Guid studentProfileId,
        Guid roadmapId,
        Guid stepId,
        bool isCompleted);
}
