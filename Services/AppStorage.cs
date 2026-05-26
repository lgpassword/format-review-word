using Microsoft.Extensions.Options;
using WordFormatAnalyzer.Options;

namespace WordFormatAnalyzer.Services;

public sealed class AppStorage
{
    public AppStorage(IWebHostEnvironment environment, IOptions<AppStorageOptions> options)
    {
        var storage = options.Value;
        DataDirectory = Path.GetFullPath(Path.Combine(environment.ContentRootPath, storage.DataDirectory));
        UploadDirectory = Path.GetFullPath(Path.Combine(DataDirectory, storage.UploadDirectory));
        WorkingDirectory = Path.GetFullPath(Path.Combine(DataDirectory, storage.WorkingDirectory));
        GeneratedDirectory = Path.GetFullPath(Path.Combine(DataDirectory, storage.GeneratedDirectory));
        DatabasePath = Path.GetFullPath(Path.Combine(DataDirectory, storage.DatabaseFileName));

        Directory.CreateDirectory(DataDirectory);
        Directory.CreateDirectory(UploadDirectory);
        Directory.CreateDirectory(WorkingDirectory);
        Directory.CreateDirectory(GeneratedDirectory);
    }

    public string DataDirectory { get; }
    public string UploadDirectory { get; }
    public string WorkingDirectory { get; }
    public string GeneratedDirectory { get; }
    public string DatabasePath { get; }

    public string CreateUploadPath(string originalFileName)
    {
        return CreateSafePath(UploadDirectory, originalFileName);
    }

    public string CreateGeneratedPath(string originalFileName)
    {
        return CreateSafePath(GeneratedDirectory, originalFileName);
    }

    public string CreateWorkingPath(string originalFileName)
    {
        return CreateSafePath(WorkingDirectory, originalFileName);
    }

    private static string CreateSafePath(string directory, string originalFileName)
    {
        var extension = Path.GetExtension(originalFileName);
        var name = Path.GetFileNameWithoutExtension(originalFileName);
        var safeName = string.Join("_", name.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));

        if (string.IsNullOrWhiteSpace(safeName))
        {
            safeName = "document";
        }

        return Path.Combine(directory, $"{DateTime.UtcNow:yyyyMMddHHmmssfff}-{Guid.NewGuid():N}-{safeName}{extension}");
    }
}
