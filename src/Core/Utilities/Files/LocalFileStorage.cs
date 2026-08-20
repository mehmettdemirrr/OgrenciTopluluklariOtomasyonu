using Microsoft.Extensions.Options;

namespace Core.Utilities.Files;

/// <summary>
/// docs/MIMARI.md · Y-49: wwwroot dışında, tek bir kök altında saklar — erişim yalnızca bu arayüz
/// üzerinden; doğrudan statik dosya sunumu (UseStaticFiles) hiçbir zaman açılmaz.
/// </summary>
public sealed class LocalFileStorage : IFileStorage
{
    private readonly string _rootPath;

    public LocalFileStorage(IOptions<FileStorageSettings> settings)
    {
        _rootPath = Path.GetFullPath(settings.Value.RootPath)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        if (_rootPath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            .Contains("wwwroot", StringComparer.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "FileStorage:RootPath 'wwwroot' altında olamaz (Y-49) — dosyalar statik olarak yayınlanamaz.");
        }

        Directory.CreateDirectory(_rootPath);
    }

    public async Task SaveAsync(string generatedFileName, Stream content, CancellationToken cancellationToken = default)
    {
        var path = ResolvePath(generatedFileName);
        await using var fileStream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
        await content.CopyToAsync(fileStream, cancellationToken).ConfigureAwait(false);
    }

    public Task<Stream?> OpenReadAsync(string generatedFileName, CancellationToken cancellationToken = default)
    {
        var path = ResolvePath(generatedFileName);
        if (!File.Exists(path))
        {
            return Task.FromResult<Stream?>(null);
        }

        Stream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        return Task.FromResult<Stream?>(stream);
    }

    public Task<bool> DeleteAsync(string generatedFileName, CancellationToken cancellationToken = default)
    {
        var path = ResolvePath(generatedFileName);
        if (!File.Exists(path))
        {
            return Task.FromResult(false);
        }

        File.Delete(path);
        return Task.FromResult(true);
    }

    // Y-40'ın "sunucu tarafında üretilen ad" kuralı path traversal'ı zaten büyük ölçüde engeller,
    // ama burada da savunma: birleşik tam yol kökün dışına çıkamaz.
    private string ResolvePath(string generatedFileName)
    {
        var fullPath = Path.GetFullPath(Path.Combine(_rootPath, generatedFileName));
        if (!fullPath.StartsWith(_rootPath + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Geçersiz dosya adı.");
        }

        return fullPath;
    }
}
