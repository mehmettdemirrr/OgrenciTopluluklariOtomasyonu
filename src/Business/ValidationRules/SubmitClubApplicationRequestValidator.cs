using Business.DTOs.ClubApplications;
using FluentValidation;

namespace Business.ValidationRules;

/// <summary>docs/MIMARI.md · Y-35: yalnızca biçimsel doğrulama — ad çakışması/danışman geçerliliği Business'ta.</summary>
public sealed class SubmitClubApplicationRequestValidator : AbstractValidator<SubmitClubApplicationRequestDto>
{
    public SubmitClubApplicationRequestValidator()
    {
        RuleFor(x => x.ProposedName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Justification).NotEmpty();
        RuleFor(x => x.ProposedAdvisorId).GreaterThan(0);
    }
}
