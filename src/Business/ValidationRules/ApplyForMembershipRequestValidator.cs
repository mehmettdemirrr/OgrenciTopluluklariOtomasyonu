using Business.DTOs.Memberships;
using FluentValidation;

namespace Business.ValidationRules;

/// <summary>docs/MIMARI.md · Aspect kataloğu: yalnızca biçimsel doğrulama — "bu kulüp var mı" iş kuralıdır.</summary>
public sealed class ApplyForMembershipRequestValidator : AbstractValidator<ApplyForMembershipRequestDto>
{
    public ApplyForMembershipRequestValidator()
    {
        RuleFor(x => x.ClubId).GreaterThan(0);
    }
}
