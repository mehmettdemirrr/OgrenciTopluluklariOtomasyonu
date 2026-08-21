using Business.DTOs.Clubs;
using FluentValidation;

namespace Business.ValidationRules;

public sealed class UpdateClubRequestValidator : AbstractValidator<UpdateClubRequestDto>
{
    public UpdateClubRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(2000);
    }
}
