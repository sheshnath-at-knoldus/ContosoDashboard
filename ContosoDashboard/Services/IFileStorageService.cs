namespace ContosoDashboard.Services;

public interface IFileStorageService
{
    Task<string> SaveAsync(Stream content, string fileName, string? folder = null);
    Task DeleteAsync(string storedPath);
    Task<Stream> OpenReadAsync(string storedPath);
    string GetSafeFileName(string fileName);
    string GetStorageRoot();
}
