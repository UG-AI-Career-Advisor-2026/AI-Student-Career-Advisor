using CareerAdvisor.Core.Models;

namespace CareerAdvisor.Core.Interfaces;

public interface IStudentProfileRepository : IRepository<StudentProfile> { }

public interface ICareerRepository : IRepository<CareerProfile>
{
    Task<CareerProfile?> GetByCodeAsync(string code);
}

/// <summary>
/// Persists recommendation sessions; backs the history contract.
/// </summary>
public interface IRecommendationRepository : IRepository<RecommendationSession>
{
    Task<IEnumerable<RecommendationSession>> GetByStudentProfileIdAsync(Guid studentProfileId);
}

/// <summary>
/// Persists and reopens profile-scoped learning-roadmap aggregates.
/// </summary>
public interface IRoadmapRepository
{
    Task<LearningRoadmap?> GetByIdForProfileAsync(
        Guid studentProfileId,
        Guid roadmapId);

    Task<LearningRoadmap?> GetByProfileAndCareerAsync(
        Guid studentProfileId,
        Guid careerProfileId);

    Task<bool> TryAddAsync(LearningRoadmap roadmap);

    Task<bool> SetStepCompletionAsync(
        Guid studentProfileId,
        Guid roadmapId,
        Guid stepId,
        bool isCompleted,
        DateTime? completedAt);
}
