namespace Business.DTOs.Auth;

/// <summary>
/// docs/PLAN-V4.md §19.2: yalnızca bölüm ve kayıt yılı. **Öğrenci numarası burada yok** — kimliğin
/// parçası olduğu için değiştirilebilir olması `(ClubId, StudentId, TermId)` unique index'ini ve
/// rapor izlenebilirliğini bozar; değişmesi gerekiyorsa Admin işi. E-posta da yok: değişimi
/// doğrulama akışını yeniden tetiklemeyi gerektirir (K-03'ün yüzeyini büyütür).
/// </summary>
public sealed class UpdateMeRequestDto
{
    public int DepartmentId { get; set; }

    public int EnrollmentYear { get; set; }
}
