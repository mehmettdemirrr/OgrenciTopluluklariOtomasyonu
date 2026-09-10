namespace Business.DTOs.Public;

/// <summary>
/// Anasayfa takvimi. <see cref="Locked"/> true ise kimlik alanları (id, başlık, kulüp) boştur —
/// üyelere özel etkinliğin varlığı tarih olarak görünür, içeriği sızmaz (Y-72).
/// </summary>
public sealed class PublicCalendarEventDto
{
    public bool Locked { get; set; }

    public int? Id { get; set; }

    public int? ClubId { get; set; }

    public string? ClubName { get; set; }

    public string? Title { get; set; }

    public string? Location { get; set; }

    public DateTime StartDateUtc { get; set; }

    public DateTime EndDateUtc { get; set; }

    public int? PosterFileId { get; set; }
}
