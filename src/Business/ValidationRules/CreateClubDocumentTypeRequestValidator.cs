using Business.DTOs.Reference;
using FluentValidation;

namespace Business.ValidationRules;

/// <summary>
/// docs/MIMARI.md · Y-71: burada YALNIZCA biçim var. "Bu kod alınmış mı" ve "zorunlu evraklar tam mı"
/// soruları veritabanına bakmayı gerektirir; onlar Business'ta karara bağlanır.
/// </summary>
public sealed class CreateClubDocumentTypeRequestValidator : AbstractValidator<CreateClubDocumentTypeRequestDto>
{
    public CreateClubDocumentTypeRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(300);
        RuleFor(x => x.DisplayOrder).GreaterThanOrEqualTo(0);
    }
}
