using Business.DTOs.Announcements;
using FluentValidation;

namespace Business.ValidationRules;

public sealed class UpdateAnnouncementRequestValidator : AbstractValidator<UpdateAnnouncementRequestDto>
{
    public UpdateAnnouncementRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(300);
        // K-42: arayüz zengin editörle yalnızca ContentJson gönderebilir — Content'i AYRICA
        // zorunlu tutmak, RichTextEditor'ün ürettiği isteği bu kural burada reddederdi.
        RuleFor(x => x)
            .Must(x => !string.IsNullOrWhiteSpace(x.Content) || !string.IsNullOrWhiteSpace(x.ContentJson))
            .WithMessage("İçerik gerekli.")
            .WithName(nameof(UpdateAnnouncementRequestDto.Content));
        RuleFor(x => x.Visibility).IsInEnum();
    }
}
