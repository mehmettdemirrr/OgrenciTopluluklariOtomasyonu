using AutoMapper;
using Business.DTOs.Clubs;
using Entities;

namespace Business.Mappings;

/// <summary>docs/MIMARI.md · Y-32: yalnızca alan taşır — koşullu mantık/DB çağrısı yok, Entity→Entity yok.</summary>
public sealed class ClubMappingProfile : Profile
{
    public ClubMappingProfile()
    {
        // Y-32: ClubCategoryName'in Club üzerinde kaynağı yok (join gerekir, AutoMapper'da DB
        // çağrısı yasak). Açıkça Ignore ediliyor; adı ClubManager dolduruyor.
        CreateMap<Club, ClubListItemDto>()
            .ForMember(d => d.ClubCategoryName, o => o.Ignore());

        // K-44: SocialLinks ayrı bir tablodan gelir (ClubSocialLink); ClubManager dolduruyor.
        CreateMap<Club, ClubDetailDto>()
            .ForMember(d => d.ClubCategoryName, o => o.Ignore())
            .ForMember(d => d.SocialLinks, o => o.Ignore());
    }
}
