using Business.DTOs.Clubs;
using FluentValidation;

namespace Business.ValidationRules;

public sealed class CreateClubRequestValidator : AbstractValidator<CreateClubRequestDto>
{
    public CreateClubRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.AdvisorId).GreaterThan(0);
    }
}
