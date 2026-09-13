using Microsoft.AspNetCore.Components.Forms;

namespace ContosoDashboard.Models;

public class DocumentUploadRequest
{
    public string Title { get; set; } = string.Empty;
    public string Category { get; set; } = "General";
    public string? Description { get; set; }
    public int? ProjectId { get; set; }
    public int? TaskId { get; set; }
    public string? Tags { get; set; }
    public IBrowserFile? File { get; set; }
}
