using CareerAdvisor.Core.Interfaces;
using CareerAdvisor.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace CareerAdvisor.Infrastructure.Data;

public sealed class CareerCatalogSynchronizer
{
    private readonly CareerAdvisorDbContext _dbContext;
    private readonly ICareerRepository _careerRepository;

    public CareerCatalogSynchronizer(
        CareerAdvisorDbContext dbContext,
        ICareerRepository careerRepository)
    {
        _dbContext = dbContext;
        _careerRepository = careerRepository;
    }

    public async Task SynchronizeAsync(
        CancellationToken cancellationToken = default)
    {
        var catalogueCareers = (await _careerRepository.GetAllAsync())
            .ToList();

        foreach (var catalogueCareer in catalogueCareers)
        {
            var existingCareer = await _dbContext.CareerProfiles
                .SingleOrDefaultAsync(
                    career => career.Code == catalogueCareer.Code,
                    cancellationToken);

            if (existingCareer is null)
            {
                await _dbContext.CareerProfiles.AddAsync(
                    CreatePersistentCareer(catalogueCareer),
                    cancellationToken);

                continue;
            }

            if (existingCareer.Id != catalogueCareer.Id)
            {
                throw new InvalidDataException(
                    $"Career '{catalogueCareer.Code}' has database ID " +
                    $"'{existingCareer.Id}', but the stable catalogue ID is " +
                    $"'{catalogueCareer.Id}'.");
            }

            existingCareer.Title = catalogueCareer.Title;
            existingCareer.Description = catalogueCareer.Description;
            existingCareer.RequiredSkills =
                catalogueCareer.RequiredSkills.ToList();
            existingCareer.RecommendedCertifications =
                catalogueCareer.RecommendedCertifications.ToList();
            existingCareer.SuggestedLearningTopics =
                catalogueCareer.SuggestedLearningTopics.ToList();
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static CareerProfile CreatePersistentCareer(
        CareerProfile catalogueCareer)
    {
        return new CareerProfile
        {
            Id = catalogueCareer.Id,
            Code = catalogueCareer.Code,
            Title = catalogueCareer.Title,
            Description = catalogueCareer.Description,
            RequiredSkills =
                catalogueCareer.RequiredSkills.ToList(),
            RecommendedCertifications =
                catalogueCareer.RecommendedCertifications.ToList(),
            SuggestedLearningTopics =
                catalogueCareer.SuggestedLearningTopics.ToList()
        };
    }
}