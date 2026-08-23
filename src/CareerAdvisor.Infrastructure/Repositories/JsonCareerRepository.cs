using System.Text.Json;
using CareerAdvisor.Core.Careers;
using CareerAdvisor.Core.Interfaces;
using CareerAdvisor.Core.Models;
using CareerAdvisor.Core.SkillGaps;

namespace CareerAdvisor.Infrastructure.Repositories;

public sealed class JsonCareerRepository : ICareerRepository
{
    private readonly IReadOnlyList<CareerProfile> _careers;
    private readonly IReadOnlyDictionary<string, CareerProfile>
        _careersByCode;

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

        ValidateRecords(records);

        _careers = records
            .Select(record =>
            {
                var code = record.Code.Trim();

                return new CareerProfile
                {
                    Id = CareerCatalogIdentity.GetId(code),
                    Code = code,
                    Title = record.Name.Trim(),
                    Description = record.Description.Trim(),
                    RequiredSkills = [.. record.RequiredSkills],
                    RecommendedCertifications =
                        [.. record.RecommendedCertifications],
                    SuggestedLearningTopics =
                        [.. record.SuggestedLearningTopics]
                };
            })
            .ToList();

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

    private static void ValidateRecords(
        IReadOnlyCollection<CareerCatalogRecord> records)
    {
        if (records.Count == 0)
        {
            throw new InvalidDataException(
                "Career catalogue is empty.");
        }

        var codes = records
            .Select(record => record.Code?.Trim() ?? string.Empty)
            .ToList();

        if (codes.Any(string.IsNullOrWhiteSpace))
        {
            throw new InvalidDataException(
                "Every career must have a unique code.");
        }

        if (codes
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count() != codes.Count)
        {
            throw new InvalidDataException(
                "Career codes must be unique.");
        }

        var unknownCode = codes.FirstOrDefault(
            code => !CareerCatalogIdentity.TryGetId(code, out _));

        if (unknownCode is not null)
        {
            throw new InvalidDataException(
                $"Career code '{unknownCode}' has no stable identity.");
        }

        var missingCodes = CareerCatalogIdentity.IdsByCode.Keys
            .Except(codes, StringComparer.OrdinalIgnoreCase)
            .OrderBy(code => code)
            .ToList();

        if (missingCodes.Count > 0)
        {
            throw new InvalidDataException(
                $"Career catalogue is missing supported codes: " +
                $"{string.Join(", ", missingCodes)}.");
        }

        foreach (var record in records)
        {
            ValidateRequiredSkills(record);
        }
    }

    private static void ValidateRequiredSkills(
        CareerCatalogRecord record)
    {
        if (record.RequiredSkills is null ||
            record.RequiredSkills.Count < 6)
        {
            throw new InvalidDataException(
                $"Career '{record.Code}' must define at least six " +
                "required skills.");
        }

        if (record.RequiredSkills.Any(skill =>
                string.IsNullOrWhiteSpace(skill) ||
                !string.Equals(
                    skill,
                    skill.Trim(),
                    StringComparison.Ordinal)))
        {
            throw new InvalidDataException(
                $"Career '{record.Code}' contains a blank or untrimmed " +
                "required skill.");
        }

        var normalizedSkills = record.RequiredSkills
            .Select(SkillNameNormalizer.Normalize)
            .ToList();

        if (normalizedSkills
                .Distinct(StringComparer.Ordinal)
                .Count() != normalizedSkills.Count)
        {
            throw new InvalidDataException(
                $"Career '{record.Code}' contains duplicate normalized " +
                "required skills.");
        }
    }

    private sealed class CareerCatalogRecord
    {
        public string Code { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public List<string> RequiredSkills { get; set; } = new();

        public List<string> RecommendedCertifications { get; set; } =
            new();

        public List<string> SuggestedLearningTopics { get; set; } =
            new();
    }
}
