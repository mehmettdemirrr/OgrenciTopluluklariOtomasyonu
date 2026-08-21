using Business.Constants;
using Business.DTOs.Admin;
using FluentValidation;

namespace Business.ValidationRules;

/// <summary>Yalnızca biçim — var olan rol adı kontrolü Manager'da (Y-03: iş kuralı).</summary>
public sealed class SetUserRolesRequestValidator : AbstractValidator<SetUserRolesRequestDto>
{
    public SetUserRolesRequestValidator()
    {
        RuleFor(x => x.RoleNames)
            .NotNull()
            .WithMessage(Messages.RoleNotFound);

        RuleForEach(x => x.RoleNames)
            .NotEmpty()
            .WithMessage(Messages.RoleNotFound);
    }
}
