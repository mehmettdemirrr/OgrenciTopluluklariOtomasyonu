namespace Platform.Contracts;

/// <summary>
/// Bir modülün kendi şemasındaki outbox tablosuna erişim sözleşmesi.
/// Her modül kendi implementasyonunu kendi DbContext'i üzerinden sağlar.
/// </summary>
public interface IOutboxStore
{
    string ModuleName { get; }

    Task<IReadOnlyList<OutboxMessageRecord>> GetUnpublishedAsync(int batchSize, CancellationToken cancellationToken);

    Task MarkPublishedAsync(Guid id, CancellationToken cancellationToken);

    Task MarkFailedAsync(Guid id, string error, CancellationToken cancellationToken);
}