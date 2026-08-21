using Business.Constants;
using Business.DTOs.Events;
using Entities.Enums;
using FluentValidation;

namespace Business.ValidationRules;

public sealed class DecideEventRequestValidator : AbstractValidator<DecideEventRequestDto>
{
    public DecideEventRequestValidator()
    {
        RuleFor(x => x.Status)
            .Must(status => status is EventStatus.Published or EventStatus.Rejected)
            .WithMessage(Messages.InvalidEventDecision);
    }
}
