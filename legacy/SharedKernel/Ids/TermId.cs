namespace SharedKernel.Ids;

public readonly record struct TermId(Guid Value)
{
    public static TermId New() => new(Guid.NewGuid());

    public override string ToString() => Value.ToString();
}