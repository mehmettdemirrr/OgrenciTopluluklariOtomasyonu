namespace Business.DTOs.Auth;

public sealed class ConfirmEmailRequestDto
{
    public int UserId { get; set; }

    public string Token { get; set; } = string.Empty;
}
