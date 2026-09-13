using Microsoft.AspNetCore.Components.Forms;
using Microsoft.EntityFrameworkCore;
using ContosoDashboard.Data;
using ContosoDashboard.Models;

namespace ContosoDashboard.Services;

public interface IDocumentService
{
    Task<List<Document>> GetUserDocumentsAsync(int userId, string? search = null, int? projectId = null);
    Task<List<Document>> GetProjectDocumentsAsync(int projectId, int requestingUserId, string? search = null);
    Task<Document?> GetDocumentByIdAsync(int documentId, int requestingUserId);
    Task<Document> UploadDocumentAsync(DocumentUploadRequest request, int userId);
    Task<bool> UpdateDocumentMetadataAsync(int documentId, int requestingUserId, string title, string? description, string category, string? tags, int? projectId, int? taskId);
    Task<bool> DeleteDocumentAsync(int documentId, int requestingUserId);
    Task<bool> ShareDocumentAsync(int documentId, int recipientUserId, int requestingUserId, string permissionLevel = "View");
    Task<List<Document>> GetRecentDocumentsAsync(int userId, int count = 5);
    Task<List<AuditEvent>> GetAuditEventsAsync(int documentId, int requestingUserId);
    Task<Stream> GetDownloadStreamAsync(int documentId, int requestingUserId);
    Task<List<Document>> GetSharedWithMeDocumentsAsync(int userId, string? search = null);
    Task<DocumentActivityReport?> GetDocumentActivityReportAsync(int requestingUserId);
}

