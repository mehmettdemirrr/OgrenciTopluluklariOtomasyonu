using Business.Constants;
using Business.DTOs.Admin;
using FluentValidation;

namespace Business.ValidationRules;

public sealed class CreateUserRequestValidator : AbstractValidator<CreateUserRequestDto>
{
    public CreateUserRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).Must(PasswordRules.IsStrongEnough).WithMessage(Messages.WeakPassword);
    }
}
