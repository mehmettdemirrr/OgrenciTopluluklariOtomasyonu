using Business.DTOs.Clubs;
using Business.ValidationRules;
using Entities.Enums;
using Xunit;

namespace Business.Tests;

/// <summary>docs/MIMARI.md · Y-80: yalnızca https ve platformun bilinen alan adı kabul edilir.</summary>
public class ClubSocialLinkValidationTests
{
    [Theory]
    [InlineData(SocialPlatform.Instagram, "https://www.instagram.com/topluluk", true)]
    [InlineData(SocialPlatform.Instagram, "http://www.instagram.com/topluluk", false)]
    [InlineData(SocialPlatform.Instagram, "https://instagram.evil.com/topluluk", false)]
    [InlineData(SocialPlatform.Instagram, "javascript:alert(1)", false)]
    [InlineData(SocialPlatform.X, "https://x.com/topluluk", true)]
    [InlineData(SocialPlatform.X, "https://twitter.com/topluluk", true)]
    [InlineData(SocialPlatform.Website, "https://topluluk.ozal.edu.tr", true)]
    [InlineData(SocialPlatform.Website, "http://topluluk.ozal.edu.tr", false)]
    public void Validator_AcceptsOnlyHttpsKnownHosts(SocialPlatform platform, string url, bool expected)
    {
        var request = new SetClubSocialLinksRequestDto
        {
            Links = [new ClubSocialLinkDto { Platform = platform, Url = url, DisplayOrder = 0 }],
        };

        var result = new SetClubSocialLinksRequestValidator().Validate(request);

        Assert.Equal(expected, result.IsValid);
    }

    [Fact]
    public void Validator_RejectsMoreThanTenLinks()
    {
        var links = Enumerable.Range(0, 11)
            .Select(i => new ClubSocialLinkDto { Platform = SocialPlatform.Website, Url = "https://a.com", DisplayOrder = i })
            .ToList();

        var result = new SetClubSocialLinksRequestValidator().Validate(new SetClubSocialLinksRequestDto { Links = links });

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validator_RejectsInvalidContactEmail()
    {
        var result = new SetClubSocialLinksRequestValidator().Validate(new SetClubSocialLinksRequestDto { ContactEmail = "not-an-email" });

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validator_AcceptsEmptyRequest()
    {
        var result = new SetClubSocialLinksRequestValidator().Validate(new SetClubSocialLinksRequestDto());

        Assert.True(result.IsValid);
    }
}
