using Business.DTOs.Reference;
using FluentValidation;

namespace Business.ValidationRules;

public sealed class CreateAcademicStaffRequestValidator : AbstractValidator<CreateAcademicStaffRequestDto>
{
    public CreateAcademicStaffRequestValidator()
    {
        RuleFor(x => x.ApplicationUserId).GreaterThan(0);
        RuleFor(x => x.Title).NotEmpty().MaximumLength(100);
        RuleFor(x => x.DepartmentId).GreaterThan(0);
    }
}
