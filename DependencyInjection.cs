using System.IO;
using CareerAdvisor.Core.Interfaces;
using CareerAdvisor.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace CareerAdvisor.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IWebHostEnvironment environment)
    {
        // Register the JsonCareerRepository.
        // It's configured as a singleton because it loads data from a file once and caches it.
        services.AddSingleton<ICareerRepository>(provider =>
        {
            // Resolve the path to data/career-catalog.json relative to the web root.
            var jsonFilePath = Path.Combine(environment.ContentRootPath, "..", "data", "career-catalog.json");
            return new JsonCareerRepository(Path.GetFullPath(jsonFilePath));
        });

        // Other infrastructure services can be registered here in the future.

        return services;
    }
}