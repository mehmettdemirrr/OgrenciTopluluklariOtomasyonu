using Business.Constants;
using Business.DTOs.ClubApplications;
using Entities.Enums;
using FluentValidation;

namespace Business.ValidationRules;

/// <summary>docs/MIMARI.md · yalnızca biçimsel doğrulama: karar Approved/Rejected olmalı, Pending olamaz.</summary>
public sealed class DecideClubApplicationRequestValidator : AbstractValidator<DecideClubApplicationRequestDto>
{
    public DecideClubApplicationRequestValidator()
    {
        RuleFor(x => x.Status)
            .Must(status => status is ApplicationStatus.Approved or ApplicationStatus.Rejected)
            .WithMessage(Messages.InvalidReviewDecision);
    }
}
