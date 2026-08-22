# CareerIQ – AI-Powered Student Career Advisor

CareerIQ is a university semester project that helps students explore technology careers using their saved profile, skills, interests and career-assessment responses.

The project is built with .NET 10, Blazor, ASP.NET Core, Entity Framework Core, SQLite, ML.NET, xUnit and GitHub Actions.

## Development Status

CareerIQ currently supports the complete Sprint 3 journey:

1. Create and save a student profile.
2. Complete the 15-question career assessment.
3. Generate three ranked, model-backed career recommendations.
4. View each career's title, description, match score and explanation.
5. Reopen the latest saved recommendation session after refreshing the page.

Currently implemented:

- Create, save, reopen and edit a student profile
- Record interests, skills and proficiency levels
- Prevent duplicate interests and skills
- Complete a 15-question career assessment
- Preserve answers while navigating between assessment questions
- Reject incomplete assessments
- Save profiles and completed assessments in SQLite
- Load eight supported careers from the career catalogue
- Map saved profile and assessment data to the approved ML feature schema
- Load and validate the committed ML.NET model and its metadata
- Generate exactly three unique, ranked career recommendations
- Display percentage-style model match scores
- Generate explanations from the student's actual saved inputs
- Display an advisory-use disclaimer
- Persist recommendation sessions and reopen saved results
- Reject missing profiles and incomplete assessments
- Return an error instead of fabricated fallback recommendations
- Provide responsive desktop and mobile recommendation layouts

The eight supported careers are:

- Software Developer
- Data Analyst
- Cybersecurity Analyst
- Cloud Engineer
- Network Administrator
- Database Administrator
- UI/UX Designer
- AI/ML Engineer

## Setup from a Fresh Clone

### Requirements

Install the following:

- Git
- .NET 10 SDK
- A modern web browser

Confirm that .NET 10 is available:

```bash
dotnet --version
```

Clone and enter the repository:

```bash
git clone https://github.com/UG-AI-Career-Advisor-2026/AI-Student-Career-Advisor.git
cd AI-Student-Career-Advisor
```

Restore the repository tools and NuGet dependencies:

```bash
dotnet tool restore
dotnet restore CareerAdvisor.sln
```

Build and test the complete solution:

```bash
dotnet build CareerAdvisor.sln --configuration Release
dotnet test CareerAdvisor.sln --configuration Release
```

Run the application:

```bash
dotnet run \
  --project src/CareerAdvisor.Web/CareerAdvisor.Web.csproj
```

Open the local address displayed in the terminal.

Stop the application with `Ctrl+C`.

## Using CareerIQ

Complete the application journey in this order:

1. Open **Student Profile**.
2. Enter and save the student's profile, interests and skills.
3. Open **Career Assessment**.
4. Answer all 15 required questions.
5. Complete the assessment.
6. Open **Recommendations**.
7. Select **Generate recommendations**.
8. Review the three ranked recommendation cards.

Each recommendation card displays:

- Rank
- Career title
- Career description
- Percentage-style model match score
- Explanation based on profile and assessment inputs

The page also displays a disclaimer explaining that the recommendations are advisory.

Refreshing the page reopens the latest persisted recommendation session. Generating new results creates another saved recommendation session instead of inventing client-side results.

If the student has no saved profile or completed assessment, the page displays the required next action. If model prediction fails, CareerIQ displays an error and does not create fallback recommendations.

## Recommendation Model

The committed runtime model and metadata are stored at:

```text
data/models/career-recommendation-model.zip
data/models/career-recommendation-model.metadata.json
```

The model is included in the repository, so retraining is not required before running the application from a fresh clone.

CareerIQ uses ML.NET multiclass classification to score the eight supported careers. The saved metadata preserves the mapping between every score-vector position and its career label.

The runtime recommendation engine:

1. Loads the saved student profile.
2. Loads the latest completed assessment and its responses.
3. Maps those inputs to the approved 25-feature prediction schema.
4. Loads the committed ML.NET model and metadata.
5. Produces and validates scores for all eight career labels.
6. Converts the scores into percentage-style match values.
7. Selects the three highest unique careers.
8. Maps each model label to the correct career-catalogue entry.
9. Generates an input-based explanation.
10. Persists the session and its three recommendations in SQLite.

