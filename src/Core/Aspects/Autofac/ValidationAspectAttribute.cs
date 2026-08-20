namespace Core.Aspects.Autofac;

/// <summary>Metodu bir FluentValidation validator tipiyle işaretler; gerçek doğrulama <see cref="ValidationAspectHandler"/>'da yapılır.</summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class ValidationAspectAttribute(Type validatorType) : AspectAttribute
{
    public Type ValidatorType { get; } = validatorType;

    public override Type HandlerType => typeof(ValidationAspectHandler);
}
