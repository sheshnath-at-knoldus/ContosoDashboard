# Document Service Contract

## Purpose

This contract describes the internal document workflow for the ContosoDashboard application. It is designed to support the Blazor Server pages and the service layer without exposing an external public API.

## Core interfaces

```csharp
public interface IFileStorageService
{
    Task<string> UploadAsync(Stream fileStream, string fileName, string contentType, string targetPath);
    Task DeleteAsync(string storedPath);
    Task<Stream> DownloadAsync(string storedPath);
    Task<string> GetDownloadUrlAsync(string storedPath);
}

public interface IDocumentService
{
    Task<Document> UploadAsync(DocumentUploadRequest request, int actingUserId);
    Task<List<Document>> GetUserDocumentsAsync(int userId);
    Task<List<Document>> GetProjectDocumentsAsync(int projectId, int actingUserId);
    Task<Document?> GetByIdAsync(int documentId, int actingUserId);
    Task<bool> UpdateMetadataAsync(int documentId, DocumentUpdateRequest request, int actingUserId);
    Task<bool> DeleteAsync(int documentId, int actingUserId);
    Task<List<Document>> SearchAsync(string query, int actingUserId);
    Task<bool> ShareAsync(int documentId, int targetUserId, int actingUserId, string permissionLevel);
}
```

## Request shapes

```csharp
public sealed class DocumentUploadRequest
{
    public required string Title { get; init; }
    public string? Description { get; init; }
    public required string Category { get; init; }
    public int? ProjectId { get; init; }
    public int? TaskId { get; init; }
    public string? Tags { get; init; }
    public required Stream FileStream { get; init; }
    public required string FileName { get; init; }
    public required string ContentType { get; init; }
}
```

## Responsibilities

- Validate file type and size before persisting.
- Enforce user authorization before read, update, share, or delete operations.
- Persist document metadata in the database.
- Save the binary content through `IFileStorageService`.
- Record audit events for access and lifecycle actions.

## Constraints

- Must remain compatible with LocalDB and offline-only training use.
- Must not expose document content outside the app’s authorization model.
- Must keep file handling outside `wwwroot` for security.
