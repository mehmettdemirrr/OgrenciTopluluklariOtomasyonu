using Business.DTOs.Reference;
using FluentValidation;

namespace Business.ValidationRules;

public sealed class CreateDepartmentRequestValidator : AbstractValidator<CreateDepartmentRequestDto>
{
    public CreateDepartmentRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    }
}
