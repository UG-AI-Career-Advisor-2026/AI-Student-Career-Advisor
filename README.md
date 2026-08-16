# CareerIQ – AI-Powered Student Career Advisor

CareerIQ is a university semester project that helps students explore technology careers using their profile, skills, interests and career-assessment responses.

The project is built with .NET 10, Blazor, EF Core, SQLite, ML.NET, xUnit and GitHub Actions.

## Development Status

CareerIQ is still under active development. Sprint 2 provides the student-profile and career-assessment foundation; it does not complete the full MVP.

Currently implemented:

- Create, save, reopen and edit a student profile
- Record interests, skills and proficiency levels
- Prevent duplicate interests and skills
- Complete a 15-question career assessment
- Preserve answers while navigating between questions
- Reject incomplete assessments
- Save completed assessments in SQLite
- Load all eight supported careers from the career catalogue

The eight careers are Software Developer, Data Analyst, Cybersecurity Analyst, Cloud Engineer, Network Administrator, Database Administrator, UI/UX Designer and AI/ML Engineer.

## Setup

### Requirements

- Git
- .NET 10 SDK
- A modern web browser

Clone and enter the repository:

```bash
git clone https://github.com/UG-AI-Career-Advisor-2026/AI-Student-Career-Advisor.git
cd AI-Student-Career-Advisor
```

Restore tools and dependencies:

```bash
dotnet tool restore
dotnet restore CareerAdvisor.sln
```

Build and test the solution:

```bash
dotnet build CareerAdvisor.sln --configuration Release
dotnet test CareerAdvisor.sln --configuration Release
```

Run the application:

```bash
dotnet run --project src/CareerAdvisor.Web/CareerAdvisor.Web.csproj
```

Open the local address displayed in the terminal. Create a student profile before starting an assessment.

Stop the application with `Ctrl+C`.

## Database and Migrations

CareerIQ uses SQLite. The local database is created automatically when the application starts because pending EF Core migrations are applied during startup.

The default database file is:

```text
src/CareerAdvisor.Web/career-advisor.db
```

List available migrations:

```bash
dotnet ef migrations list \
  --project src/CareerAdvisor.Infrastructure \
  --startup-project src/CareerAdvisor.Web
```

Apply migrations manually:

```bash
dotnet ef database update \
  --project src/CareerAdvisor.Infrastructure \
  --startup-project src/CareerAdvisor.Web
```

SQLite database files are ignored by Git using:

```text
*.db
*.db-shm
*.db-wal
```

## Current Limitations

- There are no user accounts or authentication.
- The application is intended for local development and demonstration.
- The career catalogue is a static, read-only JSON file.
- The assessment uses the most recently updated student profile.
- Recommendation generation and ML.NET prediction are not yet integrated.
- No recommendations or confidence scores are fabricated.
- Skill-gap analysis, learning roadmaps and recommendation history are not yet implemented.
- The sample ML.NET dataset is synthetic and is not suitable for real-world career decisions.

## Planned Work

The remaining project work includes:

- Recommendation generation and ML.NET integration
- Exactly three ranked recommendations
- Confidence scores and plain-language explanations
- Skill-gap analysis
- Personalised learning roadmaps
- Recommendation history
- Final integration, testing, demonstration and presentation preparation

Detailed GitHub issues for the next sprint will be created after the Sprint 2 integration review.

## Contribution Workflow

Never work directly on `main`. Create one branch per issue and open a pull request.

Before opening a pull request, run:

```bash
git diff --check
dotnet build CareerAdvisor.sln --configuration Release
dotnet test CareerAdvisor.sln --configuration Release
git status
```

Pull requests must pass CI, receive at least one approval and use squash-and-merge. Include `Closes #<issue-number>` in the pull-request description.