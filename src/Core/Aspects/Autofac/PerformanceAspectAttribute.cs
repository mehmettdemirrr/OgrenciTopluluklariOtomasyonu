namespace Core.Aspects.Autofac;

/// <summary>Metodu bir süre eşiğiyle işaretler; gerçek ölçüm <see cref="PerformanceAspectHandler"/>'da yapılır.</summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class PerformanceAspectAttribute(int thresholdMilliseconds) : AspectAttribute
{
    public int ThresholdMilliseconds { get; } = thresholdMilliseconds;

    public override Type HandlerType => typeof(PerformanceAspectHandler);
}
