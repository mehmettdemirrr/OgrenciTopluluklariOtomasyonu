namespace Core.Aspects.Autofac;

/// <summary>
/// docs/MIMARI.md · Y-45: yetki matrisini değiştiren her uç ilgili anahtarları temizler.
/// docs/PLAN-V2.md · Faz 14 (14.4): bir yazma ucu birden fazla okuma önbelleğini etkileyebilir
/// (ör. ClubManager.SetStatusAsync hem "ClubManager." hem "PublicContentManager." önbelleğini
/// bayatlatır) — attribute [AttributeUsage(AllowMultiple = true)] OLMADAN, tek örnekte birden
/// fazla desen taşıyarak bunu destekler.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class CacheRemoveAspectAttribute(params string[] patterns) : AspectAttribute
{
    public IReadOnlyCollection<string> Patterns { get; } = patterns;

    public override Type HandlerType => typeof(CacheRemoveAspectHandler);
}