## Retraining the Model

The approved synthetic training dataset is stored at:

```text
data/training/sample-career-training-data.csv
```

From the repository root, retrain the model with:

```bash
dotnet run \
  --project tools/CareerAdvisor.ModelTrainer/CareerAdvisor.ModelTrainer.csproj
```

A successful training run updates:

```text
data/models/career-recommendation-model.zip
data/models/career-recommendation-model.metadata.json
```

The trainer validates the dataset, trains the model and prints evaluation metrics. The metadata records the dataset version, training date, trainer, random seed, record counts, evaluation metrics and score-label mapping.

After retraining, run the complete verification suite:

```bash
git diff --check
dotnet build CareerAdvisor.sln --configuration Release
dotnet test CareerAdvisor.sln --configuration Release
git status
```

Do not commit a newly trained model unless its dataset, metadata and test results have been reviewed.

See [docs/ML_APPROACH.md](docs/ML_APPROACH.md) for the complete feature mapping and training design.

## Database and Migrations

CareerIQ uses SQLite. Pending Entity Framework Core migrations are applied automatically when the application starts.

The default local database file is:

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

Local SQLite database files are ignored by Git using:

```text
*.db
*.db-shm
*.db-wal
```

## Testing

Run all automated tests:

```bash
dotnet test CareerAdvisor.sln --configuration Release
```

Run the Sprint 3 integration tests:

```bash
dotnet test CareerAdvisor.sln \
  --configuration Release \
  --filter "FullyQualifiedName~Sprint3IntegrationTests"
```

The Sprint 3 integration suite verifies:

- The complete profile → assessment → recommendation journey
- Loading and using the real committed ML.NET model
- Exactly three unique recommendations
- Valid model-label-to-career mapping
- Finite percentage-style match scores
- Recommendation persistence and reopening
- Missing-profile rejection
- Incomplete-assessment rejection
- Absence of fabricated fallback recommendations after model failure

## Model and Score Limitations

CareerIQ is an academic MVP and not a professional career-placement system.

The current training dataset contains 80 synthetic records, with 10 records for each supported career. It was created to demonstrate a reproducible ML.NET workflow and has not been validated using real student outcomes, labour-market evidence or professional career-advising research.

The displayed match values:

- Are derived from the model's relative scores across all eight supported careers
- Are converted into clear percentage-style values for comparison
- Are not calibrated probabilities of career success
- Do not measure employability or expected salary
- Do not guarantee admission, employment or professional performance
- Should not be interpreted as psychological or aptitude-test results
- May change when the profile, assessment, dataset or trained model changes

Only the three highest-ranked careers are displayed. Their displayed percentages do not need to add up to 100% because the normalization considers all eight supported careers.

The explanations are deterministic summaries based on selected profile and assessment features. They are not independent professional judgments and are not generated by a large language model.

Students should use the recommendations as starting points for exploration and combine them with academic advising, personal research and professional guidance.

## Other Current Limitations

- There are no user accounts or authentication.
- The application is intended for local development and academic demonstration.
- The single-user MVP uses the most recently updated student profile.
- The career catalogue is a static, read-only JSON file.
- Only eight technology careers are supported.
- Real-time labour-market information is not used.
- Skill-gap analysis is not yet implemented.
- Personalised learning roadmaps are not yet implemented.
- Recommendation sessions are persisted, but the dedicated history interface is not yet complete.
- Production cloud deployment, monitoring and security hardening are outside the current MVP.

## Planned Work

Remaining MVP work includes:

- Skill-gap analysis
- Personalised learning roadmaps
- Recommendation-history interface
- Final integration and quality review
- Demonstration and presentation preparation

## Contribution Workflow

Never work directly on `main`. Create one branch per issue and open a pull request.

Before opening a pull request, run:

```bash
git diff --check
dotnet build CareerAdvisor.sln --configuration Release
dotnet test CareerAdvisor.sln --configuration Release
git status
```

Pull requests must:

- Pass CI
- Receive at least one approval
- Use squash-and-merge
- Include `Closes #<issue-number>` in the pull-request description