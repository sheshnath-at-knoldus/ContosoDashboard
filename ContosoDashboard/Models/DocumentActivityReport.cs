namespace ContosoDashboard.Models;

public class DocumentActivityReport
{
    public int TotalDocuments { get; set; }
    public int ActiveUploaderCount { get; set; }
    public long TotalStorageBytes { get; set; }
    public Dictionary<string, int> DocumentsByCategory { get; set; } = new();
    public Dictionary<string, int> DocumentsByFileType { get; set; } = new();
    public Dictionary<string, int> DocumentsByStatus { get; set; } = new();
    public Dictionary<string, int> AccessEventsByType { get; set; } = new();
    public List<UploaderActivity> TopUploaders { get; set; } = new();
}

public class UploaderActivity
{
    public string DisplayName { get; set; } = string.Empty;
    public int DocumentCount { get; set; }
}
