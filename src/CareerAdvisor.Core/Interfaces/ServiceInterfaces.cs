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

public interface IRecommendationService
{
    Task<RecommendationSession> GenerateRecommendationsAsync(Guid studentId);
    Task<IEnumerable<RecommendationSession>> GetHistoryAsync(Guid studentId);
}

public interface IRoadmapService
{
    Task<LearningRoadmap> GenerateRoadmapAsync(Guid studentId, Guid careerId);
    Task UpdateRoadmapProgressAsync(Guid roadmapId, Guid stepId);
}