public class DocumentService : IDocumentService
{
    private const long MaxFileSizeBytes = 25 * 1024 * 1024;
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".txt",
        ".csv", ".png", ".jpg", ".jpeg", ".gif", ".bmp", ".webp"
    };

    private readonly ApplicationDbContext _context;
    private readonly IFileStorageService _fileStorageService;
    private readonly INotificationService _notificationService;

    public DocumentService(ApplicationDbContext context, IFileStorageService fileStorageService, INotificationService notificationService)
    {
        _context = context;
        _fileStorageService = fileStorageService;
        _notificationService = notificationService;
    }

    public async Task<List<Document>> GetUserDocumentsAsync(int userId, string? search = null, int? projectId = null)
    {
        var projectIds = await _context.ProjectMembers
            .Where(pm => pm.UserId == userId)
            .Select(pm => pm.ProjectId)
            .Distinct()
            .ToListAsync();

        var query = _context.Documents
            .Include(d => d.UploadedByUser)
            .Include(d => d.Project)
            .Include(d => d.DocumentShares)
            .ThenInclude(ds => ds.User)
            .Where(d => !d.IsDeleted && (
                d.UploadedByUserId == userId ||
                d.DocumentShares.Any(ds => ds.UserId == userId) ||
                (d.ProjectId.HasValue && projectIds.Contains(d.ProjectId.Value))))
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(d =>
                d.Title.Contains(term) ||
                (d.Description != null && d.Description.Contains(term)) ||
                (d.Tags != null && d.Tags.Contains(term)) ||
                d.FileName.Contains(term) ||
                d.UploadedByUser.DisplayName.Contains(term) ||
                (d.Project != null && d.Project.Name.Contains(term)));
        }

        if (projectId.HasValue)
        {
            query = query.Where(d => d.ProjectId == projectId.Value);
        }

        return await query
            .OrderByDescending(d => d.UploadedAtUtc)
            .ToListAsync();
    }

    public async Task<List<Document>> GetProjectDocumentsAsync(int projectId, int requestingUserId, string? search = null)
    {
        var project = await _context.Projects
            .Include(p => p.ProjectMembers)
            .FirstOrDefaultAsync(p => p.ProjectId == projectId);

        if (project == null)
        {
            return new List<Document>();
        }

        var isMember = project.ProjectMembers.Any(pm => pm.UserId == requestingUserId) || project.ProjectManagerId == requestingUserId;
        if (!isMember)
        {
            return new List<Document>();
        }

        var query = _context.Documents
            .Include(d => d.UploadedByUser)
            .Include(d => d.Project)
            .Include(d => d.DocumentShares)
            .ThenInclude(ds => ds.User)
            .Where(d => !d.IsDeleted && d.ProjectId == projectId)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(d =>
                d.Title.Contains(term) ||
                (d.Description != null && d.Description.Contains(term)) ||
                (d.Tags != null && d.Tags.Contains(term)) ||
                d.UploadedByUser.DisplayName.Contains(term));
        }

        return await query
            .OrderByDescending(d => d.UploadedAtUtc)
            .ToListAsync();
    }

    public async Task<Document?> GetDocumentByIdAsync(int documentId, int requestingUserId)
    {
        var document = await _context.Documents
            .Include(d => d.UploadedByUser)
            .Include(d => d.Project)
            .Include(d => d.DocumentShares)
            .ThenInclude(ds => ds.User)
            .FirstOrDefaultAsync(d => d.DocumentId == documentId && !d.IsDeleted);

        if (document == null)
        {
            return null;
        }

        var isAuthorized = document.UploadedByUserId == requestingUserId ||
            document.DocumentShares.Any(ds => ds.UserId == requestingUserId) ||
            (document.ProjectId.HasValue && (
                document.Project!.ProjectManagerId == requestingUserId ||
                document.Project.ProjectMembers.Any(pm => pm.UserId == requestingUserId)));

        return isAuthorized ? document : null;
    }

    public async Task<Document> UploadDocumentAsync(DocumentUploadRequest request, int userId)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (request.File == null)
        {
            throw new InvalidOperationException("A file is required for upload.");
        }

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            throw new InvalidOperationException("Document title is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Category))
        {
            throw new InvalidOperationException("Document category is required.");
        }

        var extension = Path.GetExtension(request.File.Name);
        if (string.IsNullOrWhiteSpace(extension) || !AllowedExtensions.Contains(extension))
        {
            throw new InvalidOperationException("Unsupported file type. Allowed types: PDF, Office documents, text files, and common image formats.");
        }

        if (request.File.Size > MaxFileSizeBytes)
        {
            throw new InvalidOperationException("Document size exceeds the 25 MB upload limit.");
        }

        if (request.ProjectId.HasValue)
        {
            var project = await _context.Projects
                .Include(p => p.ProjectMembers)
                .FirstOrDefaultAsync(p => p.ProjectId == request.ProjectId.Value);

            if (project == null)
            {
                throw new InvalidOperationException("The selected project could not be found.");
            }

            var isAllowed = project.ProjectManagerId == userId || project.ProjectMembers.Any(pm => pm.UserId == userId);
            if (!isAllowed)
            {
                throw new InvalidOperationException("You do not have permission to upload documents to that project.");
            }
        }

        using var sourceStream = new MemoryStream();
        await request.File.OpenReadStream(MaxFileSizeBytes).CopyToAsync(sourceStream);
        sourceStream.Position = 0;

        var safeStoredPath = await _fileStorageService.SaveAsync(sourceStream, request.File.Name, "documents");

        var document = new Document
        {
            Title = request.Title.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            Category = request.Category.Trim(),
            FileName = request.File.Name,
            StoredFilePath = safeStoredPath,
            FileType = extension.TrimStart('.'),
            FileSizeBytes = request.File.Size,
            UploadedByUserId = userId,
            ProjectId = request.ProjectId,
            TaskId = request.TaskId,
            Tags = request.Tags,
            Status = DocumentStatus.PendingScan,
            UploadedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        _context.Documents.Add(document);
        await _context.SaveChangesAsync();

        var audit = new AuditEvent
        {
            DocumentId = document.DocumentId,
            UserId = userId,
            EventType = "Upload",
            EventDescription = $"Uploaded document '{document.Title}'"
        };

        _context.AuditEvents.Add(audit);
        await _context.SaveChangesAsync();

        await _notificationService.CreateNotificationAsync(new Notification
        {
            UserId = userId,
            Title = "Document uploaded",
            Message = $"Your document '{document.Title}' has been uploaded and is pending scan review.",
            Type = NotificationType.SystemAnnouncement,
            Priority = NotificationPriority.Informational
        });

        return document;
    }

    public async Task<bool> UpdateDocumentMetadataAsync(int documentId, int requestingUserId, string title, string? description, string category, string? tags, int? projectId, int? taskId)
    {
        var document = await _context.Documents
            .Include(d => d.Project)
            .ThenInclude(p => p!.ProjectMembers)
            .FirstOrDefaultAsync(d => d.DocumentId == documentId && !d.IsDeleted);

        if (document == null)
        {
            return false;
        }

        var isOwner = document.UploadedByUserId == requestingUserId;
        var isProjectManager = document.Project != null && document.Project.ProjectManagerId == requestingUserId;
        if (!isOwner && !isProjectManager)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            return false;
        }

        document.Title = title.Trim();
        document.Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        document.Category = category.Trim();
        document.Tags = tags;
        document.ProjectId = projectId;
        document.TaskId = taskId;
        document.UpdatedAtUtc = DateTime.UtcNow;

        _context.AuditEvents.Add(new AuditEvent
        {
            DocumentId = document.DocumentId,
            UserId = requestingUserId,
            EventType = "MetadataUpdate",
            EventDescription = $"Updated metadata for document '{document.Title}'"
        });

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteDocumentAsync(int documentId, int requestingUserId)
    {
        var document = await _context.Documents
            .Include(d => d.Project)
            .ThenInclude(p => p!.ProjectMembers)
            .FirstOrDefaultAsync(d => d.DocumentId == documentId && !d.IsDeleted);

        if (document == null)
        {
            return false;
        }

        var isAuthorized = document.UploadedByUserId == requestingUserId ||
            (document.Project != null && document.Project.ProjectManagerId == requestingUserId);

        if (!isAuthorized)
        {
            return false;
        }

        document.IsDeleted = true;
        document.UpdatedAtUtc = DateTime.UtcNow;

        _context.AuditEvents.Add(new AuditEvent
        {
            DocumentId = document.DocumentId,
            UserId = requestingUserId,
            EventType = "Delete",
            EventDescription = $"Deleted document '{document.Title}'"
        });

        await _context.SaveChangesAsync();

        try
        {
            await _fileStorageService.DeleteAsync(document.StoredFilePath);
        }
        catch
        {
            // ignore storage cleanup failures in training app; the DB record remains soft deleted
        }

        return true;
    }

    public async Task<bool> ShareDocumentAsync(int documentId, int recipientUserId, int requestingUserId, string permissionLevel = "View")
    {
        var document = await _context.Documents
            .Include(d => d.Project)
            .ThenInclude(p => p!.ProjectMembers)
            .FirstOrDefaultAsync(d => d.DocumentId == documentId && !d.IsDeleted);

        if (document == null)
        {
            return false;
        }

        var isAuthorized = document.UploadedByUserId == requestingUserId ||
            (document.Project != null && document.Project.ProjectManagerId == requestingUserId);

        if (!isAuthorized)
        {
            return false;
        }

        if (recipientUserId == requestingUserId)
        {
            return false;
        }

        var existingShare = await _context.DocumentShares
            .FirstOrDefaultAsync(ds => ds.DocumentId == documentId && ds.UserId == recipientUserId);

        if (existingShare != null)
        {
            existingShare.PermissionLevel = permissionLevel;
            existingShare.SharedAtUtc = DateTime.UtcNow;
        }
        else
        {
            _context.DocumentShares.Add(new DocumentShare
            {
                DocumentId = documentId,
                UserId = recipientUserId,
                SharedByUserId = requestingUserId,
                PermissionLevel = permissionLevel,
                SharedAtUtc = DateTime.UtcNow
            });
        }

        _context.AuditEvents.Add(new AuditEvent
        {
            DocumentId = document.DocumentId,
            UserId = requestingUserId,
            EventType = "Share",
            EventDescription = $"Shared document '{document.Title}' with user id {recipientUserId}"
        });

        await _context.SaveChangesAsync();

        await _notificationService.CreateNotificationAsync(new Notification
        {
            UserId = recipientUserId,
            Title = "Document shared with you",
            Message = $"A document named '{document.Title}' was shared with you.",
            Type = NotificationType.SystemAnnouncement,
            Priority = NotificationPriority.Informational
        });

        return true;
    }

    public async Task<List<Document>> GetRecentDocumentsAsync(int userId, int count = 5)
    {
        var docs = await GetUserDocumentsAsync(userId);
        return docs.Take(count).ToList();
    }

    public async Task<List<AuditEvent>> GetAuditEventsAsync(int documentId, int requestingUserId)
    {
        var document = await GetDocumentByIdAsync(documentId, requestingUserId);
        if (document == null)
        {
            return new List<AuditEvent>();
        }

        return await _context.AuditEvents
            .Include(a => a.User)
            .Where(a => a.DocumentId == documentId)
            .OrderByDescending(a => a.EventTimeUtc)
            .ToListAsync();
    }

    public async Task<Stream> GetDownloadStreamAsync(int documentId, int requestingUserId)
    {
        var document = await GetDocumentByIdAsync(documentId, requestingUserId);
        if (document == null)
        {
            throw new UnauthorizedAccessException("You are not authorized to access this document.");
        }

        var stream = await _fileStorageService.OpenReadAsync(document.StoredFilePath);
        return stream;
    }

    public async Task<List<Document>> GetSharedWithMeDocumentsAsync(int userId, string? search = null)
    {
        var query = _context.Documents
            .Include(d => d.UploadedByUser)
            .Include(d => d.Project)
            .Include(d => d.DocumentShares)
            .ThenInclude(ds => ds.User)
            .Where(d => !d.IsDeleted &&
                d.UploadedByUserId != userId &&
                d.DocumentShares.Any(ds => ds.UserId == userId))
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(d =>
                d.Title.Contains(term) ||
                (d.Description != null && d.Description.Contains(term)) ||
                (d.Tags != null && d.Tags.Contains(term)) ||
                d.UploadedByUser.DisplayName.Contains(term));
        }

        return await query
            .OrderByDescending(d => d.UploadedAtUtc)
            .ToListAsync();
    }

    public async Task<DocumentActivityReport?> GetDocumentActivityReportAsync(int requestingUserId)
    {
        var requestingUser = await _context.Users.FindAsync(requestingUserId);
        if (requestingUser == null || requestingUser.Role != UserRole.Administrator)
        {
            return null;
        }

        var activeDocuments = await _context.Documents
            .Include(d => d.UploadedByUser)
            .Where(d => !d.IsDeleted)
            .ToListAsync();

        var eventCounts = await _context.AuditEvents
            .GroupBy(a => a.EventType)
            .Select(g => new { EventType = g.Key, Count = g.Count() })
            .ToListAsync();

        return new DocumentActivityReport
        {
            TotalDocuments = activeDocuments.Count,
            ActiveUploaderCount = activeDocuments.Select(d => d.UploadedByUserId).Distinct().Count(),
            TotalStorageBytes = activeDocuments.Sum(d => d.FileSizeBytes),
            DocumentsByCategory = activeDocuments
                .GroupBy(d => d.Category)
                .OrderByDescending(g => g.Count())
                .ToDictionary(g => g.Key, g => g.Count()),
            DocumentsByFileType = activeDocuments
                .GroupBy(d => d.FileType.ToUpperInvariant())
                .OrderByDescending(g => g.Count())
                .ToDictionary(g => g.Key, g => g.Count()),
            DocumentsByStatus = activeDocuments
                .GroupBy(d => d.Status.ToString())
                .ToDictionary(g => g.Key, g => g.Count()),
            AccessEventsByType = eventCounts.ToDictionary(e => e.EventType, e => e.Count),
            TopUploaders = activeDocuments
                .GroupBy(d => d.UploadedByUser.DisplayName)
                .Select(g => new UploaderActivity { DisplayName = g.Key, DocumentCount = g.Count() })
                .OrderByDescending(u => u.DocumentCount)
                .Take(5)
                .ToList()
        };
    }
}
