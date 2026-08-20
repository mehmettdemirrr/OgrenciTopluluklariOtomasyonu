namespace Core.Aspects.Autofac;

/// <summary>docs/MIMARI.md · A-17: referans verisi, topluluk listesi, izin kataloğu için.</summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class CacheAspectAttribute(int durationMinutes) : AspectAttribute
{
    public int DurationMinutes { get; } = durationMinutes;

    public override Type HandlerType => typeof(CacheAspectHandler);
}
