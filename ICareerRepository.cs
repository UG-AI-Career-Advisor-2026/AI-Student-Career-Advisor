using System.Collections.Generic;
using System.Threading.Tasks;
using CareerAdvisor.Core.Models;

namespace CareerAdvisor.Core.Interfaces;

/// <summary>
/// Defines a repository for accessing career catalog data.
/// </summary>
public interface ICareerRepository
{
    /// <summary>
    /// Retrieves all available careers from the catalog.
    /// </summary>
    Task<IEnumerable<Career>> GetCareersAsync();

    /// <summary>
    /// Retrieves a single career by its unique code.
    /// </summary>
    Task<Career?> GetCareerByCodeAsync(string code);
}