using Business.DTOs.Clubs;
using Entities.Enums;
using FluentValidation;

namespace Business.ValidationRules;

/// <summary>docs/MIMARI.md · Y-80: yalnızca https ve platformun bilinen alan adı.</summary>
public sealed class SetClubSocialLinksRequestValidator : AbstractValidator<SetClubSocialLinksRequestDto>
{
    private static readonly IReadOnlyDictionary<SocialPlatform, string[]> KnownHosts =
        new Dictionary<SocialPlatform, string[]>
        {
            [SocialPlatform.Instagram] = ["instagram.com", "www.instagram.com"],
            [SocialPlatform.X] = ["x.com", "www.x.com", "twitter.com", "www.twitter.com"],
            [SocialPlatform.LinkedIn] = ["linkedin.com", "www.linkedin.com"],
            [SocialPlatform.YouTube] = ["youtube.com", "www.youtube.com", "youtu.be"],
            [SocialPlatform.Facebook] = ["facebook.com", "www.facebook.com", "m.facebook.com"],
        };

    public SetClubSocialLinksRequestValidator()
    {
        RuleFor(x => x.ContactEmail).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.ContactEmail));
        RuleFor(x => x.ContactPhone).MaximumLength(32);
        RuleFor(x => x.Links).Must(links => links.Count <= 10).WithMessage("En fazla 10 bağlantı eklenebilir.");
        RuleForEach(x => x.Links).ChildRules(link =>
        {
            link.RuleFor(l => l.Url).Must(BeAllowedUrl).WithMessage("Bağlantı https olmalı ve platformun adresine gitmelidir.");
            link.RuleFor(l => l.Platform).IsInEnum();
        });
    }

    // Website platformu serbest alan adıdır (kulübün kendi sitesi); şema kısıtı yeterlidir.
    private static bool BeAllowedUrl(ClubSocialLinkDto link, string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
        {
            return false;
        }

        return !KnownHosts.TryGetValue(link.Platform, out var hosts)
            || hosts.Contains(uri.Host, StringComparer.OrdinalIgnoreCase);
    }
}
