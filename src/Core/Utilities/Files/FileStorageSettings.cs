namespace Core.Utilities.Files;

/// <summary>docs/MIMARI.md · Y-49: wwwroot dışı klasör. RootPath secret değil, appsettings'te durabilir.</summary>
public sealed class FileStorageSettings
{
    public string RootPath { get; init; } = string.Empty;

    public long MaxUploadBytes { get; init; } = 5 * 1024 * 1024;
}
