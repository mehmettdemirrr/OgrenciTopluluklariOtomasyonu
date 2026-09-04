namespace WebAPI.Models;

/// <summary>
/// docs/MIMARI.md · Y-05/Y-09: `IFormFile` YALNIZCA bu katmanda yaşar. Controller bunu
/// Business'ın ilkel DTO'suna çevirir; Business ASP.NET'e bağlanmaz.
/// Model bağlama adları: `Documents[0].DocumentTypeId`, `Documents[0].File`.
/// </summary>
public sealed class SubmitClubApplicationForm
{
    public string ProposedName { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string Justification { get; set; } = string.Empty;

    public int ProposedAdvisorId { get; set; }

    /// <summary>docs/MIMARI.md · K-35/A-60: önerilen kategori. Opsiyonel.</summary>
    public int? ProposedCategoryId { get; set; }

    /// <summary>docs/MIMARI.md · K-40: opsiyonel logo. Y-05: IFormFile bu katmanda kalır.</summary>
    public IFormFile? Logo { get; set; }

    public List<SubmitClubApplicationDocumentForm> Documents { get; set; } = [];
}

public sealed class SubmitClubApplicationDocumentForm
{
    public int DocumentTypeId { get; set; }

    public IFormFile? File { get; set; }
}
