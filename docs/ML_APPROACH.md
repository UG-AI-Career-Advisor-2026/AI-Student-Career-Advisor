# CareerIQ Recommendation Feature and ML Approach

## Purpose

CareerIQ uses ML.NET multiclass classification to rank eight supported technology careers using a student's saved profile and completed career assessment.

This document defines:

- The stable recommendation feature contract
- The synthetic training dataset
- The reproducible ML.NET training workflow
- Runtime model loading and validation
- Career-label mapping
- Recommendation ranking and persistence
- The limitations of the model and displayed match scores

## Current Status

The Sprint 3 recommendation workflow is implemented.

CareerIQ can now:

1. Load a saved student profile.
2. Load the student's latest completed 15-question assessment.
3. Map the profile and responses to the approved prediction schema.
4. Load the committed ML.NET model and metadata.
5. Produce and validate scores for all eight supported careers.
6. Select exactly three unique careers in descending score order.
7. Map each model label to its correct career-catalogue entry.
8. Generate a plain-language explanation from actual student inputs.
9. Persist the recommendation session and its three recommendations.
10. Reopen saved results after the application or page is restarted.

The recommendations interface also displays an advisory-use disclaimer and does not create fabricated fallback recommendations when generation fails.

## Runtime Model Artifacts

The committed runtime model is stored at:

```text
data/models/career-recommendation-model.zip
```

Its metadata is stored at:

```text
data/models/career-recommendation-model.metadata.json
```

Both files are tracked in the repository. A fresh clone can therefore build, test and run recommendations without retraining the model.

The metadata records:

- Dataset version
- Training date in UTC
- Trainer name
- Random seed
- Total record count
- Training record count
- Test record count
- Micro-accuracy
- Macro-accuracy
- Log-loss
- Score-vector position to career-label mapping

The current committed artifact was trained with:

- Dataset version `1.0.0-synthetic-80`
- 80 total records
- 68 training records
- 12 test records
- Random seed `42`

## Supported Career Labels

| Career catalogue code | ML label |
|---|---|
| `SD-001` | Software Developer |
| `DA-002` | Data Analyst |
| `CS-003` | Cybersecurity Analyst |
| `CE-004` | Cloud Engineer |
| `NA-005` | Network Administrator |
| `DBA-006` | Database Administrator |
| `UX-007` | UI/UX Designer |
| `AI-008` | AI/ML Engineer |

These mappings must remain stable because every ML output label must resolve to an existing entry in `data/career-catalog.json`.

The runtime engine rejects predictions that:

- Omit an approved label
- Include an unknown label
- Include a duplicate label
- Produce a career label that cannot be mapped to the catalogue

## ML Input Columns

The training dataset contains 26 columns: 25 prediction inputs and one output label.

### Profile columns

- `AcademicBackground`
- `AcademicLevel`
- `ProgrammingSkill`
- `DataSkill`
- `CybersecuritySkill`
- `CloudSkill`
- `NetworkingSkill`
- `DatabaseSkill`
- `DesignSkill`
- `AISkill`

### Assessment columns

- `TechnologyInterest`
- `DataInterest`
- `DesignInterest`
- `LeadershipInterest`
- `SocialImpactInterest`
- `ProgrammingSelfAssessment`
- `CommunicationSelfAssessment`
- `ProblemSolvingSelfAssessment`
- `CollaborationSelfAssessment`
- `LearningAgility`
- `PreferredEnvironment`
- `PreferredPace`
- `StabilityPreference`
- `CompensationPreference`
- `IndustryPreference`

### Output column

- `CareerLabel`

## Student Profile Mapping

The student's name, database identifiers and timestamps are not ML features.

| Profile information | ML mapping |
|---|---|
| Programme | `AcademicBackground` |
| Academic level | `AcademicLevel` |
| Interests and programming-related skills | `ProgrammingSkill` |
| Interests and data-related skills | `DataSkill` |
| Interests and security-related skills | `CybersecuritySkill` |
| Interests and cloud-related skills | `CloudSkill` |
| Interests and networking-related skills | `NetworkingSkill` |
| Interests and database-related skills | `DatabaseSkill` |
| Interests and design-related skills | `DesignSkill` |
| Interests and AI/ML-related skills | `AISkill` |

Programme values are trimmed and normalized before being used as categorical values. Academic level uses the corresponding `AcademicLevel` enum name.

Interest and skill text is matched case-insensitively using whole words or recognized phrases from `RecommendationFeatureSchema.ProfileDomainKeywordsByColumn`. Arbitrary substring matching is not used.

### Profile-domain scoring

Each profile-domain column uses the documented 1–5 scale:

| Evidence | Numeric value |
|---|---:|
| No matching interest or skill | 1 |
| Matching skill at Beginner proficiency | 2 |
| Matching interest without a matching skill | 3 |
| Matching skill at Intermediate proficiency | 3 |
| Matching skill at Advanced proficiency | 4 |
| Matching skill at Expert proficiency | 5 |

