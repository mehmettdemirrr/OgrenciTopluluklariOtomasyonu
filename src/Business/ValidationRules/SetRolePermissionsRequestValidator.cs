using Business.Constants;
using Business.DTOs.Admin;
using FluentValidation;

namespace Business.ValidationRules;

/// <summary>Yalnızca biçim — bilinen izin kodu kontrolü Manager'da (Y-03: iş kuralı).</summary>
public sealed class SetRolePermissionsRequestValidator : AbstractValidator<SetRolePermissionsRequestDto>
{
    public SetRolePermissionsRequestValidator()
    {
        RuleFor(x => x.Permissions)
            .NotNull()
            .WithMessage(Messages.UnknownPermissionCode);

        RuleForEach(x => x.Permissions)
            .NotEmpty()
            .WithMessage(Messages.UnknownPermissionCode);
    }
}
