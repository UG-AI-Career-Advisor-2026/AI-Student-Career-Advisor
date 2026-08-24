using CareerAdvisor.Core.Interfaces;
using CareerAdvisor.Core.Models;
using CareerAdvisor.Infrastructure.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace CareerAdvisor.Infrastructure.Repositories;

/// <summary>
/// Persists and reopens complete profile-scoped learning-roadmap aggregates.
/// </summary>
public sealed class RoadmapRepository : IRoadmapRepository
{
    private const int SqliteConstraintError = 19;
    private const int SqliteUniqueConstraintError = 2067;
    private const string ProfileCareerConflictMessage =
        "UNIQUE constraint failed: " +
        "LearningRoadmaps.StudentProfileId, " +
        "LearningRoadmaps.CareerProfileId";

    private readonly CareerAdvisorDbContext _dbContext;

    public RoadmapRepository(CareerAdvisorDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<LearningRoadmap?> GetByIdForProfileAsync(
        Guid studentProfileId,
        Guid roadmapId)
    {
        return OrderSteps(await _dbContext.LearningRoadmaps
            .AsNoTracking()
            .Include(roadmap => roadmap.Steps)
            .SingleOrDefaultAsync(roadmap =>
                roadmap.Id == roadmapId &&
                roadmap.StudentProfileId == studentProfileId));
    }

    public async Task<LearningRoadmap?> GetByProfileAndCareerAsync(
        Guid studentProfileId,
        Guid careerProfileId)
    {
        return OrderSteps(await _dbContext.LearningRoadmaps
            .AsNoTracking()
            .Include(roadmap => roadmap.Steps)
            .SingleOrDefaultAsync(roadmap =>
                roadmap.StudentProfileId == studentProfileId &&
                roadmap.CareerProfileId == careerProfileId));
    }

    public async Task<bool> TryAddAsync(LearningRoadmap roadmap)
    {
        ArgumentNullException.ThrowIfNull(roadmap);

        await _dbContext.LearningRoadmaps.AddAsync(roadmap);

        try
        {
            await _dbContext.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateException exception)
            when (IsProfileCareerConflict(exception))
        {
            DetachFailedAggregate(roadmap);
            return false;
        }
    }

    public async Task<bool> SetStepCompletionAsync(
        Guid studentProfileId,
        Guid roadmapId,
        Guid stepId,
        bool isCompleted,
        DateTime? completedAt)
    {
        var scopedSteps = _dbContext.RoadmapSteps
            .Where(candidate =>
                candidate.Id == stepId &&
                candidate.LearningRoadmapId == roadmapId &&
                _dbContext.LearningRoadmaps.Any(roadmap =>
                    roadmap.Id == roadmapId &&
                    roadmap.StudentProfileId == studentProfileId));

        var changed = isCompleted
            ? await scopedSteps
                .Where(step => !step.IsCompleted)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(step => step.IsCompleted, true)
                    .SetProperty(step => step.CompletedAt, completedAt))
            : await scopedSteps
                .Where(step => step.IsCompleted)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(step => step.IsCompleted, false)
                    .SetProperty(
                        step => step.CompletedAt,
                        (DateTime?)null));

        if (changed == 1)
        {
            return true;
        }

        return isCompleted
            ? await scopedSteps.AnyAsync(step =>
                step.IsCompleted && step.CompletedAt != null)
            : await scopedSteps.AnyAsync(step =>
                !step.IsCompleted && step.CompletedAt == null);
    }

    private static LearningRoadmap? OrderSteps(LearningRoadmap? roadmap)
    {
        if (roadmap?.Steps is not null)
        {
            roadmap.Steps = roadmap.Steps
                .OrderBy(step => step.Order)
                .ThenBy(step => step.Id)
                .ToList();
        }

        return roadmap;
    }

    private static bool IsProfileCareerConflict(
        DbUpdateException exception)
    {
        return exception.InnerException is SqliteException
        {
            SqliteErrorCode: SqliteConstraintError,
            SqliteExtendedErrorCode: SqliteUniqueConstraintError
        } sqliteException &&
        sqliteException.Message.Contains(
            ProfileCareerConflictMessage,
            StringComparison.Ordinal);
    }

    private void DetachFailedAggregate(LearningRoadmap roadmap)
    {
        foreach (var step in roadmap.Steps.ToList())
        {
            _dbContext.Entry(step).State = EntityState.Detached;
        }

        _dbContext.Entry(roadmap).State = EntityState.Detached;
    }
}
