# Implementation Plan: Document Upload and Management

**Branch**: `001-document-upload-management` | **Date**: 2026-09-13 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/001-document-upload-management/spec.md`

## Summary

This feature adds a secure, offline-first document management capability for the ContosoDashboard training app. The implementation will allow users to upload supported files, associate them with personal or project contexts, search and browse them by metadata, and restrict access using the app’s existing role and project membership model. The solution will store metadata in the EF Core database and files on disk outside `wwwroot`, using a storage abstraction to keep the design compatible with future Azure migration without changing business logic. A background worker process will handle asynchronous malware scanning after upload, which keeps the user experience responsive and allows a queue-based Azure-native pattern to be introduced later without forcing a change to the app’s business workflow.

## Technical Context

**Language/Version**: C# / .NET 8.0  
**Primary Dependencies**: ASP.NET Core, Blazor Server, Entity Framework Core, LocalDB, Bootstrap 5, Azure Functions, Azure Queue Storage  
**Storage**: Local filesystem for uploaded files; SQL Server LocalDB via EF Core for metadata and access records; Azure Queue Storage for async scan jobs in the cloud-ready design  
**Testing**: xUnit + bUnit (planned for feature validation; not yet scaffolded in the repo)  
**Target Platform**: Linux/macOS/Windows developer workstation for the training app; Azure-hosted function environment for future production migration  
**Project Type**: Web application (single-project Blazor Server app)  
**Performance Goals**: Uploads complete within 30 seconds for files up to 25 MB; document list/search screens load within 2 seconds for up to 500 records  
**Constraints**: Offline-only, no external cloud dependency, secure file storage outside the web root, role-based authorization and project membership enforcement, asynchronous security scan before a document is marked available  
**Scale/Scope**: Single training application with a small user base, project-centric access model, and modest document volumes; cloud-ready extension for Azure-hosted scanning at larger scale

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

The design aligns with the project constitution:

- Training-first scope: pass. The feature stays offline and educational and does not introduce production-only assumptions.
- Security and access boundaries: pass. The feature enforces authorization by role and project membership before exposing files or metadata.
- Test-first change discipline: pass. The work will add validation around upload rules, authorization, and search results before finalizing behavior.
- Simple, explicit architecture: pass. The feature fits the existing Models, Data, Services, and Pages structure.
- Offline-first and maintainability: pass. Files are stored locally and abstracted behind an interface for future migration.

No constitutional exceptions are required for this feature.

## Project Structure

### Documentation (this feature)

```text
specs/001-document-upload-management/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── document-service.md
└── spec.md
```

### Source Code (repository root)

```text
ContosoDashboard/
├── Data/
│   └── ApplicationDbContext.cs
├── Models/
│   ├── Project.cs
│   ├── ProjectMember.cs
│   ├── User.cs
│   ├── TaskItem.cs
│   ├── Notification.cs
│   └── ...
├── Services/
│   ├── ProjectService.cs
│   ├── TaskService.cs
│   ├── UserService.cs
│   ├── NotificationService.cs
│   ├── DashboardService.cs
│   └── ...
├── Pages/
│   ├── Projects.razor
│   ├── ProjectDetails.razor
│   ├── Tasks.razor
│   ├── Index.razor
│   └── ...
├── Shared/
└── Program.cs
```

**Structure Decision**: The feature will be implemented within the existing single-application Blazor Server structure, adding new document entities to the EF Core model and extending the service/page layer without introducing a separate backend or front-end project. For the malware scan flow, the app will publish a queue message after upload, and an Azure Function with a Queue Storage trigger will process the job asynchronously. In the local training mode, the same queue message path can be simulated or skipped while preserving the same contract and behavior.

### Background scan workflow

The upload workflow will follow this sequence:

1. User uploads a file and the app validates extension, size, and metadata.
2. The application saves the file to a secure local path outside `wwwroot` and records document metadata as `PendingScan`.
3. The app emits a message to a queue containing document metadata, file path, and a scan request identifier.
4. An Azure Function with a Queue Storage trigger consumes the message and runs the malware or content inspection workflow.
5. The function updates the document state to `Approved`, `Rejected`, or `Quarantined` and notifies the app or marks the file unavailable for download.
6. The UI only surfaces the document for other users after the scan status reaches an approved state.

This pattern keeps the web request fast and avoids blocking the user on a potentially slow antivirus task. The same contract can be supported locally with a lightweight background worker if cloud services are not available during training.

## Complexity Tracking

No constitution violations are expected for this implementation. The design remains within the existing architecture and does not require a broader technology shift. The only additional complexity is the asynchronous scan pipeline, which is justified by the security requirement and the need to avoid blocking the upload experience.
