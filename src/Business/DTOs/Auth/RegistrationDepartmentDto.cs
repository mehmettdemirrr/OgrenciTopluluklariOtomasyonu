namespace Business.DTOs.Auth;

/// <summary>
/// docs/PLAN-V2.md · Faz 11: kayıt formunun bölüm seçimi için dar, anonim-güvenli izdüşüm.
/// ReferenceDataService'in DepartmentListItemDto'su reference.manage gerektirdiği için burada
/// yeniden kullanılmaz — kayıt formu kimlik doğrulaması olmadan çalışır.
/// </summary>
public sealed class RegistrationDepartmentDto
{
    public int Id { get; set; }

    public required string Name { get; set; }

    public required string FacultyName { get; set; }
}