When an interest and skill both match the same domain, the higher applicable value is used.

## Assessment Question Mapping

Every assessment question maps to exactly one ML input column.

| Question code | ML column | Value type |
|---|---|---|
| `Q1_INT_TECH` | `TechnologyInterest` | Numeric |
| `Q2_INT_DATA` | `DataInterest` | Numeric |
| `Q3_INT_CREA` | `DesignInterest` | Numeric |
| `Q4_INT_MGMT` | `LeadershipInterest` | Numeric |
| `Q5_INT_SOCIAL` | `SocialImpactInterest` | Numeric |
| `Q6_SKILL_PROG` | `ProgrammingSelfAssessment` | Numeric |
| `Q7_SKILL_COMM` | `CommunicationSelfAssessment` | Numeric |
| `Q8_SKILL_PROB` | `ProblemSolvingSelfAssessment` | Numeric |
| `Q9_SKILL_COLL` | `CollaborationSelfAssessment` | Numeric |
| `Q10_SKILL_LEARN` | `LearningAgility` | Numeric |
| `Q11_WORK_ENV` | `PreferredEnvironment` | Categorical |
| `Q12_WORK_PACE` | `PreferredPace` | Categorical |
| `Q13_WORK_STABIL` | `StabilityPreference` | Categorical |
| `Q14_WORK_COMP` | `CompensationPreference` | Categorical |
| `Q15_WORK_INDUS` | `IndustryPreference` | Categorical |

Recommendation generation requires a completed assessment containing valid responses to all 15 questions. Missing, duplicated or unknown responses are rejected by the feature builder.

## Numeric Assessment Values

The first ten assessment questions use numeric values between 1 and 5.

| Questions | Option A | Option B | Option C | Option D |
|---|---:|---:|---:|---:|
| Q1–Q5 | 5 | 4 | 3 | 1 |
| Q6–Q9 | 5 | 4 | 2 | 1 |
| Q10 | 5 | 4 | 3 | 2 |

Option identifiers follow the stable format `Q<number>_OPT_<letter>`, such as `Q1_OPT_A`.

## Categorical Assessment Values

| Question | A | B | C | D |
|---|---|---|---|---|
| Q11 | `RemoteHybrid` | `OfficeBased` | `Flexible` | `NoPreference` |
| Q12 | `Fast` | `Moderate` | `Flexible` | `Slow` |
| Q13 | `Growth` | `Stability` | `Balanced` | `Situational` |
| Q14 | `SalaryBenefits` | `SalaryEquity` | `FreelanceContract` | `NoPreference` |
| Q15 | `Technology` | `Finance` | `Healthcare` | `Other` |

## Synthetic Training Dataset

The approved training data is stored at:

```text
data/training/sample-career-training-data.csv
```

It contains 80 synthetic records with exactly 10 records for each supported career.

The dataset exists to demonstrate:

- A stable feature schema
- Dataset validation
- Reproducible model training
- Model evaluation
- Saved-model loading
- Runtime recommendation integration

It is not a collection of real student histories or verified career outcomes.

## Training Pipeline

The pipeline uses:

- ML.NET 5.0
- `SdcaMaximumEntropy` multiclass classification
- Random seed `42`
- A requested 20% randomized test fraction
- One-hot encoding for categorical inputs
- Concatenation of numeric and encoded inputs
- Min-max feature normalization

The trainer validates:

- Required columns
- Record count
- Numeric ranges
- Approved categorical values
- Recognized career labels
- Balanced representation of all eight careers

ML.NET's randomized train-test assignment does not guarantee that the resulting record counts equal an exact mathematical percentage. With the current seed and 80-record dataset, the committed model used 68 training records and 12 test records.

## Runtime Recommendation Pipeline

At application startup, CareerIQ registers the committed model and metadata with the recommendation engine.

When a student generates recommendations:

1. `RecommendationService` verifies that the requested saved profile exists.
2. `AssessmentService` loads the latest completed assessment and all responses.
3. `RecommendationInputBuilder` maps the profile and assessment to `CareerTrainingInput`.
4. `CareerModelPredictor` loads the saved model and validates its metadata.
5. The predictor pairs every model score with the corresponding label stored in the metadata.
6. The recommendation service validates that all eight approved labels are present exactly once.
7. The service rejects non-finite, negative or unusable score vectors.
8. Scores are normalized across all eight supported careers.
9. The top three unique careers are selected in descending match-score order.
10. Each model label is mapped to the corresponding career-catalogue code.
11. Explanations are generated from the strongest applicable profile and assessment features.
12. The recommendation session and its three recommendations are persisted in SQLite.
13. The saved session is reopened before being returned to the interface.

## Score-Vector Mapping

ML.NET returns one score for each career label. The position of a score in the output vector must not be assumed.

The metadata file preserves the label corresponding to each score-vector position. `CareerModelPredictor` uses this stored mapping to return explicit label-and-score pairs.

