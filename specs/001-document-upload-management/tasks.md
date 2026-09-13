# Tasks: Document Upload and Management

**Input**: Design documents from `/specs/001-document-upload-management/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/document-service.md

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Prepare the project structure and shared document storage foundations

- [x] T001 Create document feature storage layout and secure upload directories under ContosoDashboard/AppData/uploads
- [x] T002 [P] Add document storage abstraction and default local implementation in ContosoDashboard/Services/IFileStorageService.cs and ContosoDashboard/Services/LocalFileStorageService.cs
- [x] T003 [P] Update ContosoDashboard/Program.cs to register the document storage service and any required dependency injection entries

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core data and authorization foundations for all document workflows

**Checkpoint**: Foundation ready - document user story implementation can now begin in parallel

- [x] T004 Add document-related entities to ContosoDashboard/Models/Document.cs, ContosoDashboard/Models/DocumentShare.cs, and ContosoDashboard/Models/AuditEvent.cs
- [x] T005 [P] Extend ContosoDashboard/Data/ApplicationDbContext.cs with DbSet properties, indexes, and seed-safe configuration for document tables
- [x] T006 [P] Implement core authorization and query helpers in ContosoDashboard/Services/DocumentService.cs (depends on T004, T005)
- [x] T007 Add document scan status and queue-ready metadata fields to the data model and business logic for PendingScan/Approved/Rejected/Quarantined handling
- [x] T008 Configure upload validation rules and error handling for file type, file size, and safe filename generation in ContosoDashboard/Services/DocumentService.cs

---

## Phase 3: User Story 1 - Upload and organize work documents (Priority: P1) 🎯 MVP

**Goal**: Allow users to upload valid documents, store metadata, and organize them by category and project.

**Independent Test**: A logged-in user can upload a supported file, complete the required metadata, and see the document appear in their list with correct category, project, and size details.

### Implementation for User Story 1

- [x] T009 [P] [US1] Create the Document model and validation attributes in ContosoDashboard/Models/Document.cs
- [x] T010 [P] [US1] Update ContosoDashboard/Data/ApplicationDbContext.cs to include Document configuration and relationships to User, Project, and TaskItem
- [x] T011 [US1] Implement upload workflow and storage orchestration in ContosoDashboard/Services/DocumentService.cs (depends on T004, T006, T008)
- [x] T012 [US1] Add upload UI flow in ContosoDashboard/Pages/Documents.razor and ContosoDashboard/Pages/Documents.razor.cs for selecting files, entering metadata, and submitting uploads
- [x] T013 [US1] Add personal and project document list views and category filtering in ContosoDashboard/Pages/Documents.razor and ContosoDashboard/Pages/ProjectDetails.razor
- [x] T014 [US1] Add success and validation messaging for size/type errors and upload completion in ContosoDashboard/Pages/Documents.razor

**Checkpoint**: At this point, User Story 1 should be fully functional and independently testable.

---

## Phase 4: User Story 2 - Find and access project documents with proper permissions (Priority: P2)

**Goal**: Let users find authorized documents and access them through project, search, and list workflows without exposing unauthorized content.

**Independent Test**: A project member can search and browse documents by title, tag, uploader, or project and is blocked from viewing documents outside their permitted scope.

### Implementation for User Story 2

- [x] T015 [P] [US2] Add project-scoped document queries and permission checks in ContosoDashboard/Services/DocumentService.cs
- [x] T016 [P] [US2] Implement document search and filter logic for title, description, tags, uploader name, and project association in ContosoDashboard/Services/DocumentService.cs
- [x] T017 [US2] Add authorized document preview/download access in ContosoDashboard/Pages/ProjectDetails.razor and ContosoDashboard/Pages/Documents.razor
- [x] T018 [US2] Integrate recent-document and project-document display in ContosoDashboard/Pages/Index.razor and ContosoDashboard/Pages/ProjectDetails.razor
- [x] T019 [US2] Add permission-safe document result rendering and hiding unauthorized docs in the UI layers for ContosoDashboard/Pages/Documents.razor

**Checkpoint**: At this point, User Stories 1 and 2 should both work independently.

---

## Phase 5: User Story 3 - Share and manage document lifecycle (Priority: P3)

**Goal**: Support sharing, metadata updates, deletion, notifications, and audit tracking for document lifecycle management.

**Independent Test**: A document owner or authorized manager can share a document, update its details, and delete it after confirmation while the event is recorded for audit and notifications.

### Implementation for User Story 3

- [x] T020 [P] [US3] Create DocumentShare and AuditEvent models and validation in ContosoDashboard/Models/DocumentShare.cs and ContosoDashboard/Models/AuditEvent.cs
- [x] T021 [US3] Add share and audit persistence logic in ContosoDashboard/Services/DocumentService.cs and ContosoDashboard/Data/ApplicationDbContext.cs
- [x] T022 [US3] Implement metadata update and replace-file actions in ContosoDashboard/Services/DocumentService.cs
- [x] T023 [US3] Implement delete confirmation and cleanup workflow, including file removal and audit logging, in ContosoDashboard/Services/DocumentService.cs
- [x] T024 [US3] Integrate recipient notifications and shared-document views in ContosoDashboard/Services/NotificationService.cs and ContosoDashboard/Pages/Notifications.razor
- [x] T025 [US3] Add administrator reporting hooks and access pattern summary support in ContosoDashboard/Services/DocumentService.cs and ContosoDashboard/Pages/Index.razor

**Checkpoint**: All user stories should now be independently functional and ready for cross-cutting polish.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Final quality, documentation, and security hardening across the feature

- [x] T026 [P] Review all document pages and services for authorization consistency, file validation, and security hygiene in ContosoDashboard/Pages and ContosoDashboard/Services
- [x] T027 [P] Add or update training-focused documentation in README.md and StakeholderDocs/document-upload-and-management-feature.md
- [x] T028 Validate upload, search, and permission scenarios against quickstart.md in specs/001-document-upload-management/quickstart.md
- [x] T029 [P] Run targeted validation of upload limits, search results, and unauthorized access behavior against the core document workflow
- [x] T030 Update dashboard and notifications UX to reflect document status, recent uploads, and scan state transitions

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - blocks all story work
- **User Stories (Phase 3+)**: All depend on the Foundational phase completion
  - User Story 1 (P1) is the MVP path and should be validated before moving forward
  - User Story 2 (P2) can proceed after core upload and authorization foundations are ready
  - User Story 3 (P3) builds on the same document service layer and can proceed after US1 and US2 are stable
- **Polish (Phase 6)**: Depends on all required user stories being complete

### User Story Dependencies

- **User Story 1 (P1)**: None beyond the Foundational phase
- **User Story 2 (P2)**: Depends on the upload, storage, and authorization foundation from US1
- **User Story 3 (P3)**: Depends on the core document model and permission behavior from US1/US2

### Parallel Opportunities

- T002 and T003 can run in parallel for setup
- T005 and T006 can run in parallel after T004 is available
- T009 and T010 can be worked in parallel within US1
- T015 and T016 can be worked in parallel within US2
- T020 and T021 can be worked in parallel within US3
- Final validation tasks T026, T028, and T029 can be run in parallel once the stories are complete

---

## Parallel Example: User Story 1

```bash
# Shared setup and model work
Task: "Add document storage abstraction in ContosoDashboard/Services/IFileStorageService.cs"
Task: "Update ContosoDashboard/Data/ApplicationDbContext.cs with Document configuration"

# Upload workflow tasks
Task: "Implement upload workflow in ContosoDashboard/Services/DocumentService.cs"
Task: "Add upload UI flow in ContosoDashboard/Pages/Documents.razor"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational
3. Complete Phase 3: User Story 1
4. Validate the upload and list workflow independently
5. Stop and confirm the user can upload a valid file and view it in their own document list

### Incremental Delivery

1. Save the document metadata and secure file path
2. Validate file type and size before persistence
3. Add project-scoped search and access checks
4. Add sharing, notifications, and audit tracking
5. Final polish and validation of the end-to-end flow

### Parallel Team Strategy

With multiple developers:

1. One developer completes the shared setup and storage work
2. One developer focuses on the upload and model path for US1
3. One developer focuses on search, permissions, and project access for US2
4. One developer focuses on sharing, notifications, and reporting for US3
5. Final validation is performed after each story is stabilized
