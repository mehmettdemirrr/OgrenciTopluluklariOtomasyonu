using Business.DTOs.Clubs;
using Entities.Enums;
using FluentValidation;

namespace Business.ValidationRules;

public sealed class UpdateClubRoleDefinitionRequestValidator : AbstractValidator<UpdateClubRoleDefinitionRequestDto>
{
    /// <summary>docs/MIMARI.md · A-68/Y-69: kapalı kümenin tamamı — bunun dışı kabul edilmez.</summary>
    private const ClubCapability AllClubCapabilities =
        ClubCapability.MembersView
        | ClubCapability.MembersManage
        | ClubCapability.EventsManage
        | ClubCapability.EventParticipantsView
        | ClubCapability.AnnouncementsManage
        | ClubCapability.ReportsView;

    public UpdateClubRoleDefinitionRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.ClubRole).IsInEnum();
        RuleFor(x => x.DisplayOrder).GreaterThanOrEqualTo(0);

        // Y-69: yalnızca tanımlı bayrakların birleşimi kabul edilir — istemci uydurma bir sayı
        // gönderip kapalı kümenin dışına çıkamaz. IsInEnum() [Flags] için YETMEZ: birleşik
        // değerler (örn. 5 = MembersView|EventsManage) enum'da tek tek tanımlı değildir.
        RuleFor(x => x.Capabilities)
            .Must(value => (value & ~AllClubCapabilities) == ClubCapability.None)
            .WithMessage("Tanımsız yetki değeri gönderildi.");
    }
}
