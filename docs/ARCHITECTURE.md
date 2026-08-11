# Architecture Overview

## Purpose

The solution is structured to keep domain rules independent from infrastructure details and user interface concerns. This supports maintainability, testing, and future changes to storage or recommendation services without rewriting the core business logic.

## Project Responsibilities

- `CareerAdvisor.Core`
  - Domain models
  - Application-facing interfaces
  - Validation rules
  - Core business logic
- `CareerAdvisor.Infrastructure`
  - Entity Framework Core integration
  - SQLite persistence for development
  - ML.NET model loading and execution
  - Implementations of interfaces defined in `CareerAdvisor.Core`
- `CareerAdvisor.Web`
  - Blazor Web App user interface
  - Server-side interactivity
  - Dependency injection setup
  - User workflows for profiles, assessments, recommendations, and history
- `CareerAdvisor.Tests`
  - Automated tests covering Core and Infrastructure behavior

## Dependency Direction

The intended dependency direction is inward toward the core domain:

- `CareerAdvisor.Web` depends on `CareerAdvisor.Core` and uses registered services from `CareerAdvisor.Infrastructure`
- `CareerAdvisor.Infrastructure` depends on `CareerAdvisor.Core`
- `CareerAdvisor.Core` does not depend on Web or Infrastructure
- `CareerAdvisor.Tests` may reference Core and Infrastructure as needed for verification

This keeps the domain model and business rules at the center of the solution while infrastructure and UI remain replaceable layers around it.

## Application Flow

The planned runtime flow is:

`Blazor UI -> application interfaces -> infrastructure services -> SQLite/ML model`

In practical terms:

1. A user interacts with the Blazor UI in `CareerAdvisor.Web`.
2. The UI calls interfaces defined in `CareerAdvisor.Core`.
3. Dependency injection resolves those interfaces to implementations in `CareerAdvisor.Infrastructure`.
4. Infrastructure services read and write student data through SQLite and EF Core.
5. Recommendation services load or invoke ML.NET models to generate career suggestions, confidence scores, and supporting outputs.
6. Results flow back to the UI for display to the student.

## Architectural Notes

- Business rules and validation should remain in `CareerAdvisor.Core`, not in UI components or database classes.
- Infrastructure should implement interfaces rather than define the application contract.
- The web project should coordinate user interaction and presentation, but should avoid embedding persistence or ML logic directly in components.
- SQLite is the planned development database and may later be replaced without changing core business rules if interfaces remain stable.
- ML.NET should be treated as an implementation detail of recommendation services, not as a dependency of the UI layer.
