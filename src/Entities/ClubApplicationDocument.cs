using Core.Entities;

namespace Entities;

/// <summary>
/// docs/MIMARI.md · K-37/A-63: başvuruya yüklenen evrak. Dosya daima Protected görünürlükte
/// saklanır (Y-70) ve yalnızca korumalı uçtan servis edilir.
/// Y-18: (ClubApplicationId, ClubDocumentTypeId) unique — aynı evrak iki kez yüklenemez.
/// </summary>
public sealed class ClubApplicationDocument : IEntity
{
    public int Id { get; set; }

    public int ClubApplicationId { get; set; }

    public int ClubDocumentTypeId { get; set; }

    public int StoredFileId { get; set; }
}
