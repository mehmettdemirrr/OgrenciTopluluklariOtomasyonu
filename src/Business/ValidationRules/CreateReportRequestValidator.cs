using Business.Constants;
using Business.DTOs.Reports;
using FluentValidation;

namespace Business.ValidationRules;

/// <summary>docs/MIMARI.md · yalnızca biçimsel doğrulama: tür geçerli, ilgili id pozitif olmalı.</summary>
public sealed class CreateReportRequestValidator : AbstractValidator<CreateReportRequestDto>
{
    public CreateReportRequestValidator()
    {
        RuleFor(x => x.ReportType).IsInEnum();

        // FluentValidation'da GreaterThan tek başına null'ı sessizce geçerli sayar (karşılaştıracak
        // değer yok) — NotNull() burada kasıtlı, aksi hâlde eksik ClubId/EventId biçim hatası olarak yakalanmaz.
        RuleFor(x => x.ClubId)
            .NotNull()
            .GreaterThan(0)
            .When(x => x.ReportType == ReportType.ClubMembers)
            .WithMessage(Messages.InvalidReportParameters);

        RuleFor(x => x.EventId)
            .NotNull()
            .GreaterThan(0)
            .When(x => x.ReportType == ReportType.EventParticipants)
            .WithMessage(Messages.InvalidReportParameters);
    }
}
