using Business.DTOs.Auth;
using FluentValidation;

namespace Business.ValidationRules;

/// <summary>docs/MIMARI.md · Y-35: yalnızca biçim — bölümün var olup olmadığı Business'ın kararı.</summary>
public sealed class UpdateMeRequestValidator : AbstractValidator<UpdateMeRequestDto>
{
    public UpdateMeRequestValidator()
    {
        RuleFor(x => x.DepartmentId).GreaterThan(0);
        RuleFor(x => x.EnrollmentYear).InclusiveBetween(2000, DateTime.UtcNow.Year + 1);
    }
}
