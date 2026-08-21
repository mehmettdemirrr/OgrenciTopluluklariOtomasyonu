using Business.Constants;
using Business.DTOs.Admin;
using FluentValidation;

namespace Business.ValidationRules;

/// <summary>
/// Yalnızca biçim: harf/rakam/boşluk/._- dışı karakterler (regex metakarakterleri dahil)
/// baştan elenir — bilinen izin kodu/ad çakışması kontrolü Manager'da (Y-03: iş kuralı).
/// </summary>
public sealed class CreateRoleRequestValidator : AbstractValidator<CreateRoleRequestDto>
{
    public CreateRoleRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(256)
            .Matches(@"^[\p{L}\p{Nd} ._-]+$")
            .WithMessage(Messages.InvalidRoleName);
    }
}
