using Business.DTOs.Announcements;
using FluentValidation;

namespace Business.ValidationRules;

public sealed class UpdateAnnouncementRequestValidator : AbstractValidator<UpdateAnnouncementRequestDto>
{
    public UpdateAnnouncementRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Content).NotEmpty();
        RuleFor(x => x.Visibility).IsInEnum();
    }
}
