using Business.DTOs.Announcements;
using Business.ValidationRules;
using Entities.Enums;
using Xunit;

namespace Business.Tests;

/// <summary>
/// docs/MIMARI.md · K-42: RichTextEditor yalnızca ContentJson gönderir — Content'i AYRICA
/// zorunlu tutan eski kural bu isteği reddederdi. Content VEYA ContentJson'dan biri yeterlidir.
/// </summary>
public class AnnouncementRequestValidatorTests
{
    [Fact(DisplayName = "Create: yalnızca ContentJson dolu olan istek geçerlidir")]
    public void CreateValidator_AcceptsContentJsonOnly()
    {
        var result = new CreateAnnouncementRequestValidator().Validate(new CreateAnnouncementRequestDto
        {
            Title = "Duyuru", ContentJson = """{"type":"doc"}""", Visibility = AnnouncementVisibility.Public,
        });

        Assert.True(result.IsValid);
    }

    [Fact(DisplayName = "Create: ne Content ne ContentJson doluysa geçersizdir")]
    public void CreateValidator_RejectsWhenBothEmpty()
    {
        var result = new CreateAnnouncementRequestValidator().Validate(new CreateAnnouncementRequestDto
        {
            Title = "Duyuru", Visibility = AnnouncementVisibility.Public,
        });

        Assert.False(result.IsValid);
    }

    [Fact(DisplayName = "Update: yalnızca ContentJson dolu olan istek geçerlidir")]
    public void UpdateValidator_AcceptsContentJsonOnly()
    {
        var result = new UpdateAnnouncementRequestValidator().Validate(new UpdateAnnouncementRequestDto
        {
            Title = "Duyuru", ContentJson = """{"type":"doc"}""", Visibility = AnnouncementVisibility.Public,
        });

        Assert.True(result.IsValid);
    }
}
