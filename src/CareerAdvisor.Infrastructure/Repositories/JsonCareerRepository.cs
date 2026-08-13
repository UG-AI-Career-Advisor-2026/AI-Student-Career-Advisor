using System.Text.Json;
using CareerAdvisor.Core.Interfaces;
using CareerAdvisor.Core.Models;

namespace CareerAdvisor.Infrastructure.Repositories;

public sealed class JsonCareerRepository : ICareerRepository
{
    private readonly IReadOnlyList<CareerProfile> _careers;
    private readonly IReadOnlyDictionary<string, CareerProfile> _careersByCode;

    public JsonCareerRepository(string jsonFilePath)
    {
        if (string.IsNullOrWhiteSpace(jsonFilePath))
        {
            throw new ArgumentException(
                "Career catalogue path is required.",
                nameof(jsonFilePath));
        }

        if (!File.Exists(jsonFilePath))
        {
            throw new FileNotFoundException(
                "Career catalogue file was not found.",
                jsonFilePath);
        }

        var json = File.ReadAllText(jsonFilePath);

        var records = JsonSerializer.Deserialize<List<CareerCatalogRecord>>(
            json,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            })
            ?? throw new InvalidDataException(
                "Career catalogue could not be read.");

        _careers = records.Select(record => new CareerProfile
        {
            Code = record.Code,
            Title = record.Name,
            Description = record.Description,
            RequiredSkills = record.RequiredSkills,
            RecommendedCertifications = record.RecommendedCertifications,
            SuggestedLearningTopics = record.SuggestedLearningTopics
        }).ToList();

        if (_careers.Count == 0)
        {
            throw new InvalidDataException("Career catalogue is empty.");
        }

        if (_careers.Any(career =>
                string.IsNullOrWhiteSpace(career.Code)))
        {
            throw new InvalidDataException(
                "Every career must have a unique code.");
        }

        if (_careers
                .Select(career => career.Code)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count() != _careers.Count)
        {
            throw new InvalidDataException(
                "Career codes must be unique.");
        }

        _careersByCode = _careers.ToDictionary(
            career => career.Code,
            StringComparer.OrdinalIgnoreCase);
    }

    public Task<IEnumerable<CareerProfile>> GetAllAsync()
    {
        return Task.FromResult<IEnumerable<CareerProfile>>(_careers);
    }

    public Task<CareerProfile?> GetByIdAsync(Guid id)
    {
        return Task.FromResult(
            _careers.FirstOrDefault(career => career.Id == id));
    }

    public Task<CareerProfile?> GetByCodeAsync(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return Task.FromResult<CareerProfile?>(null);
        }

        _careersByCode.TryGetValue(code.Trim(), out var career);
        return Task.FromResult(career);
    }

    public Task AddAsync(CareerProfile entity)
    {
        throw new NotSupportedException(
            "The JSON career catalogue is read-only.");
    }

    public Task UpdateAsync(CareerProfile entity)
    {
        throw new NotSupportedException(
            "The JSON career catalogue is read-only.");
    }

    public Task DeleteAsync(Guid id)
    {
        throw new NotSupportedException(
            "The JSON career catalogue is read-only.");
    }

    private sealed class CareerCatalogRecord
    {
        public string Code { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public List<string> RequiredSkills { get; set; } = new();

        public List<string> RecommendedCertifications { get; set; } = new();

        public List<string> SuggestedLearningTopics { get; set; } = new();
    }
}