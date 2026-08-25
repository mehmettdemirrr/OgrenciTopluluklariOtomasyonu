namespace Business.DTOs.Reference;

/// <summary>
/// docs/MIMARI.md · K-33 (PLAN-V5 §27.1): mevcut bir kullanıcıya akademik personel profili ekler.
///
/// v5.0'a kadar <c>AcademicStaff</c> <b>salt-okunur</b>ydu — hiçbir uç oluşturmuyordu, tek kayıt
/// demo seed'inden geliyordu. Yani sisteme yeni danışman eklemek imkânsızdı ve kulübe atanabilecek
/// danışman kümesi sabitti (PLAN-V5 bildirilen #3).
///
/// Kullanıcıyı da birlikte oluşturmak isteyen akış <c>POST /api/users</c>'tır (Y-67) — bu uç
/// <b>zaten var olan</b> bir kullanıcıya profil eklemek içindir.
/// </summary>
public sealed class CreateAcademicStaffRequestDto
{
    public int ApplicationUserId { get; set; }

    public string Title { get; set; } = string.Empty;

    public int DepartmentId { get; set; }
}
