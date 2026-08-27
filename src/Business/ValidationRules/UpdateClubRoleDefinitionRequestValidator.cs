using Business.DTOs.Clubs;
using FluentValidation;

namespace Business.ValidationRules;

public sealed class UpdateClubRoleDefinitionRequestValidator : AbstractValidator<UpdateClubRoleDefinitionRequestDto>
{
    public UpdateClubRoleDefinitionRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.ClubRole).IsInEnum();
        RuleFor(x => x.DisplayOrder).GreaterThanOrEqualTo(0);
    }
}
