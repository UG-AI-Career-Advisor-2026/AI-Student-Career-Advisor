# Learning Roadmap Rules

## Purpose

CareerIQ creates an academic learning roadmap for a saved student profile and a
supported career. A roadmap is guidance for further study; it is not a measure
of professional competence and does not guarantee certification, employment or
career success.

## Authorization and Inputs

A roadmap can only be generated when:

- the student profile exists in SQLite;
- the selected career exists in the approved JSON career catalogue; and
- that career appears in at least one persisted recommendation session owned by
  the same profile.

Any qualifying historical session is accepted. CareerIQ reads the saved
recommendation entities for authorization and never reruns prediction during
roadmap generation. Cross-profile sessions and roadmaps do not authorize access.

`ISkillGapService` performs the real deterministic comparison against every
required catalogue skill. Roadmap generation reuses that result and does not
duplicate, weaken or replace the approved matching rules. See
[Skill-Gap Matching Rules](SKILL_GAP_MATCHING.md).

## Deterministic Step Generation

Candidates are considered in this priority order:

1. Missing required skills
2. Skills classified as Needs Development
3. Suggested learning topics from the career catalogue
4. Certification exploration from the career catalogue

Matched skills do not create skill-development steps. The maximums are:

| Category | Maximum |
| --- | ---: |
| Missing and Needs Development combined | 6 |
| Suggested learning topics | 4 |
| Certification exploration | 2 |
| Total roadmap steps | 12 |

Missing skills consume the shared six skill positions before Needs Development
skills. Within each category, source labels are ordered first by their normalized
value using ordinal comparison and then by the original display value using
ordinal comparison. Persisted step orders are contiguous and begin at 1.

The normalized source label is also the category-independent duplicate key. The
first candidate from the highest-priority category wins. Duplicate-normalized
values inside one catalogue source collection are malformed catalogue data and
are rejected rather than silently removed.

## Stable Step Categories

Generated titles use stable prefixes:

- `Build foundation: ` — skill development for a Missing skill
- `Develop further: ` — skill development for a Beginner skill
- `Study topic: ` — suggested learning topic
- `Explore certification: ` — certification exploration

The interface recognizes only these exact ordinal prefixes. An unknown persisted
prefix is presented neutrally as roadmap guidance rather than guessed from
keywords.

Every generated `ResourceLink` is intentionally `string.Empty`. CareerIQ does
not invent providers, course URLs, prices, durations, institutions or enrolment
actions.

## Persistence and Reopening

There can be only one roadmap for a profile/career pair. If a valid persisted
roadmap already exists, generation returns it unchanged. CareerIQ does not
regenerate it, replace it, reset its progress or call skill-gap analysis again.

The roadmap and all ordered steps are inserted atomically. Reads use explicit
step ordering by persisted `Order` and then stable step ID. Roadmaps and progress
can therefore be reopened through a new database context or after application
restart.

## Progress Transitions

Progress updates are scoped by profile, roadmap and step identifiers:

- Incomplete → complete sets `IsCompleted` and records the service's current UTC
  timestamp.
- Complete → complete is idempotent and preserves the original timestamp.
- Complete → incomplete clears `CompletedAt`.
- Incomplete → incomplete remains incomplete with no timestamp.

The database update is conditional, so concurrent completion cannot replace the
first persisted completion timestamp. Unknown and cross-profile roadmaps receive
the same outward not-found behavior. A step from another roadmap is not updated.

## Limitations

- `Intermediate` is the academic MVP comparison baseline, not a professional
  standard.
- Saved skill proficiency is self-reported and is not independently assessed.
- Certification steps recommend exploration only. Certifications are not
  mandatory employment requirements.
- CareerIQ does not provide enrolment, payments, reminders or calendar services.
- Completing every step does not guarantee certification, employability,
  employment, job performance or career success.
