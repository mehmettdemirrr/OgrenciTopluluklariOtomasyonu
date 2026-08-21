namespace Business.DTOs.Clubs;

public sealed class UpdateClubRequestDto
{
    public required string Name { get; set; }

    public string? Description { get; set; }
}
