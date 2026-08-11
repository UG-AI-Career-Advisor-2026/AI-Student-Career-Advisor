# AI-Powered Student Career Advisor

## Project Overview

AI-Powered Student Career Advisor is a university team project focused on helping students and recent graduates explore suitable technology careers. The application will guide users through profile setup, a structured assessment, and AI-assisted career recommendations that explain why a path may be a strong fit.

## Problem Being Solved

Many students understand their academic subjects better than the technology roles available to them after graduation. They may not know how their interests, strengths, and current skills connect to careers such as software development, cloud engineering, or cybersecurity. This project aims to provide a clear starting point by translating student information into practical career guidance and learning direction.

## MVP Features

- Student profile creation and editing
- Career assessment questionnaire
- Top three AI-generated career recommendations with confidence scores
- Explanation of why each career was recommended
- Skill-gap analysis for a selected career
- Personalised learning roadmap
- Recommendation history

## Technology Stack

- .NET 10
- Blazor Web App with Server interactivity
- ASP.NET Core
- ML.NET
- Entity Framework Core
- SQLite for development
- xUnit for testing

## Solution Structure

- `CareerAdvisor.Core`: domain models, interfaces, validation rules, and core business logic
- `CareerAdvisor.Infrastructure`: EF Core, SQLite, ML.NET model loading, data storage, and service implementations
- `CareerAdvisor.Web`: Blazor UI, dependency injection, and user interactions
- `CareerAdvisor.Tests`: automated tests for Core and Infrastructure

## Prerequisites

- .NET 10 SDK
- A local development environment capable of running ASP.NET Core applications
- Git

## Commands

Restore dependencies:

```bash
dotnet restore CareerAdvisor.sln
```

Build the solution:

```bash
dotnet build CareerAdvisor.sln
```

Run automated tests:

```bash
dotnet test CareerAdvisor.sln
```

Run the web application:

```bash
dotnet run --project src/CareerAdvisor.Web/CareerAdvisor.Web.csproj
```

## Development Status

The repository is currently in initial setup and documentation planning. The MVP scope, architecture direction, and contribution workflow are defined here, but the full application functionality described in this document should be treated as planned work rather than completed implementation.

## Short Roadmap

- Establish the initial solution structure and project references
- Define domain models, interfaces, and validation rules in `CareerAdvisor.Core`
- Implement persistence and service integrations in `CareerAdvisor.Infrastructure`
- Build the student workflow in the Blazor web application
- Add automated tests for core logic and infrastructure behavior
- Refine recommendation quality, explanations, and learning roadmap output
