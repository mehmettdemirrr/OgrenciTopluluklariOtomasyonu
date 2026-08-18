namespace SharedKernel.Ids;

public readonly record struct ClubId(Guid Value)
{
    public static ClubId New() => new(Guid.NewGuid());

    public override string ToString() => Value.ToString();
}