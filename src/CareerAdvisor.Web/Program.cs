using CareerAdvisor.Core.Interfaces;
using CareerAdvisor.Infrastructure.Data;
using CareerAdvisor.Infrastructure.Repositories;
using CareerAdvisor.Infrastructure.Services;
using CareerAdvisor.Web.Components;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var careerCatalogPath = Path.GetFullPath(
    Path.Combine(
        builder.Environment.ContentRootPath,
        "..",
        "..",
        "data",
        "career-catalog.json"));

builder.Services.AddSingleton<ICareerRepository>(
    _ => new JsonCareerRepository(careerCatalogPath));

builder.Services.AddScoped<ICareerService, CareerService>();
builder.Services.AddScoped<IAssessmentService, AssessmentService>();
builder.Services.AddScoped<ProtectedSessionStorage>();

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
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
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