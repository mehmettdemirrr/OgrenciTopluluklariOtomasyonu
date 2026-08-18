namespace Core.Aspects.Autofac;

/// <summary>
/// docs/MIMARI.md · Aspect kataloğu: her aspect attribute'u kendi davranışını uygulayan
/// <see cref="IAspectHandler"/> implementasyonunun tipini tip güvenli şekilde bildirir.
/// </summary>
public abstract class AspectAttribute : Attribute
{
    public abstract Type HandlerType { get; }
}
