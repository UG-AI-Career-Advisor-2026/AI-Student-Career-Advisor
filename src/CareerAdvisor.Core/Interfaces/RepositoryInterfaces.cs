using CareerAdvisor.Core.Models;

namespace CareerAdvisor.Core.Interfaces;

public interface IStudentProfileRepository : IRepository<StudentProfile> { }

public interface ICareerRepository : IRepository<CareerProfile> { }

public interface IRecommendationRepository : IRepository<RecommendationSession> { }

public interface IRoadmapRepository : IRepository<LearningRoadmap> { }