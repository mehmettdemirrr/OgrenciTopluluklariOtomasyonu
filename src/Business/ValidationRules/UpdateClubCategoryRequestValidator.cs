using Business.DTOs.Reference;
using FluentValidation;

namespace Business.ValidationRules;

public sealed class UpdateClubCategoryRequestValidator : AbstractValidator<UpdateClubCategoryRequestDto>
{
    public UpdateClubCategoryRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    }
}
