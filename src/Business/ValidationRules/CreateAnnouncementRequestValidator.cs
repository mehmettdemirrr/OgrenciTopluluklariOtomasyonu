using Business.DTOs.Announcements;
using FluentValidation;

namespace Business.ValidationRules;

public sealed class CreateAnnouncementRequestValidator : AbstractValidator<CreateAnnouncementRequestDto>
{
    public CreateAnnouncementRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Content).NotEmpty();
        RuleFor(x => x.Visibility).IsInEnum();
    }
}
