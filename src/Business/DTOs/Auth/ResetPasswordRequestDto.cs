namespace Business.DTOs.Auth;

public sealed class ResetPasswordRequestDto
{
    public int UserId { get; set; }

    public string Token { get; set; } = string.Empty;

    public string NewPassword { get; set; } = string.Empty;
}
