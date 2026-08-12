using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using CareerAdvisor.Core.Interfaces;
using CareerAdvisor.Core.Models;

namespace CareerAdvisor.Infrastructure.Persistence;

/// <summary>
/// An implementation of ICareerRepository that loads career data from a JSON file.
/// </summary>
public class JsonCareerRepository : ICareerRepository
{
    private readonly Lazy<IReadOnlyDictionary<string, Career>> _careers;

    public JsonCareerRepository(string jsonFilePath)
    {
        if (string.IsNullOrWhiteSpace(jsonFilePath))
        {
            throw new ArgumentException("JSON file path must be provided.", nameof(jsonFilePath));
        }

        _careers = new Lazy<IReadOnlyDictionary<string, Career>>(() =>
        {
            if (!File.Exists(jsonFilePath))
            {
                throw new FileNotFoundException("The career catalog file was not found.", jsonFilePath);
            }

            var jsonContent = File.ReadAllText(jsonFilePath);
            var careers = JsonSerializer.Deserialize<List<Career>>(jsonContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (careers is null || careers.Count == 0)
            {
                return new Dictionary<string, Career>();
            }

            return careers.ToDictionary(c => c.Code, StringComparer.OrdinalIgnoreCase);
        });
    }

    public Task<IEnumerable<Career>> GetCareersAsync()
    {
        return Task.FromResult(_careers.Value.Values.AsEnumerable());
    }

    public Task<Career?> GetCareerByCodeAsync(string code)
    {
        _careers.Value.TryGetValue(code, out var career);
        return Task.FromResult(career);
    }
}