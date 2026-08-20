using Business.Constants;
using Business.DTOs.Memberships;
using Entities.Enums;
using FluentValidation;

namespace Business.ValidationRules;

/// <summary>docs/MIMARI.md · yalnızca biçimsel doğrulama: karar Approved/Rejected olmalı, Pending olamaz.</summary>
public sealed class ReviewMembershipApplicationRequestValidator : AbstractValidator<ReviewMembershipApplicationRequestDto>
{
    public ReviewMembershipApplicationRequestValidator()
    {
        RuleFor(x => x.Status)
            .Must(status => status is ApplicationStatus.Approved or ApplicationStatus.Rejected)
            .WithMessage(Messages.InvalidReviewDecision);
    }
}
