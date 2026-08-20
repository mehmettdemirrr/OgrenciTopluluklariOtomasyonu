namespace Core.Utilities.Files;

/// <summary>docs/MIMARI.md · K-05/A-31: dosya baytlarının fiziksel depolanması. Görünürlük/iş kuralı bilmez.</summary>
public interface IFileStorage
{
    Task SaveAsync(string generatedFileName, Stream content, CancellationToken cancellationToken = default);

    Task<Stream?> OpenReadAsync(string generatedFileName, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(string generatedFileName, CancellationToken cancellationToken = default);
}
