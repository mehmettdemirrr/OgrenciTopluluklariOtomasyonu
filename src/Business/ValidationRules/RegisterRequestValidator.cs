using Business.Constants;
using Business.DTOs.Auth;
using FluentValidation;

namespace Business.ValidationRules;

public sealed class RegisterRequestValidator : AbstractValidator<RegisterRequestDto>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).Must(PasswordRules.IsStrongEnough).WithMessage(Messages.WeakPassword);
        RuleFor(x => x.StudentNumber).NotEmpty().MaximumLength(50);
        RuleFor(x => x.DepartmentId).GreaterThan(0);
        RuleFor(x => x.EnrollmentYear).InclusiveBetween(2000, DateTime.UtcNow.Year + 1);
    }
}
