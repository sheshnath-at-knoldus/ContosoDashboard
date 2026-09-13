using System.Security.Cryptography;
using System.Text;

namespace ContosoDashboard.Services;

public class LocalFileStorageService : IFileStorageService
{
    private readonly string _storageRoot;

    public LocalFileStorageService()
    {
        var appDataPath = Path.Combine(AppContext.BaseDirectory, "AppData", "uploads");
        _storageRoot = Path.GetFullPath(appDataPath);
        Directory.CreateDirectory(_storageRoot);
    }

    public string GetStorageRoot() => _storageRoot;

    public string GetSafeFileName(string fileName)
    {
        var extension = Path.GetExtension(fileName);
        var name = Path.GetFileNameWithoutExtension(fileName);
        var safeName = string.Concat(name.Select(ch => char.IsLetterOrDigit(ch) || ch == '-' || ch == '_' || ch == '.' ? ch : '_')).Trim();
        safeName = string.IsNullOrWhiteSpace(safeName) ? "document" : safeName;

        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(fileName + DateTime.UtcNow.Ticks.ToString()))).Substring(0, 8);
        return $"{safeName}_{hash}{extension}";
    }

    public async Task<string> SaveAsync(Stream content, string fileName, string? folder = null)
    {
        if (content == null) throw new ArgumentNullException(nameof(content));
        if (string.IsNullOrWhiteSpace(fileName)) throw new ArgumentException("File name is required", nameof(fileName));

        var targetFolder = string.IsNullOrWhiteSpace(folder)
            ? _storageRoot
            : Path.Combine(_storageRoot, folder);

        Directory.CreateDirectory(targetFolder);

        var safeFileName = GetSafeFileName(fileName);
        var fullPath = Path.Combine(targetFolder, safeFileName);

        using var destination = File.Create(fullPath);
        await content.CopyToAsync(destination);

        return fullPath;
    }

    public async Task DeleteAsync(string storedPath)
    {
        if (string.IsNullOrWhiteSpace(storedPath)) return;

        var fullPath = Path.GetFullPath(storedPath);
        if (File.Exists(fullPath))
        {
            await Task.Run(() => File.Delete(fullPath));
        }
    }

    public async Task<Stream> OpenReadAsync(string storedPath)
    {
        if (string.IsNullOrWhiteSpace(storedPath)) throw new ArgumentException("Stored path is required", nameof(storedPath));

        var fullPath = Path.GetFullPath(storedPath);
        if (!File.Exists(fullPath)) throw new FileNotFoundException("Document not found", fullPath);

        return await Task.FromResult<Stream>(File.OpenRead(fullPath));
    }
}
