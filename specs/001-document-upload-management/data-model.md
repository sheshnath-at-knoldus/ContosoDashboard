# Data Model: Document Upload and Management

## Entities

### Document

Represents a file uploaded by a user and associated with a project, personal space, or shared resource.

| Field | Type | Constraints | Notes |
| --- | --- | --- | --- |
| DocumentId | int | PK, required | Consistent with existing integer-based entity IDs |
| Title | string | required, max 255 | Human-readable document name |
| Description | string | optional | Free-form summary |
| Category | string | required | Example values: Project Documents, Team Resources, Personal Files, Reports, Presentations, Other |
| FileName | string | required, max 255 | Original file name or safe stored name |
| StoredFilePath | string | required, max 500 | Relative path used for local storage |
| FileType | string | required, max 255 | MIME type or safe file type label |
| FileSizeBytes | long | required | Used for validation, display, and reporting |
| UploadedByUserId | int | required, FK to User | Owner or uploader |
| ProjectId | int? | optional, FK to Project | For project-related documents |
| TaskId | int? | optional, FK to TaskItem | If attached to a task |
| UploadedAtUtc | DateTime | required | Upload timestamp |
| UpdatedAtUtc | DateTime | required | Last metadata update |
| IsDeleted | bool | required, default false | Soft-delete support before physical removal |

### DocumentShare

Tracks explicit document sharing relationships outside the default project visibility rules.

| Field | Type | Constraints | Notes |
| --- | --- | --- | --- |
| DocumentShareId | int | PK | Unique record |
| DocumentId | int | FK | Referenced document |
| UserId | int | FK | User receiving access |
| SharedByUserId | int | FK | User initiating the share |
| SharedAtUtc | DateTime | required | Timestamp |
| PermissionLevel | string | required | Example: View, Download, Edit |

### AuditEvent

Records document-related activity for reporting and compliance.

| Field | Type | Constraints | Notes |
| --- | --- | --- | --- |
| AuditEventId | int | PK | Unique event id |
| DocumentId | int? | optional | Document affected |
| UserId | int | required | Acting user |
| EventType | string | required | Upload, Download, Delete, Share, Edit |
| EventDescription | string | required, max 1000 | Human-readable summary |
| EventTimeUtc | DateTime | required | Timestamp |

## Relationships

- User 1:N Document
- Project 1:N Document
- Task Item 1:N Document
- Document 1:N DocumentShare
- Document 1:N AuditEvent
- User 1:N DocumentShare (via SharedByUserId and UserId)

## Validation and behavior rules

- A document must have a valid title and category before save.
- File size must remain within the 25 MB limit.
- File type must be on the approved list and must match the file being persisted.
- Authorization will enforce: owner, project manager, project member, or explicitly shared recipient visibility.
- Share records must be checked before a document is surfaced to a user outside the standard project context.
- Deletion should create an audit event and then remove the file and metadata after user confirmation.
