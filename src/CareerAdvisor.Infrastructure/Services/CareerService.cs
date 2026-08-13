using CareerAdvisor.Core.Interfaces;
using CareerAdvisor.Core.Models;

namespace CareerAdvisor.Infrastructure.Services;

public sealed class CareerService : ICareerService
{
    private readonly ICareerRepository _repository;

    public CareerService(ICareerRepository repository)
    {
        _repository = repository;
    }

    public Task<IEnumerable<CareerProfile>> GetAllCareersAsync()
    {
        return _repository.GetAllAsync();
    }

    public Task<CareerProfile?> GetCareerByIdAsync(Guid id)
    {
        return _repository.GetByIdAsync(id);
    }

    public Task<CareerProfile?> GetCareerByCodeAsync(string code)
    {
        return _repository.GetByCodeAsync(code);
    }
}