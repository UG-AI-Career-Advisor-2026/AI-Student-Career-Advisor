# Skill-Gap Matching Rules

## Purpose and Scope

This document defines the deterministic Core contracts and name-matching rules
used to support CareerIQ skill-gap analysis. The current implementation provides
classification rules, normalization, approved aliases, pure matching primitives,
and result validation. It does not implement the analysis service, persistence,
roadmap generation, or a user interface.

Career required skills always come from `CareerProfile.RequiredSkills`, which is
loaded from `data/career-catalog.json`. The alias catalogue does not replace or
extend a career's required-skill list. It only defines approved ways a saved
student skill can be recognized as a catalogue requirement.

## Academic MVP Baseline

CareerIQ uses `Intermediate` as the academic MVP comparison baseline:

- No recognized saved-profile skill match is `Missing`.
- A recognized `Beginner` skill is `NeedsDevelopment`.
- A recognized `Intermediate`, `Advanced`, or `Expert` skill is `Matched`.

`Intermediate` is only an academic MVP comparison baseline. It is not evidence
of professional readiness, employability, job performance, industry competence,
or certification. A `Matched` result must not be presented as a guarantee that a
student meets an employer's requirements.

## Normalization

Both catalogue requirements and saved student skills are normalized before
comparison:

1. Leading and trailing whitespace is removed.
2. Text is converted to invariant lowercase.
3. Repeated whitespace is collapsed to one space.
4. Common punctuation becomes token boundaries.
5. Meaningful technology punctuation is converted to stable words:
   - `#` becomes `sharp`, so `C#` becomes `c sharp`.
   - `+` becomes `plus`, so `C++` becomes `c plus plus`.
   - `&` becomes `and`.
   - Slashes, hyphens, parentheses, periods, and commas become boundaries.

Examples include:

- `CI/CD` → `ci cd`
- `TCP/IP` → `tcp ip`
- `UI/UX` → `ui ux`
- `AI/ML` → `ai ml`
- `Power-BI` → `power bi`

Normalization does not use stemming, edit distance, fuzzy matching, semantic
inference, ML.NET, an LLM, or arbitrary character substring matching.

## Canonical and Alias Matching

The complete normalized canonical catalogue phrase is always accepted. An
additional alias is accepted only when it is explicitly listed in the Core alias
catalogue.

An approved phrase can occur inside a longer saved skill description only when:

- all phrase tokens are consecutive;
- every token uses complete token boundaries; and
- no arbitrary character substring comparison is used.

Therefore `SQL` can match `Advanced SQL reporting`, but it cannot match `NoSQL`.
Similarly, `Java` cannot match `JavaScript`, and `AI` cannot match an unrelated
word containing the same characters.

### Approved aliases

| Career-catalogue requirement | Additional approved aliases |
| --- | --- |
| C# or Java Programming | C#, C# Programming, Java, Java Programming |
| Python Scripting | Python |
| Git Version Control | Git |
| RESTful API Design | RESTful API, REST API |
| SQL and Database Integration | SQL |
| SQL Querying | SQL |
| Python or R Programming | Python, Python Programming, R, R Programming |
| Data Visualization (Tableau/Power BI) | Data Visualization, Tableau, Power BI |
| Excel/Spreadsheets | Excel, Spreadsheets |
| Scripting (Python/Bash) | Python, Bash, Python Scripting, Bash Scripting |
| AWS/Azure/GCP Platforms | AWS, Amazon Web Services, Azure, Microsoft Azure, GCP, Google Cloud Platform |
| Infrastructure as Code (Terraform) | Infrastructure as Code, IaC, Terraform |
| CI/CD Integration | CI/CD |
| TCP/IP Protocol Suite | TCP/IP |
| Packet Analysis (Wireshark) | Packet Analysis, Wireshark |
| SQL Server / PostgreSQL / MySQL | SQL Server, PostgreSQL, Postgres, MySQL |
| Scripting (Bash/Python) | Bash, Python, Bash Scripting, Python Scripting |
| Figma / Adobe XD | Figma, Adobe XD |
| TensorFlow or PyTorch | TensorFlow, PyTorch |
| Mathematics (Linear Algebra/Calculus) | Linear Algebra, Calculus |

Broad aliases such as `programming`, `scripting`, `cloud`, `security`,
`database`, or `design` are deliberately excluded. An unlisted synonym is not a
match even when it might seem related.

## Candidate Selection

Matching operates on one required skill at a time and returns at most one saved
student skill:

1. Recognized candidates are grouped by normalized saved-skill name so casing,
   whitespace, or punctuation duplicates are considered once.
2. The highest-proficiency candidate is selected.
3. A proficiency tie prefers an exact normalized canonical match.
4. The next tie-breaker prefers the longest approved phrase.
5. A remaining tie uses the normalized saved-skill name, followed by the
   original display name and stable skill ID.

The final tie-breakers are ordinal and do not depend on database or input
collection order.

The original catalogue requirement and original saved skill names are preserved
for display. Normalized strings are comparison values only.

The same saved student skill may match two different career requirements. Global
allocation of saved skills across requirements is intentionally not performed by
these primitives and belongs to the later analysis-service workflow.

## Validation

`SkillGapResultValidator` uses the shared `ValidationResult` pattern and
accumulates ordinary domain errors. It validates:

- student and career identity;
- career code and title;
- the fixed `Intermediate` baseline;
- at least one result item;
- defined classification and proficiency values;
- classification, selected-skill, and proficiency consistency; and
- unique required skills after normalization.

It does not reject the same saved skill appearing on different required-skill
items.

The JSON career repository also checks the catalogue's documented invariant that
each career contains at least six required skills. Required skill values must be
nonblank, already trimmed, and unique after normalization. Invalid catalogue
values are rejected rather than silently rewritten.

## Limitations

- Aliases are a small, manually reviewed list and may not recognize every valid
  way a student describes a skill.
- Proficiency is self-reported profile data and is not independently assessed.
- Matching does not measure depth, recency, practical experience, or quality.
- Compound catalogue requirements treat an explicitly approved alternative as a
  recognized match; they do not prove mastery of every technology in the phrase.
- The rules do not rank careers, change recommendation scores, or use labour-market
  information.
