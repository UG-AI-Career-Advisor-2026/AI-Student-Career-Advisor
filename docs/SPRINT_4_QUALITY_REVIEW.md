# Sprint 4 Integration and Quality Review

## Automated Coverage

The Sprint 4 integration suite exercises the connected production-service flow:

`profile → assessment → recommendations → skill gaps → roadmap → progress → history`

`Sprint4IntegrationTests` uses the committed JSON career catalogue, ML.NET model
and metadata. It applies the complete migration chain to a unique disposable
file-backed SQLite database, synchronizes the catalogue, and uses the production
repositories, services, validators and matching rules.

The three scenarios verify:

- the complete journey and persistence after rebuilding the service provider;
- all three skill-gap classifications and deterministic value results;
- recommendation authorization, deterministic unique roadmap steps and progress;
- exact preservation of recommendation identities, scores and reasoning;
- missing/unknown prerequisites without writes or fabricated fallbacks; and
- cross-profile roadmap and recommendation-history isolation.

Focused service, repository, validator and migration tests retain responsibility
for exhaustive malformed-data, alias, cap, concurrency and rollback boundaries.

## Disposable Database Safety

Integration tests never use `src/CareerAdvisor.Web/career-advisor.db`. Each test
creates a unique file under the operating system temporary directory with SQLite
pooling disabled. Providers, scopes and contexts are disposed, SQLite pools are
cleared, and only that test's temporary directory is removed.

`EnsureCreatedAsync` is not used by the Sprint 4 integration fixture. The fixture
uses `Database.MigrateAsync()` so it exercises the complete migration chain.

## Verification Commands

Restore tools and dependencies:

```bash
dotnet tool restore
dotnet restore CareerAdvisor.sln
```

List migrations without updating the normal application database:

```bash
dotnet ef migrations list \
  --project src/CareerAdvisor.Infrastructure \
  --startup-project src/CareerAdvisor.Web
```

Build and test:

```bash
dotnet build CareerAdvisor.sln --configuration Release
dotnet test CareerAdvisor.sln --configuration Release
dotnet test CareerAdvisor.sln \
  --configuration Release \
  --filter "FullyQualifiedName~Sprint4IntegrationTests"
```

The migration integration tests verify that the model has no pending changes and
that the full chain applies to disposable SQLite:

```bash
dotnet test CareerAdvisor.sln \
  --configuration Release \
  --filter "FullyQualifiedName~LearningRoadmapPersistenceTests"
```

## Fresh-Clone Verification

Use a disposable directory outside the active repository:

1. Clone the repository and check out the Sprint 4 integration branch.
2. Confirm the checkout is clean with `git status --short`.
3. Run `dotnet tool restore` and `dotnet restore CareerAdvisor.sln`.
4. Run the Release build and complete Release test suite.
5. List migrations with the command above.
6. Run the migration integration tests, which apply migrations only to temporary
   SQLite databases.
7. If the application is started, override its connection string to a database
   inside the disposable clone or temporary directory.
8. Stop the application and remove only the disposable clone directory.

Before the maintainer commits and pushes, a clean clone of the base with only the
approved uncommitted patch overlaid can validate the same commands. A genuine
remote-branch verification of the final revision remains pending until that
revision exists remotely.

## Manual Page and State Review

### Student Profile

- Loading, initial save, reopen, edit and validation states
- Empty, duplicate and long interest/skill values
- Keyboard access and visible focus for every control

### Career Assessment

- Missing-profile, in-progress, validation and completed states
- All 15 questions, answer preservation and progress semantics
- Keyboard operation of options and navigation controls

### Recommendations

- Loading, profile-required, assessment-required and ready states
- Generation success, preserved prior results after failure and generic errors
- Exactly three saved ranked careers, scores, explanations and one disclaimer
- No fallback recommendations after a model or persistence failure

### Learning Roadmap

- Loading, profile-required, recommendation-required and unavailable states
- Latest-session career selection and all three real skill-gap groups
- Generate/reopen action, ordered categories and intentionally absent links
- Completion, uncompletion, completed-roadmap and progress-error states
- Direct-route refresh and preserved progress after restart

### Recommendation History

- Loading, missing-profile, empty, list, detail, unavailable and error states
- Newest-first order and exactly three original saved recommendations
- Detail → list → another detail enhanced-navigation behavior
- Unknown and cross-profile sessions reveal no other profile's data

## Accessibility and Responsive Review

- Navigate every page using keyboard only and verify visible focus.
- Confirm one page heading, logical subheadings, native controls and landmarks.
- Confirm loading status is announced once and errors use scoped alerts.
- Confirm status regions announce concise changes rather than full result lists.
- Verify progress names, values, step action labels and UTC timestamps.
- Check that selection, error, category and completion do not rely on color alone.
- Check 200% zoom and widths near 1440px, 768px, 390px and 320px.
- Confirm long career, skill, topic and certification names wrap without
  horizontal page scrolling.

## Screenshot Checklist

Capture both desktop and mobile views of:

- three generated recommendation cards;
- roadmap career selection and skill-gap groups;
- an active persisted roadmap with partial progress;
- the completed-roadmap state;
- recommendation-history list; and
- recommendation-session details.

Do not include personal data, local paths, database details or raw error output.

## Supported Environment and Limitations

CareerIQ targets .NET 10 and uses Blazor Server interactivity, EF Core, SQLite,
ML.NET and a modern browser. CI verifies Linux; macOS and Windows should also be
checked manually for browser layout and SQLite file cleanup behavior.

The application is a single-user academic MVP without authentication. The most
recently updated profile is selected. Service-level profile scoping prevents
cross-profile reads but is not production authentication. Recommendations,
skill gaps and roadmaps are advisory and do not guarantee professional
readiness, certification, employability, employment or career success.
