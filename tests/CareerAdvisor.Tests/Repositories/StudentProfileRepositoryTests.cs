using CareerAdvisor.Core.Enums;
using CareerAdvisor.Core.Models;
using CareerAdvisor.Infrastructure.Data;
using CareerAdvisor.Infrastructure.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace CareerAdvisor.Tests.Repositories;

public class StudentProfileRepositoryTests
{
    [Fact]
    public async Task AddAsync_PersistsProfileWithInterestsAndSkills()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = CreateOptions(connection);
        var profile = CreateValidProfile();

        await using (var writeContext = new CareerAdvisorDbContext(options))
        {
            await writeContext.Database.EnsureCreatedAsync();

            var repository = new StudentProfileRepository(writeContext);
            await repository.AddAsync(profile);
        }

        await using var readContext = new CareerAdvisorDbContext(options);
        var readRepository = new StudentProfileRepository(readContext);

        var savedProfile = await readRepository.GetByIdAsync(profile.Id);

        Assert.NotNull(savedProfile);
        Assert.Equal("Ama Mensah", savedProfile.Name);
        Assert.Equal("Computer Science", savedProfile.Programme);
        Assert.Equal(AcademicLevel.Undergraduate, savedProfile.AcademicLevel);
        Assert.Equal(2, savedProfile.Interests.Count);
        Assert.Equal(2, savedProfile.Skills.Count);
        Assert.All(
            savedProfile.Skills,
            skill => Assert.Equal(profile.Id, skill.StudentProfileId));
    }

    [Fact]
    public async Task UpdateAsync_UpdatesProfileAndReconcilesSkills()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = CreateOptions(connection);
        var profile = CreateValidProfile();

        await using (var createContext = new CareerAdvisorDbContext(options))
        {
            await createContext.Database.EnsureCreatedAsync();

            var repository = new StudentProfileRepository(createContext);
            await repository.AddAsync(profile);
        }

        await using (var updateContext = new CareerAdvisorDbContext(options))
        {
            var repository = new StudentProfileRepository(updateContext);
            var profileToUpdate =
                await repository.GetByIdAsync(profile.Id);

            Assert.NotNull(profileToUpdate);

            profileToUpdate.Name = "Ama Nyarko";
            profileToUpdate.Programme = "Information Technology";
            profileToUpdate.Interests = ["Cloud", "Cybersecurity"];

            var retainedSkill = profileToUpdate.Skills
                .Single(skill => skill.SkillName == "C#");

            retainedSkill.SkillName = "ASP.NET Core";
            retainedSkill.Proficiency = SkillProficiency.Advanced;

            var removedSkill = profileToUpdate.Skills
                .Single(skill => skill.SkillName == "SQL");

            profileToUpdate.Skills.Remove(removedSkill);
            profileToUpdate.Skills.Add(new StudentSkill
            {
                SkillName = "Azure",
                Proficiency = SkillProficiency.Beginner
            });

            await repository.UpdateAsync(profileToUpdate);
        }

        await using var readContext = new CareerAdvisorDbContext(options);
        var readRepository = new StudentProfileRepository(readContext);

        var updatedProfile =
            await readRepository.GetByIdAsync(profile.Id);

        Assert.NotNull(updatedProfile);
        Assert.Equal("Ama Nyarko", updatedProfile.Name);
        Assert.Equal(
            "Information Technology",
            updatedProfile.Programme);
        Assert.Equal(
            ["Cloud", "Cybersecurity"],
            updatedProfile.Interests);
        Assert.Equal(2, updatedProfile.Skills.Count);
        Assert.Contains(
            updatedProfile.Skills,
            skill =>
                skill.SkillName == "ASP.NET Core" &&
                skill.Proficiency == SkillProficiency.Advanced);
        Assert.Contains(
            updatedProfile.Skills,
            skill => skill.SkillName == "Azure");
        Assert.DoesNotContain(
            updatedProfile.Skills,
            skill => skill.SkillName == "SQL");
    }

    [Fact]
    public async Task GetAllAsync_ReturnsProfilesWithSkills()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = CreateOptions(connection);

        await using var context = new CareerAdvisorDbContext(options);
        await context.Database.EnsureCreatedAsync();

        var repository = new StudentProfileRepository(context);
        await repository.AddAsync(CreateValidProfile());

        var profiles = (await repository.GetAllAsync()).ToList();

        var savedProfile = Assert.Single(profiles);
        Assert.Equal(2, savedProfile.Skills.Count);
    }

    [Fact]
    public async Task DeleteAsync_RemovesProfileAndItsSkills()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = CreateOptions(connection);
        var profile = CreateValidProfile();

        await using (var createContext = new CareerAdvisorDbContext(options))
        {
            await createContext.Database.EnsureCreatedAsync();

            var repository = new StudentProfileRepository(createContext);
            await repository.AddAsync(profile);
        }

        await using (var deleteContext = new CareerAdvisorDbContext(options))
        {
            var repository = new StudentProfileRepository(deleteContext);
            await repository.DeleteAsync(profile.Id);
        }

        await using var readContext = new CareerAdvisorDbContext(options);

        Assert.Null(
            await readContext.StudentProfiles.FindAsync(profile.Id));
        Assert.Empty(await readContext.StudentSkills.ToListAsync());
    }

    private static DbContextOptions<CareerAdvisorDbContext> CreateOptions(
        SqliteConnection connection)
    {
        return new DbContextOptionsBuilder<CareerAdvisorDbContext>()
            .UseSqlite(connection)
            .Options;
    }

    private static StudentProfile CreateValidProfile()
    {
        return new StudentProfile
        {
            Name = "Ama Mensah",
            Programme = "Computer Science",
            AcademicLevel = AcademicLevel.Undergraduate,
            Interests = ["Artificial Intelligence", "Software Development"],
            Skills =
            [
                new StudentSkill
                {
                    SkillName = "C#",
                    Proficiency = SkillProficiency.Intermediate
                },
                new StudentSkill
                {
                    SkillName = "SQL",
                    Proficiency = SkillProficiency.Beginner
                }
            ]
        };
    }
}