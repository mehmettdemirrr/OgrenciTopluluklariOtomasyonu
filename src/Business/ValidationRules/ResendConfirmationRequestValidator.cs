using Business.DTOs.Auth;
using FluentValidation;

namespace Business.ValidationRules;

public sealed class ResendConfirmationRequestValidator : AbstractValidator<ResendConfirmationRequestDto>
{
    public ResendConfirmationRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
    }
}
