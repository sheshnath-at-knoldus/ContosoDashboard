<!--
Sync Impact Report
- Version change: 0.0.0 -> 1.0.0
- Modified principles: N/A (new constitution)
- Added sections: Core Principles, Technology Constraints, Development Workflow, Governance
- Removed sections: N/A
- Deferred TODOs: None
-->

# ContosoDashboard Constitution

## Core Principles

### I. Training-First Scope
This project exists to teach Spec-Driven Development in a safe, offline environment. Every change must remain understandable to learners, must not claim production readiness, and must preserve the training goal of demonstrating core software engineering practices without external service dependencies.

Rationale: ContosoDashboard is intentionally a mock system for education. Keeping the project clearly scoped to training prevents the codebase from drifting into production assumptions and reduces risk during hands-on exercises.

### II. Security and Access Boundaries
All protected pages, services, and data access paths MUST enforce authorization before exposing data or actions. Role checks, project membership checks, and user isolation rules are mandatory for every feature that reads or mutates user-specific records.

Rationale: The project demonstrates real security responsibilities. A training app is still expected to enforce access boundaries clearly so learners see the difference between UI intent and server-side enforcement.

### III. Test-First Change Discipline
All non-trivial bug fixes and feature work MUST begin with a failing test or an equivalent validation step. The implementation must then satisfy the test before merge, with the final change set kept small and focused on the stated behavior.

Rationale: Test-first development is the main teaching outcome of this repository. Without a failing proof, changes are difficult to reason about and regressions become harder to catch.

### IV. Simple, Explicit Architecture
The codebase MUST maintain clear separation between Models, Data, Services, and UI. Business logic belongs in service-layer code, data access stays in the data layer, and pages remain thin and task-oriented. Cross-layer shortcuts and hidden coupling are prohibited.

Rationale: The sample application is designed to teach maintainable structure. Clear boundaries improve readability, reduce onboarding friction, and make change impact easier to analyze.

### V. Offline-First and Maintainability
The project MUST remain runnable without cloud infrastructure, external services, or paid dependencies. New features must work in the local training environment and must not require a production identity provider, cloud database, or service bus to function.

Rationale: The repository is meant to be accessible in constrained training environments. Local-first operation preserves learning continuity and keeps the system easy to inspect and modify.

## Technology Constraints

ContosoDashboard uses ASP.NET Core with Blazor Server, Entity Framework Core, and LocalDB for training scenarios. The project MUST prefer simple, explainable implementations over enterprise frameworks or hidden infrastructure.

- All application code MUST remain compatible with the existing .NET training stack unless a deliberate modernization task is approved.
- Authentication and authorization rules MUST be enforced server-side even when mock auth is used for training.
- Database and state changes MUST preserve the existing offline workflow and seeded sample data.
- New features MUST avoid introducing external runtime dependencies that would prevent local execution.
- Security-sensitive work MUST remain aligned with the training goals of teaching access control, data isolation, and safe service design.

## Development Workflow

All changes MUST be made through small, reviewable steps that preserve the repository's educational value.

- Each task MUST define the user-visible behavior it changes and the validation path used to confirm it.
- Pull requests MUST include evidence that the relevant tests or checks pass for the changed behavior.
- Changes that affect authentication, authorization, project membership, or data exposure MUST be reviewed by someone other than the original author.
- Documentation changes MUST be included when a feature alters the user workflow, security model, or project architecture.
- A feature is not complete until the code, tests, and documentation are aligned.

## Governance

This Constitution supersedes informal repository habits whenever a conflict arises. Compliance is mandatory for all contributors and reviewers in the project lifecycle.

Amendments require a documented rationale, a version bump in accordance with the policy below, and a review of the affected principles before merge. Any change that materially alters the training scope, security model, or development workflow MUST include a migration note or update to the relevant teaching material.

Versioning policy:
- MAJOR: backward-incompatible principle removals, redefining a principle, or changes that invalidate prior governance expectations.
- MINOR: adding a new principle or materially expanding an existing section with new mandatory requirements.
- PATCH: clarifying wording, fixing non-semantic mistakes, and improving readability without changing required behavior.

Compliance review expectations:
- Every change MUST be checked against the relevant principles before merge.
- Security-sensitive logic MUST be reviewed for authorization, user isolation, and data leakage risks.
- The project maintainers MUST reject any update that weakens the training-only safety boundaries or adds hidden production assumptions.

**Version**: 1.0.0 | **Ratified**: 2026-09-13 | **Last Amended**: 2026-09-13
