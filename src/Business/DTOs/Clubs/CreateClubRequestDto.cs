namespace Business.DTOs.Clubs;

public sealed class CreateClubRequestDto
{
    public required string Name { get; set; }

    public string? Description { get; set; }

    public int AdvisorId { get; set; }
}
