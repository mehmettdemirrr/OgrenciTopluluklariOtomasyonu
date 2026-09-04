using Business.DTOs.Files;

namespace Business.DTOs.ClubApplications;

public sealed class SubmitClubApplicationRequestDto
{
    public string ProposedName { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string Justification { get; set; } = string.Empty;

    public int ProposedAdvisorId { get; set; }

    /// <summary>docs/MIMARI.md · K-35/A-60: önerilen kategori. Opsiyonel; verilirse var olmalı.</summary>
    public int? ProposedCategoryId { get; set; }

    /// <summary>docs/MIMARI.md · K-37/Y-71: yüklenen evraklar. Bütünlük kontrolü Business'ta, katalog okunarak.</summary>
    public IReadOnlyList<ClubApplicationDocumentUploadDto> Documents { get; set; } = [];

    /// <summary>docs/MIMARI.md · K-40/A-69: opsiyonel logo. Null = logosuz başvuru.</summary>
    public UploadFileRequestDto? Logo { get; set; }
}
