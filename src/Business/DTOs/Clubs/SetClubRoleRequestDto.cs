using Entities.Enums;

namespace Business.DTOs.Clubs;

public sealed class SetClubRoleRequestDto
{
    public ClubRole ClubRole { get; set; }

    /// <summary>
    /// docs/MIMARI.md · K-36/A-61: atanacak unvan. Verilirse <see cref="ClubRole"/> TANIMDAN okunur
    /// ve yukarıdaki alan yok sayılır (Y-22). Null ise unvansız atama — bugünkü davranış.
    /// </summary>
    public int? ClubRoleDefinitionId { get; set; }
}
