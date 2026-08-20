namespace Core.Aspects.Autofac;

/// <summary>docs/MIMARI.md · Y-45: yetki matrisini değiştiren her uç ilgili anahtarları temizler.</summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class CacheRemoveAspectAttribute(string pattern) : AspectAttribute
{
    public string Pattern { get; } = pattern;

    public override Type HandlerType => typeof(CacheRemoveAspectHandler);
}
