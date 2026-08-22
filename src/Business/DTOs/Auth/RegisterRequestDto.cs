namespace Business.DTOs.Auth;

public sealed class RegisterRequestDto
{
    public string Email { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string StudentNumber { get; set; } = string.Empty;

    public int DepartmentId { get; set; }

    public int EnrollmentYear { get; set; }
}
