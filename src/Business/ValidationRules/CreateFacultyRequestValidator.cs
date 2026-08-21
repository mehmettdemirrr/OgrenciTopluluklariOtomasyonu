using Business.DTOs.Reference;
using FluentValidation;

namespace Business.ValidationRules;

public sealed class CreateFacultyRequestValidator : AbstractValidator<CreateFacultyRequestDto>
{
    public CreateFacultyRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    }
}
