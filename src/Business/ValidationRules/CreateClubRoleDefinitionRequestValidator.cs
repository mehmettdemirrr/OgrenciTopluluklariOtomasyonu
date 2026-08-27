using Business.DTOs.Clubs;
using FluentValidation;

namespace Business.ValidationRules;

/// <summary>
/// Y-35: yalnızca biçim. "Bu unvan alınmış mı" ve "ikinci başkan üretir mi" kararları
/// veritabanına bakmayı gerektirir; onlar Business'ta verilir.
/// </summary>
public sealed class CreateClubRoleDefinitionRequestValidator : AbstractValidator<CreateClubRoleDefinitionRequestDto>
{
    public CreateClubRoleDefinitionRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.ClubRole).IsInEnum();
        RuleFor(x => x.DisplayOrder).GreaterThanOrEqualTo(0);
    }
}
