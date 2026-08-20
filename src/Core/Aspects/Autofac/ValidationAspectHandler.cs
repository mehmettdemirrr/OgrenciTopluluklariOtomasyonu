using Autofac;
using Castle.DynamicProxy;
using FluentValidation;
using Core.Utilities.Results;

namespace Core.Aspects.Autofac;

/// <summary>
/// docs/MIMARI.md · Aspect kataloğu: "yalnızca biçimsel doğrulama" — iş kuralı değil.
/// Y-33: service locator (ILifetimeScope.Resolve) yalnızca aspect içinde kullanılır.
/// </summary>
public sealed class ValidationAspectHandler(ILifetimeScope lifetimeScope) : IAspectHandler
{
    public async Task<TResult> HandleAsync<TResult>(IInvocation invocation, AspectAttribute attribute, Func<Task<TResult>> next)
    {
        var validatorType = ((ValidationAspectAttribute)attribute).ValidatorType;
        var validatedType = GetValidatedType(validatorType);

        var argument = invocation.Arguments.FirstOrDefault(a => a is not null && validatedType.IsInstanceOfType(a));
        if (argument is null)
        {
            throw new InvalidOperationException(
                $"{invocation.Method.Name} metodunda {validatedType.Name} tipinde bir parametre bulunamadı (ValidationAspect).");
        }

        var validator = (IValidator)lifetimeScope.Resolve(validatorType);
        var contextType = typeof(ValidationContext<>).MakeGenericType(validatedType);
        var context = (IValidationContext)Activator.CreateInstance(contextType, argument)!;

        var validationResult = await validator.ValidateAsync(context).ConfigureAwait(false);
        if (validationResult.IsValid)
        {
            return await next().ConfigureAwait(false);
        }

        var message = string.Join(" ", validationResult.Errors.Select(e => e.ErrorMessage));
        return AspectResultFactory.Build<TResult>(nameof(Result.ValidationError), message);
    }

    private static Type GetValidatedType(Type validatorType) =>
        validatorType.GetInterfaces()
            .First(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IValidator<>))
            .GetGenericArguments()[0];
}
