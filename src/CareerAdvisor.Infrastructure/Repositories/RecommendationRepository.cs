using CareerAdvisor.Core.Interfaces;
using CareerAdvisor.Core.Models;
using CareerAdvisor.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CareerAdvisor.Infrastructure.Repositories;

public sealed class RecommendationRepository : IRecommendationRepository
{
    private readonly CareerAdvisorDbContext _dbContext;

    public RecommendationRepository(
        CareerAdvisorDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<RecommendationSession?> GetByIdAsync(Guid id)
    {
        return await _dbContext.RecommendationSessions
            .AsNoTracking()
            .Include(session => session.Recommendations)
                .ThenInclude(recommendation => recommendation.Career)
            .SingleOrDefaultAsync(session => session.Id == id);
    }

    public async Task<IEnumerable<RecommendationSession>> GetAllAsync()
    {
        return await _dbContext.RecommendationSessions
            .AsNoTracking()
            .Include(session => session.Recommendations)
                .ThenInclude(recommendation => recommendation.Career)
            .OrderByDescending(session => session.GeneratedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<RecommendationSession>>
        GetByStudentProfileIdAsync(Guid studentProfileId)
    {
        return await _dbContext.RecommendationSessions
            .AsNoTracking()
            .Where(session =>
                session.StudentProfileId == studentProfileId)
            .Include(session => session.Recommendations)
                .ThenInclude(recommendation => recommendation.Career)
            .OrderByDescending(session => session.GeneratedAt)
            .ToListAsync();
    }

    public async Task AddAsync(RecommendationSession entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        if (entity.Id == Guid.Empty)
        {
            entity.Id = Guid.NewGuid();
        }

        if (entity.GeneratedAt == default)
        {
            entity.GeneratedAt = DateTime.UtcNow;
        }

        await ValidateCareerReferencesAsync(entity.Recommendations);

        foreach (var recommendation in entity.Recommendations)
        {
            if (recommendation.Id == Guid.Empty)
            {
                recommendation.Id = Guid.NewGuid();
            }

            recommendation.RecommendationSessionId = entity.Id;

            // CareerProfile already exists in the database. Only its
            // foreign-key ID should be persisted with the recommendation.
            recommendation.Career = null;
        }

        await _dbContext.RecommendationSessions.AddAsync(entity);
        await _dbContext.SaveChangesAsync();
    }

    public async Task UpdateAsync(RecommendationSession entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        var existingSession = await _dbContext.RecommendationSessions
            .Include(session => session.Recommendations)
            .SingleOrDefaultAsync(session => session.Id == entity.Id);

        if (existingSession is null)
        {
            throw new KeyNotFoundException(
                $"Recommendation session '{entity.Id}' was not found.");
        }

        await ValidateCareerReferencesAsync(entity.Recommendations);

        existingSession.StudentProfileId = entity.StudentProfileId;
        existingSession.GeneratedAt = entity.GeneratedAt;

        var incomingRecommendationIds = entity.Recommendations
            .Where(recommendation =>
                recommendation.Id != Guid.Empty)
            .Select(recommendation => recommendation.Id)
            .ToHashSet();

        var removedRecommendations = existingSession.Recommendations
            .Where(recommendation =>
                !incomingRecommendationIds.Contains(recommendation.Id))
            .ToList();

        _dbContext.CareerRecommendations.RemoveRange(
            removedRecommendations);

        foreach (var incoming in entity.Recommendations)
        {
            var existing = incoming.Id == Guid.Empty
                ? null
                : existingSession.Recommendations.SingleOrDefault(
                    recommendation =>
                        recommendation.Id == incoming.Id);

            if (existing is not null)
            {
                existing.CareerProfileId = incoming.CareerProfileId;
                existing.MatchScore = incoming.MatchScore;
                existing.Reasoning = incoming.Reasoning;
                continue;
            }

            var newRecommendation = new CareerRecommendation
            {
                Id = incoming.Id == Guid.Empty
                    ? Guid.NewGuid()
                    : incoming.Id,
                RecommendationSessionId = existingSession.Id,
                CareerProfileId = incoming.CareerProfileId,
                MatchScore = incoming.MatchScore,
                Reasoning = incoming.Reasoning
            };

            existingSession.Recommendations.Add(newRecommendation);
        }

        await _dbContext.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var session = await _dbContext.RecommendationSessions
            .SingleOrDefaultAsync(session => session.Id == id);

        if (session is null)
        {
            return;
        }

        _dbContext.RecommendationSessions.Remove(session);
        await _dbContext.SaveChangesAsync();
    }

    private async Task ValidateCareerReferencesAsync(
        IEnumerable<CareerRecommendation> recommendations)
    {
        var careerIds = recommendations
            .Select(recommendation =>
                recommendation.CareerProfileId)
            .Distinct()
            .ToList();

        if (careerIds.Any(id => id == Guid.Empty))
        {
            throw new InvalidOperationException(
                "Every recommendation must reference a career.");
        }

        var existingCareerIds = await _dbContext.CareerProfiles
            .Where(career => careerIds.Contains(career.Id))
            .Select(career => career.Id)
            .ToListAsync();

        var missingCareerIds = careerIds
            .Except(existingCareerIds)
            .ToList();

        if (missingCareerIds.Count > 0)
        {
            throw new InvalidOperationException(
                "One or more recommended careers are not present " +
                "in the persisted career catalogue.");
        }
    }
}