Runtime consumers must use this metadata-defined mapping rather than maintaining a separate assumed label order.

## Match-Score Interpretation

The recommendation service validates the eight raw model scores and divides each score by the sum of all eight scores. The resulting values are rounded to four decimal places and stored between 0 and 1.

The interface displays these values in percentage-style form to help students compare the supported careers.

These values:

- Represent relative model alignment within CareerIQ's eight-career catalogue
- Are not calibrated probabilities of career success
- Are not employability, salary or admission predictions
- Are not professional aptitude or psychological-test results
- Must not be presented as guarantees

Only the top three careers are displayed. Their displayed percentages do not need to total 100% because normalization includes all eight model scores.

## Explanations and Disclaimer

Each recommendation explanation uses actual mapped profile and assessment features.

The explanation engine selects relevant evidence for the recommended career, such as:

- Technology or data interest
- Programming or design skill
- Communication or collaboration self-assessment
- Problem-solving ability
- Learning agility

The explanations are deterministic templates. They are not generated by a large language model and should not be interpreted as independent professional judgments.

Every explanation includes the advisory disclaimer defined by `RecommendationDisclaimer.Text`. The recommendations interface also displays the disclaimer separately.

## Failure and Fallback Behaviour

CareerIQ does not invent fallback recommendations.

Recommendation generation fails clearly when:

- The profile identifier is empty
- The saved profile cannot be found
- No completed assessment exists
- The assessment does not contain all 15 valid responses
- The model or metadata file is missing
- The metadata does not contain exactly the approved labels
- Prediction scores are missing, duplicated, non-finite, negative or unusable
- A model label cannot be mapped to the career catalogue
- Exactly three unique recommendations cannot be created
- Persistence or reopening fails

A failed prediction does not create a recommendation session or placeholder career results.

## Retraining the Model

From the repository root, run:

```bash
dotnet run \
  --project tools/CareerAdvisor.ModelTrainer/CareerAdvisor.ModelTrainer.csproj
```

A successful run updates:

```text
data/models/career-recommendation-model.zip
data/models/career-recommendation-model.metadata.json
```

After retraining:

1. Review the printed evaluation metrics.
2. Confirm the metadata contains all eight approved labels.
3. Confirm the dataset version and record counts.
4. Run the model and integration tests.
5. Review the Git diff before committing the artifacts.

Do not commit a newly trained model without its matching metadata.

## Evaluation Limitations

The current dataset is small, balanced and synthetic. Its career labels were deliberately constructed around recognizable feature patterns.

As a result:

- Very high test accuracy is possible.
- The test partition is small.
- Metrics may be unstable when records or the random seed change.
- Results do not demonstrate generalization to real students.
- Results do not establish production readiness.
- Results do not establish professional or scientific validity.
- The model has not been externally validated.
- The model has not been evaluated for demographic fairness.
- The model does not use Ghanaian labour-market outcomes.
- The model does not account for changing employer demand.
- The displayed match scores are not calibrated confidence probabilities.

Micro-accuracy, macro-accuracy and log-loss demonstrate that the training pipeline executes and can evaluate a held-out synthetic partition. They must not be used as evidence that CareerIQ can guarantee suitable careers or employment outcomes.

Before any real-world use, the project would require ethically collected real data, consent and privacy controls, broader validation, bias testing, calibration, monitoring and review by qualified career professionals.

## Verification

Run model-training tests:

```bash
dotnet test CareerAdvisor.sln \
  --configuration Release \
  --filter "FullyQualifiedName~CareerModelTrainerTests"
```

Run saved-model loading tests:

```bash
dotnet test CareerAdvisor.sln \
  --configuration Release \
  --filter "FullyQualifiedName~CareerModelPredictorTests"
```

Run feature-mapping tests:

```bash
dotnet test CareerAdvisor.sln \
  --configuration Release \
  --filter "FullyQualifiedName~RecommendationInputBuilderTests"
```

Run recommendation-service tests:

```bash
dotnet test CareerAdvisor.sln \
  --configuration Release \
  --filter "FullyQualifiedName~RecommendationServiceTests"
```

Run the Sprint 3 integration tests:

```bash
dotnet test CareerAdvisor.sln \
  --configuration Release \
  --filter "FullyQualifiedName~Sprint3IntegrationTests"
```

Run the complete verification suite:

```bash
git diff --check
dotnet build CareerAdvisor.sln --configuration Release
dotnet test CareerAdvisor.sln --configuration Release
git status
```

The automated tests verify:

- Training-data validation
- Model training and evaluation
- Saved-model loading
- Metadata-defined label mapping
- Finite model scores
- Profile and assessment feature mapping
- Exactly three unique recommendations
- Valid career-catalogue mapping
- Input-based explanations
- Missing-profile rejection
- Incomplete-assessment rejection
- Recommendation persistence and reopening
- Absence of fabricated fallback recommendations