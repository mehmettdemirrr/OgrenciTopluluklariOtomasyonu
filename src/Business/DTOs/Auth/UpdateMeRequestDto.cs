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

    /// <summary>
    /// docs/MIMARI.md · A-56: ad soyad. Öğrenci numarasının aksine kimliğin veritabanı tarafındaki
    /// parçası değil — kullanıcı kendi adını düzeltebilir. Mevcut hesaplar boş başladığı için
    /// profil sayfası doldurmanın tek yolu.
    /// </summary>
    public string? FirstName { get; set; }

    /// <inheritdoc cref="FirstName"/>
    public string? LastName { get; set; }
}
