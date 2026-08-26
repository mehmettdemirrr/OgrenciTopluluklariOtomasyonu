using Business.Constants;
using Business.DTOs.Reference;
using FluentValidation;

namespace Business.ValidationRules;

/// <summary>
/// docs/MIMARI.md · Y-35: yalnızca biçim doğrulanır — "pencere şu anda açık mı" kararı
/// ClubApplicationManager.EvaluateWindow'da kalır (Y-73).
/// </summary>
public sealed class SetClubApplicationWindowRequestValidator : AbstractValidator<SetClubApplicationWindowRequestDto>
{
    public SetClubApplicationWindowRequestValidator()
    {
        RuleFor(x => x.Override).IsInEnum();

        // A-66: ikisi de null olabilir (takvimi temizlemek meşru). İkisi de doluysa sıra doğru olmalı.
        RuleFor(x => x.EndUtc)
            .GreaterThan(x => x.StartUtc)
            .When(x => x.StartUtc is not null && x.EndUtc is not null)
            .WithMessage(Messages.InvalidAcademicTermDateRange);
    }
}
