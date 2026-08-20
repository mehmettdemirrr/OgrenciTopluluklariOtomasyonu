using Entities;

namespace DataAccess.Seed;

/// <summary>
/// docs/MIMARI.md · A-27: Faz 5'in dikey diliminin denenebilmesi için gereken asgari referans
/// verisi — genel bir fakülte/bölüm yönetim ucu değil (bu Faz 7 kapsamı), yalnızca demo akışının
/// üzerine kurulacağı sabit satırlar. HasData IClock kullanamadığı için tarihler sabit literaldir.
/// </summary>
public static class DomainSeedData
{
    public const int FacultyId = 1;
    public const int DepartmentId = 1;
    public const int AcademicTermId = 1;

    public static Faculty Faculty() => new()
    {
        Id = FacultyId,
        Name = "Mühendislik Fakültesi",
    };

    public static Department Department() => new()
    {
        Id = DepartmentId,
        Name = "Bilgisayar Mühendisliği",
        FacultyId = FacultyId,
    };

    public static AcademicTerm AcademicTerm() => new()
    {
        Id = AcademicTermId,
        Name = "2026-2027 Güz",
        StartDateUtc = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc),
        EndDateUtc = new DateTime(2027, 1, 31, 0, 0, 0, DateTimeKind.Utc),
        IsCurrent = true,
    };
}
