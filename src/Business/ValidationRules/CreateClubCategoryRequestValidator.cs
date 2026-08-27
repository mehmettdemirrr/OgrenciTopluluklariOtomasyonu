using Business.DTOs.Reference;
using FluentValidation;

namespace Business.ValidationRules;

public sealed class CreateClubCategoryRequestValidator : AbstractValidator<CreateClubCategoryRequestDto>
{
    public CreateClubCategoryRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    }
}
