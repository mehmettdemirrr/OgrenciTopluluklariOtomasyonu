namespace Entities.Enums;

/// <summary>Üyelik başvuru durumu. DB'de int, API'de metin.</summary>
public enum ApplicationStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2,
}
