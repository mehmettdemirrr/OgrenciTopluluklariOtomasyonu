using Business.DTOs.Reference;
using FluentValidation;

namespace Business.ValidationRules;

public sealed class UpdateFacultyRequestValidator : AbstractValidator<UpdateFacultyRequestDto>
{
    public UpdateFacultyRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    }
}
