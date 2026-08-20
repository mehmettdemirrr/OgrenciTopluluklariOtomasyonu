namespace Core.Aspects.Autofac;

/// <summary>Metodu bir EF transaction'ı içinde çalıştırır; gerçek yönetim <see cref="TransactionAspectHandler"/>'da yapılır.</summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class TransactionAspectAttribute : AspectAttribute
{
    public override Type HandlerType => typeof(TransactionAspectHandler);
}
