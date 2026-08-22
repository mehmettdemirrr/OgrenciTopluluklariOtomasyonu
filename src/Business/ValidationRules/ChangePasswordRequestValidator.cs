using Business.Constants;
using Business.DTOs.Auth;
using FluentValidation;

namespace Business.ValidationRules;

public sealed class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequestDto>
{
    public ChangePasswordRequestValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty();
        RuleFor(x => x.NewPassword).Must(PasswordRules.IsStrongEnough).WithMessage(Messages.WeakPassword);
    }
}
