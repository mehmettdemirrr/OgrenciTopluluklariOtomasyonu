using Business.Constants;
using Business.DTOs.Auth;
using FluentValidation;

namespace Business.ValidationRules;

public sealed class ResetPasswordRequestValidator : AbstractValidator<ResetPasswordRequestDto>
{
    public ResetPasswordRequestValidator()
    {
        RuleFor(x => x.UserId).GreaterThan(0);
        RuleFor(x => x.Token).NotEmpty();
        RuleFor(x => x.NewPassword).Must(PasswordRules.IsStrongEnough).WithMessage(Messages.WeakPassword);
    }
}
