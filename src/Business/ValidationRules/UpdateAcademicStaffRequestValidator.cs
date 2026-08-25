using Business.DTOs.Reference;
using FluentValidation;

namespace Business.ValidationRules;

public sealed class UpdateAcademicStaffRequestValidator : AbstractValidator<UpdateAcademicStaffRequestDto>
{
    public UpdateAcademicStaffRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(100);
        RuleFor(x => x.DepartmentId).GreaterThan(0);
    }
}
