namespace Business.DTOs.Reference;

/// <summary>
/// docs/MIMARI.md · K-33: unvan ve bölüm güncellenir. <c>ApplicationUserId</c> burada YOK —
/// profilin hangi kullanıcıya ait olduğu kimliğin parçasıdır; değişmesi gerekiyorsa profil silinip
/// yenisi açılır (Student'ın öğrenci numarasıyla aynı gerekçe, PLAN-V4 §19.2).
/// </summary>
public sealed class UpdateAcademicStaffRequestDto
{
    public string Title { get; set; } = string.Empty;

    public int DepartmentId { get; set; }
}
