using Business.Constants;
using Business.DTOs.Reference;
using FluentValidation;

namespace Business.ValidationRules;

public sealed class CreateAcademicTermRequestValidator : AbstractValidator<CreateAcademicTermRequestDto>
{
    public CreateAcademicTermRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);

        RuleFor(x => x.EndDateUtc)
            .GreaterThan(x => x.StartDateUtc)
            .WithMessage(Messages.InvalidAcademicTermDateRange);
    }
}
