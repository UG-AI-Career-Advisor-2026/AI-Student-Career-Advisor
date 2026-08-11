# Contributing Guide

## Workflow Rules

- Do not develop directly on `main`.
- Create a branch for every task before starting work.
- Use one task per branch.
- Submit changes through a pull request for review.

## Branch Naming

Use one of the following prefixes:

- `feature/`
- `fix/`
- `docs/`
- `test/`

## Commit Messages

- Write clear, descriptive commit messages.
- Keep each commit focused on a coherent change.
- Avoid vague messages such as `update files` or `misc fixes`.

## Required Validation

Before submitting work, run:

```bash
dotnet build CareerAdvisor.sln
dotnet test CareerAdvisor.sln
```

## Pull Requests

- Open a pull request when the branch is ready for review.
- Summarise the purpose of the change clearly.
- Note any known limitations, follow-up work, or areas that need attention from reviewers.

## Files That Must Not Be Committed

Do not commit:

- `bin/`
- `obj/`
- database files
- secrets
- personal settings files

## Collaboration Expectations

- Keep changes aligned to the current scope of the project.
- Prefer small, reviewable pull requests over large unrelated changes.
- Update documentation when behavior, setup, or project structure changes.
