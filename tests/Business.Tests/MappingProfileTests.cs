using AutoMapper;
using Business.DTOs.Clubs;
using Business.Mappings;
using Entities;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Business.Tests;

/// <summary>docs/MIMARI.md · Y-32: AutoMapper profilleri için zorunlu doğrulama testi.</summary>
public class MappingProfileTests
{
    [Fact(DisplayName = "ClubMappingProfile geçerlidir")]
    public void ClubMappingProfile_IsValid()
    {
        var configuration = new MapperConfiguration(cfg => cfg.AddProfile<ClubMappingProfile>(), NullLoggerFactory.Instance);

        configuration.AssertConfigurationIsValid();
    }

    [Fact(DisplayName = "Y-32/A-80: ClubCategoryNames profilde açıkça yok sayılır (kaynağı Club üzerinde yok)")]
    public void ClubMappingProfile_IgnoresClubCategoryNames()
    {
        var configuration = new MapperConfiguration(cfg => cfg.AddProfile<ClubMappingProfile>(), NullLoggerFactory.Instance);
        var mapper = configuration.CreateMapper();

        var club = new Club
        {
            Id = 1, Name = "Test", AdvisorId = 1, IsActive = true,
            CreatedAtUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        };

        var dto = mapper.Map<ClubListItemDto>(club);

        // Kategori bağ tablosunda tutulur (A-80); adlar bir join gerektirdiği için AutoMapper'da
        // DB çağrısı yasak (Y-32) — ClubManager tek toplu sorgudan doldurur.
        Assert.Empty(dto.ClubCategoryNames);
    }
}
