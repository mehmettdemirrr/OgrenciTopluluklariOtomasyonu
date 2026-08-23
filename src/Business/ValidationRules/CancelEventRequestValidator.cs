using Business.DTOs.Events;
using FluentValidation;

namespace Business.ValidationRules;

/// <summary>docs/MIMARI.md · Y-35: yalnızca biçim — etkinliğin iptal edilebilir durumda olması Business'ın kararı.</summary>
public sealed class CancelEventRequestValidator : AbstractValidator<CancelEventRequestDto>
{
    public CancelEventRequestValidator()
    {
        RuleFor(x => x.CancellationReason).NotEmpty().MaximumLength(500);
    }
}
