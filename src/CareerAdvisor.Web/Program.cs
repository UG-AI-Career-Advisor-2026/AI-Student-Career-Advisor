using CareerAdvisor.Core.Interfaces;
using CareerAdvisor.Core.Validators;
using CareerAdvisor.Infrastructure.Data;
using CareerAdvisor.Infrastructure.MachineLearning;
using CareerAdvisor.Infrastructure.Repositories;
using CareerAdvisor.Infrastructure.Services;
using CareerAdvisor.Web.Components;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var repositoryRootPath = Path.GetFullPath(
    Path.Combine(
        builder.Environment.ContentRootPath,
        "..",
        ".."));

var careerCatalogPath = Path.Combine(
    repositoryRootPath,
    "data",
    "career-catalog.json");

var recommendationModelPath = Path.Combine(
    repositoryRootPath,
    "data",
    "models",
    "career-recommendation-model.zip");

var recommendationMetadataPath = Path.Combine(
    repositoryRootPath,
    "data",
    "models",
    "career-recommendation-model.metadata.json");

builder.Services.AddSingleton<ICareerRepository>(
    _ => new JsonCareerRepository(careerCatalogPath));

builder.Services.AddSingleton<RecommendationInputBuilder>();

builder.Services.AddSingleton<ICareerModelPredictor>(
    _ => new CareerModelPredictor(
        recommendationModelPath,
        recommendationMetadataPath));

builder.Services.AddScoped<ICareerService, CareerService>();
builder.Services.AddScoped<IAssessmentService, AssessmentService>();

builder.Services.AddScoped<
    IStudentProfileRepository,
    StudentProfileRepository>();

builder.Services.AddScoped<
    IRecommendationRepository,
    RecommendationRepository>();

builder.Services.AddScoped<
    IRecommendationService,
    RecommendationService>();

builder.Services.AddScoped<StudentProfileValidator>();
builder.Services.AddScoped<ProtectedSessionStorage>();
builder.Services.AddScoped<CareerCatalogSynchronizer>();

var connectionString = builder.Configuration.GetConnectionString(
        "CareerAdvisorDatabase")
    ?? throw new InvalidOperationException(
        "Connection string 'CareerAdvisorDatabase' was not found.");

builder.Services.AddDbContext<CareerAdvisorDbContext>(options =>
    options.UseSqlite(connectionString));

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider
        .GetRequiredService<CareerAdvisorDbContext>();

    await dbContext.Database.MigrateAsync();

    var catalogSynchronizer = scope.ServiceProvider
        .GetRequiredService<CareerCatalogSynchronizer>();

    await catalogSynchronizer.SynchronizeAsync();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler(
        "/Error",
        createScopeForErrors: true);

    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute(
    "/not-found",
    createScopeForStatusCodePages: true);

app.UseHttpsRedirection();
app.UseAntiforgery();

app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();