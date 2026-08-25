# CareerIQ – AI-Powered Student Career Advisor

CareerIQ is a university semester project that helps students explore technology careers using their saved profile, skills, interests and career-assessment responses.

The project is built with .NET 10, Blazor, ASP.NET Core, Entity Framework Core, SQLite, ML.NET, xUnit and GitHub Actions.

## Development Status

CareerIQ supports the complete Sprint 4 journey:

1. Create and save a student profile.
2. Complete the 15-question career assessment.
3. Generate three ranked, model-backed career recommendations.
4. View each career's title, description, match score and explanation.
5. Compare saved skills with a recommended career's catalogue requirements.
6. Generate or reopen a persisted learning roadmap.
7. Save roadmap-step progress and reopen it after restarting the application.
8. Review previous recommendation sessions and their original saved values.

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
- Compare every required career skill using deterministic matching rules
- Classify skill gaps as Missing, Needs Development or Matched
- Generate bounded, deterministic roadmaps only for persisted recommendations
- Persist roadmap steps and completion timestamps in SQLite
- Reopen roadmap progress after a page refresh or application restart
- Review recommendation history newest first and reopen saved session details
- Reject missing profiles and incomplete assessments
- Return an error instead of fabricated fallback recommendations
- Provide responsive desktop and mobile layouts for the complete journey

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
9. Open **Learning Roadmap**.
10. Select one of the three careers from the newest recommendation session.
11. Review the real Missing, Needs Development and Matched skill groups.
12. Select **Generate or reopen roadmap**.
13. Mark roadmap steps complete or incomplete. Progress is saved immediately.
14. Open **Recommendation History** to review saved sessions newest first.

Each recommendation card displays:

- Rank
- Career title
- Career description
- Percentage-style model match score
- Explanation based on profile and assessment inputs

The page also displays a disclaimer explaining that the recommendations are advisory.

Refreshing the page reopens the latest persisted recommendation session. When
generation produces the same ranked career IDs, exact scores and explanations,
CareerIQ reuses the newest identical session without writing another history
entry. Changed careers, rank, exact scores or explanations create a new saved
session. The page never invents client-side results.

A learning roadmap can only be generated for a career that appears in a
persisted recommendation session belonging to the selected saved profile. If a
roadmap already exists for that profile and career, CareerIQ reopens it without
regenerating its content or resetting progress. Roadmap steps are academic
guidance based on deterministic skill gaps and the approved career catalogue.

Recommendation History is read-only. It preserves the original three career
identities, scores and explanations saved with each session; it does not rerun
the model or recalculate historical values.

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
10. Reuses the newest identical saved session, or persists the changed session
    and its three recommendations in SQLite.

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

See [docs/ML_APPROACH.md](docs/ML_APPROACH.md) for the complete feature mapping
and training design, [docs/SKILL_GAP_MATCHING.md](docs/SKILL_GAP_MATCHING.md)
for deterministic skill matching, and
[docs/LEARNING_ROADMAP.md](docs/LEARNING_ROADMAP.md) for roadmap generation and
progress rules.

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

The automatic startup migration and the manual command are alternatives. Do
not run the manual command against a database unless that database is the
intended target. Automated tests use isolated disposable SQLite databases.

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

Run the Sprint 4 integration tests:

```bash
dotnet test CareerAdvisor.sln \
  --configuration Release \
  --filter "FullyQualifiedName~Sprint4IntegrationTests"
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

The Sprint 4 integration suite verifies the connected profile → assessment →
recommendation → skill-gap → persisted roadmap → progress → history journey
using the committed catalogue and model with a disposable migrated SQLite
database. It also verifies restart persistence, invalid prerequisites and
cross-profile isolation without returning fabricated fallback results.

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
- Skill proficiency is self-reported and does not prove professional readiness.
- `Intermediate` is an academic MVP comparison baseline, not evidence of
  employability, certification or professional competence.
- Certification steps are exploration suggestions, not mandatory employment
  requirements or enrolment recommendations.
- Completing a roadmap does not guarantee certification, employment or career
  success.
- Production cloud deployment, monitoring and security hardening are outside the current MVP.

## Planned Work

Post-MVP work may include authentication, production deployment hardening,
institutional integration and evidence-based expansion beyond the current
eight-career academic catalogue.

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
