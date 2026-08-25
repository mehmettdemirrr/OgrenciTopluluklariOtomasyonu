namespace Business.DTOs.Admin;

/// <summary>
/// docs/MIMARI.md · A-56/Y-67 (K-32): yönetici kullanıcı oluşturma.
///
/// v5.0'a kadar bu istek yalnızca Identity kaydı üretiyordu. Sonuç: <c>Member</c> rolü verilen
/// kullanıcının <c>Student</c> kaydı olmadığı için kulübe başvuramıyor, etkinliğe kaydolamıyordu
/// ("öğrenci profiline sahip değilsiniz"); <c>Advisor</c> verilenin <c>AcademicStaff</c> kaydı
/// olmadığı için kulübe danışman atanamıyordu. Ne kullanıcı ne de onu oluşturan yönetici sebebi
/// görebiliyordu (PLAN-V5 bulgu 10).
/// </summary>
public sealed class CreateUserRequestDto
{
    public string Email { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public IReadOnlyCollection<string> RoleNames { get; set; } = [];

    /// <summary>Y-67: <c>Member</c> rolü seçildiyse zorunlu — öğrenci numarası.</summary>
    public string? StudentNumber { get; set; }

    /// <summary>Y-67: <c>Member</c> veya <c>Advisor</c> seçildiyse zorunlu.</summary>
    public int? DepartmentId { get; set; }

    /// <summary>Y-67: <c>Member</c> seçildiyse zorunlu.</summary>
    public int? EnrollmentYear { get; set; }

    /// <summary>Y-67: <c>Advisor</c> rolü seçildiyse zorunlu — akademik unvan.</summary>
    public string? Title { get; set; }
}
