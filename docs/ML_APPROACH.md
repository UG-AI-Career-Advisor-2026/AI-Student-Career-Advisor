# ML.NET Career Recommendation Approach

## 1. Purpose

This document defines the proposed machine-learning approach for the AI Student Career Advisor. It is a design document for the MVP and does not implement or train the final model.

The model will analyse a student's academic background, self-assessed skills, interests and questionnaire responses. It will then recommend the three most suitable careers from the eight careers supported by the MVP.

The first sample dataset is synthetic and exists only to demonstrate the expected data structure. It must not be treated as real evidence about students or career suitability.

## 2. Prediction Problem

The recommendation task will be treated as a multiclass classification problem. Each training row represents a student profile, and the target label represents the career that best matches that profile.

Although the model predicts one primary class internally, the application will examine the probability scores for all eight classes. The three careers with the highest scores will be presented as the student's top three recommendations.

The scores will support ranking and user-friendly confidence values. They must not be presented as guarantees that a student will succeed in a particular career.

## 3. Supported Career Labels

The model will use the following eight career labels:

1. Software Developer
2. Data Analyst
3. Cybersecurity Analyst
4. Cloud Engineer
5. Network Administrator
6. Database Administrator
7. UI/UX Designer
8. AI/ML Engineer

These labels must remain consistent across the dataset, career catalogue, domain models and user interface.

## 4. Input Features

The proposed dataset combines student profile information with questionnaire responses.

| Column | Type | Description |
|---|---|---|
| AcademicBackground | Categorical text | The student's main academic background, such as ComputerScience, InformationTechnology or Statistics. |
| ProgrammingLevel | Numeric | Self-assessed programming ability. |
| DataAnalysisLevel | Numeric | Ability and interest in analysing and interpreting data. |
| CybersecurityInterest | Numeric | Interest in protecting systems, investigating threats and managing security risks. |
| CloudInterest | Numeric | Interest in cloud platforms, deployment and scalable infrastructure. |
| NetworkingLevel | Numeric | Understanding of computer networks and network administration. |
| DatabaseLevel | Numeric | Understanding of databases, SQL and data management. |
| DesignInterest | Numeric | Interest in interface design and user experience. |
| AIInterest | Numeric | Interest in artificial intelligence and machine learning. |
| MathematicsLevel | Numeric | Confidence in mathematics, statistics and quantitative reasoning. |
| ProblemSolving | Numeric | Preference and ability for solving technical or analytical problems. |
| Creativity | Numeric | Preference for creative thinking and producing original solutions. |
| AttentionToDetail | Numeric | Ability to notice errors, patterns and small but important details. |
| PreferredWorkStyle | Categorical text | Preferred working style, such as Independent, Collaborative, Structured, Creative or Research. |
| CareerLabel | Target label | The career associated with the representative profile. |

## 5. Numeric Representation

Skill levels, interests and questionnaire responses will use a five-point scale:

| Value | Meaning |
|---:|---|
| 1 | Very low |
| 2 | Low |
| 3 | Moderate |
| 4 | High |
| 5 | Very high |

Using the same range makes the questionnaire easier to understand and keeps the numeric features consistent. The application should validate responses so that values below 1 or above 5 are rejected.

Categorical columns such as `AcademicBackground` and `PreferredWorkStyle` will be converted into numeric feature vectors using one-hot encoding during preprocessing.

## 6. Proposed ML.NET Pipeline

The proposed ML.NET pipeline will perform the following stages:

1. Load the CSV data into an `IDataView`.
2. Convert `CareerLabel` into a key type using `MapValueToKey`.
3. One-hot encode `AcademicBackground` and `PreferredWorkStyle`.
4. Combine the encoded categorical values and numeric questionnaire values into a single `Features` vector.
5. Normalize the feature vector using `NormalizeMinMax`.
6. Train a multiclass model using `SdcaMaximumEntropy`.
7. Convert the predicted key back to its career name using `MapKeyToValue`.
8. Read the probability score for each career and return the three highest-scoring careers.

`SdcaMaximumEntropy` is suitable as the initial trainer because it supports multiclass classification, produces class probabilities and is practical for structured numeric and categorical features. Other trainers may be compared later when a larger and more reliable dataset is available.

No final trainer will be implemented as part of this issue.

## 7. Training and Validation

The planned full dataset will be divided as follows:

- 80% for training
- 20% for validation
- Random seed: 42, to make experiments reproducible

The dataset should remain balanced so that every career is adequately represented in both portions. A larger dataset may also use cross-validation during model comparison.

The current 24-row synthetic CSV is too small to measure real predictive quality. Its purpose is only to confirm the schema, labels and planned preprocessing steps.

## 8. Evaluation Metrics

The trained multiclass model will be evaluated using:

### Micro-accuracy

Micro-accuracy measures the overall proportion of predictions that are correct. A value closer to 1 indicates better overall performance.

### Macro-accuracy

Macro-accuracy calculates accuracy separately for each career and then averages the results. It helps identify whether the model performs reasonably across all eight careers instead of favouring only common labels.

### Log-loss

Log-loss evaluates the quality of the model's probability scores. Incorrect predictions made with very high confidence receive a larger penalty. A value closer to 0 is better.

The three metrics should be considered together. Accuracy alone is not sufficient because the application also displays confidence scores.

## 9. Dataset Balance

The initial sample CSV will contain 24 rows:

- Three Software Developer profiles
- Three Data Analyst profiles
- Three Cybersecurity Analyst profiles
- Three Cloud Engineer profiles
- Three Network Administrator profiles
- Three Database Administrator profiles
- Three UI/UX Designer profiles
- Three AI/ML Engineer profiles

This creates equal representation for all eight labels. However, balance in synthetic data does not prove that the profiles reflect real students.

## 10. Limitations and Responsible Use

The initial dataset is manually created synthetic sample data. It has not been collected from students, career counsellors, employers or validated research. A model trained only on this sample would learn the assumptions of its creators and could produce misleading recommendations.

Before the system is considered reliable:

- More representative training data must be collected.
- Career experts should review the feature definitions and labels.
- Data should represent students from different programmes and backgrounds.
- Potential bias should be tested across relevant student groups.
- Users should be informed that recommendations are advisory.
- Students should be allowed to explore careers outside the top three.
- Personal profile information must be handled responsibly.

The application must not claim that an ML.NET prediction guarantees career success. Final decisions should remain with the student, supported where possible by academic advisers or career counsellors.

## 11. Future Implementation

A later issue will implement the ML.NET pipeline, train candidate models and connect the selected model to the recommendation service. That work should use the schema proposed here but may refine it if testing identifies weaknesses.

The future implementation should also record the model version, training date, evaluation results and dataset version so that recommendations can be reproduced and reviewed.

## References

- [Microsoft ML.NET multiclass classification tutorial](https://learn.microsoft.com/en-us/dotnet/machine-learning/tutorials/github-issue-classification)
- [Microsoft ML.NET model evaluation metrics](https://learn.microsoft.com/en-us/dotnet/machine-learning/resources/metrics)
- [Microsoft ML.NET SdcaMaximumEntropy trainer](https://learn.microsoft.com/en-us/dotnet/api/microsoft.ml.trainers.sdcamaximumentropymulticlasstrainer)