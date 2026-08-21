using Business.Constants;
using Business.DTOs.Events;
using FluentValidation;

namespace Business.ValidationRules;

public sealed class CreateEventRequestValidator : AbstractValidator<CreateEventRequestDto>
{
    public CreateEventRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(300);

        RuleFor(x => x.EndDateUtc)
            .GreaterThan(x => x.StartDateUtc)
            .WithMessage(Messages.InvalidAcademicTermDateRange);

        RuleFor(x => x.Capacity)
            .GreaterThan(0)
            .When(x => x.Capacity is not null);
    }
}
