namespace Core.Aspects.Autofac;

/// <summary>Metodu bir izin koduyla işaretler; gerçek kontrol <see cref="SecuredOperationAspectHandler"/>'da yapılır.</summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class SecuredOperationAttribute(string permission) : AspectAttribute
{
    public string Permission { get; } = permission;

    public override Type HandlerType => typeof(SecuredOperationAspectHandler);
}
