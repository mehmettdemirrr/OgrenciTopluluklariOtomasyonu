using Entities.Enums;

namespace Entities.Dtos.Reports;

/// <summary>docs/MIMARI.md · K-06: kulüp üye listesi Excel çıktısının bir satırı.</summary>
public sealed class ClubMemberExportRowDto
{
    public required string StudentNumber { get; set; }

    public required string Email { get; set; }

    public required string DepartmentName { get; set; }

    public ClubRole ClubRole { get; set; }

    public DateTime JoinedAtUtc { get; set; }
}
