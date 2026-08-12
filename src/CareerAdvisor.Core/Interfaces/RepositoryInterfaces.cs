using CareerAdvisor.Core.Models;

namespace CareerAdvisor.Core.Interfaces;

public interface IStudentProfileRepository : IRepository<StudentProfile> { }

public interface ICareerRepository : IRepository<CareerProfile> { }

/// <summary>
/// Persists recommendation sessions; backs the history contract.
/// </summary>
public interface IRecommendationRepository : IRepository<RecommendationSession>
{
    Task<IEnumerable<RecommendationSession>> GetByStudentProfileIdAsync(Guid studentProfileId);
}

public interface IRoadmapRepository : IRepository<LearningRoadmap> { }
