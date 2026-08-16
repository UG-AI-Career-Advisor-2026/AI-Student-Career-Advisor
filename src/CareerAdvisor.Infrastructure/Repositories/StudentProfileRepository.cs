using CareerAdvisor.Core.Interfaces;
using CareerAdvisor.Core.Models;
using CareerAdvisor.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CareerAdvisor.Infrastructure.Repositories;

public sealed class StudentProfileRepository : IStudentProfileRepository
{
    private readonly CareerAdvisorDbContext _dbContext;

    public StudentProfileRepository(CareerAdvisorDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<StudentProfile?> GetByIdAsync(Guid id)
    {
        return await _dbContext.StudentProfiles
            .AsNoTracking()
            .Include(profile => profile.Skills)
            .SingleOrDefaultAsync(profile => profile.Id == id);
    }

    public async Task<IEnumerable<StudentProfile>> GetAllAsync()
    {
        return await _dbContext.StudentProfiles
            .AsNoTracking()
            .Include(profile => profile.Skills)
            .OrderByDescending(profile => profile.UpdatedAt)
            .ToListAsync();
    }

    public async Task AddAsync(StudentProfile entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        if (entity.Id == Guid.Empty)
        {
            entity.Id = Guid.NewGuid();
        }

        var now = DateTime.UtcNow;
        entity.CreatedAt = now;
        entity.UpdatedAt = now;

        foreach (var skill in entity.Skills)
        {
            if (skill.Id == Guid.Empty)
            {
                skill.Id = Guid.NewGuid();
            }

            skill.StudentProfileId = entity.Id;
        }

        await _dbContext.StudentProfiles.AddAsync(entity);
        await _dbContext.SaveChangesAsync();
    }

    public async Task UpdateAsync(StudentProfile entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        var existingProfile = await _dbContext.StudentProfiles
            .Include(profile => profile.Skills)
            .SingleOrDefaultAsync(profile => profile.Id == entity.Id);

        if (existingProfile is null)
        {
            throw new KeyNotFoundException(
                $"Student profile '{entity.Id}' was not found.");
        }

        existingProfile.Name = entity.Name;
        existingProfile.Programme = entity.Programme;
        existingProfile.AcademicLevel = entity.AcademicLevel;
        existingProfile.Interests = entity.Interests.ToList();
        existingProfile.UpdatedAt = DateTime.UtcNow;

        var incomingSkillIds = entity.Skills
            .Select(skill => skill.Id)
            .ToHashSet();

        var removedSkills = existingProfile.Skills
            .Where(skill => !incomingSkillIds.Contains(skill.Id))
            .ToList();

        _dbContext.StudentSkills.RemoveRange(removedSkills);

        foreach (var incomingSkill in entity.Skills)
        {
            var existingSkill = existingProfile.Skills
                .SingleOrDefault(skill => skill.Id == incomingSkill.Id);

            if (existingSkill is not null)
            {
                existingSkill.SkillName = incomingSkill.SkillName;
                existingSkill.Proficiency = incomingSkill.Proficiency;
                continue;
            }

            var newSkill = new StudentSkill
{
    Id = incomingSkill.Id == Guid.Empty
        ? Guid.NewGuid()
        : incomingSkill.Id,
    SkillName = incomingSkill.SkillName,
    Proficiency = incomingSkill.Proficiency,
    StudentProfileId = existingProfile.Id
};

existingProfile.Skills.Add(newSkill);

// Explicitly mark this disconnected skill for insertion.
// Its pre-generated GUID would otherwise make it look existing.
_dbContext.StudentSkills.Add(newSkill);
        }

        await _dbContext.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var profile = await _dbContext.StudentProfiles
            .SingleOrDefaultAsync(profile => profile.Id == id);

        if (profile is null)
        {
            return;
        }

        _dbContext.StudentProfiles.Remove(profile);
        await _dbContext.SaveChangesAsync();
    }
}