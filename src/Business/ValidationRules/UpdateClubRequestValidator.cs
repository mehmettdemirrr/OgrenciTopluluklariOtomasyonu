using Business.DTOs.Clubs;
using FluentValidation;

namespace Business.ValidationRules;

public sealed class UpdateClubRequestValidator : AbstractValidator<UpdateClubRequestDto>
{
    public UpdateClubRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(2000);

        // K-49: kart tasarımı üç rozetten fazlasını taşımaz; kapı burada (Y-35).
        RuleFor(x => x.ClubCategoryIds)
            .NotNull()
            .Must(ids => ids.Count <= 3).WithMessage("En fazla 3 kategori seçilebilir.")
            .Must(ids => ids.Distinct().Count() == ids.Count).WithMessage("Aynı kategori birden fazla kez seçilemez.");
    }
}
