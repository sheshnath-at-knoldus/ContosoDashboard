# Research: Document Upload and Management

## Decisions

### Decision: Use a local file storage abstraction with database-backed metadata
The solution will create a dedicated `IFileStorageService` abstraction with a `LocalFileStorageService` implementation for the training app. Uploaded files will be stored outside `wwwroot` in a secure per-user/project path structure, while document metadata remains in the EF Core database.

**Rationale**: This matches the app’s offline-first training goals and the existing design notes in the stakeholder specification. It keeps the storage boundary explicit and allows a future Azure blob implementation without changing the business logic or UI.

### Decision: Add explicit metadata and access tracking entities
The feature will introduce `Document`, `DocumentShare`, and `AuditEvent` entities. The app already has a `ProjectMember` concept and role-based permission patterns, so the document feature can reuse those guards instead of inventing a parallel security model.

**Rationale**: The project already enforces authorization in service methods. Reusing the existing access model keeps the implementation predictable and easy to teach.

### Decision: Use project membership and role checks for document visibility
Visibility will be controlled by the existing `UserRole` and `ProjectMember` model, with project managers and owners inheriting more permissions than ordinary users.

**Rationale**: This is consistent with the current application’s security posture and with the IDOR protection patterns already used in the project services.

### Decision: Keep metadata in text fields for category and MIME type
The app’s existing pattern favors simple, understandable fields over heavy enum or typed lookup tables. Category will be stored as text values and file type as a string that can accommodate long MIME strings.

**Rationale**: This matches the training context and keeps the model approachable for learners while still meeting the security and reporting requirements.

## Alternatives considered

### Alternative: Store files directly under wwwroot
Rejected because it exposes the uploaded content to the web application and is inconsistent with the security guidance from the stakeholder specification.

### Alternative: Use direct DB binary storage
Rejected because it complicates file size management, increases database bloat, and produces a less portable design for future cloud migration.

### Alternative: Create a completely separate document module
Rejected because the project architecture is intentionally simple and the feature should integrate with existing project and task patterns rather than creating a separate, isolated subsystem.

## Research findings

- Existing service patterns already centralize authorization in the business layer.
- The app uses integer primary keys for core entities, so document IDs should remain integer-based for consistency with `UserId` and `ProjectId`.
- The project already supports offline workflows and local-only persistence, so file storage should remain file-system based for the initial feature.
- Search, browse, and dashboard widgets can be implemented using the same EF Core patterns already used for tasks and projects.
