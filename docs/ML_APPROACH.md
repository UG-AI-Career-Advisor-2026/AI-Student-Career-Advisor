# CareerIQ Recommendation Feature and ML Approach

## Purpose

CareerIQ will use ML.NET multiclass classification to rank the eight supported technology careers from a student's saved profile and completed career assessment.

This document defines the stable feature contract used by the recommendation workflow. It does not train a model or generate recommendations.

## Current Status

The recommendation feature schema and synthetic training dataset are ready for later ML.NET integration.

The application does not currently generate career recommendations or confidence scores. Recommendation generation will be implemented in a later Sprint 3 issue.

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

These mappings must remain stable because the ML output label must resolve to an existing career in `data/career-catalog.json`.

## ML Input Columns

The dataset contains the following 26 columns:

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

The student's name, identifiers and timestamps are not ML features.

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

Interest and skill text is matched case-insensitively using whole words or recognized phrases from `RecommendationFeatureSchema.ProfileDomainKeywordsByColumn`. Arbitrary substring matching must not be used.

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

When both an interest and skill match the same domain, the higher applicable value is used.

## Assessment Question Mapping

Every one of the 15 assessment questions maps to exactly one ML input column.

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

## Numeric Assessment Values

The first ten questions use numeric values between 1 and 5.

| Questions | Option A | Option B | Option C | Option D |
|---|---:|---:|---:|---:|
| Q1–Q5 | 5 | 4 | 3 | 1 |
| Q6–Q9 | 5 | 4 | 2 | 1 |
| Q10 | 5 | 4 | 3 | 2 |

The option identifiers follow the stable format `Q<number>_OPT_<letter>`, such as `Q1_OPT_A`.

## Categorical Assessment Values

| Question | A | B | C | D |
|---|---|---|---|---|
| Q11 | `RemoteHybrid` | `OfficeBased` | `Flexible` | `NoPreference` |
| Q12 | `Fast` | `Moderate` | `Flexible` | `Slow` |
| Q13 | `Growth` | `Stability` | `Balanced` | `Situational` |
| Q14 | `SalaryBenefits` | `SalaryEquity` | `FreelanceContract` | `NoPreference` |
| Q15 | `Technology` | `Finance` | `Healthcare` | `Other` |

## Synthetic Training Dataset

The training data is stored in:

```text
data/training/sample-career-training-data.csv