namespace SharedKernel.Ids;

public readonly record struct StudentId(Guid Value)
{
    public static StudentId New() => new(Guid.NewGuid());

    public override string ToString() => Value.ToString();
}