using Business.DTOs.Reference;
using FluentValidation;

namespace Business.ValidationRules;

public sealed class UpdateDepartmentRequestValidator : AbstractValidator<UpdateDepartmentRequestDto>
{
    public UpdateDepartmentRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    }
}
