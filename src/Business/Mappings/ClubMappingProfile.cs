using AutoMapper;
using Business.DTOs.Clubs;
using Entities;

namespace Business.Mappings;

/// <summary>docs/MIMARI.md · Y-32: yalnızca alan taşır — koşullu mantık/DB çağrısı yok, Entity→Entity yok.</summary>
public sealed class ClubMappingProfile : Profile
{
    public ClubMappingProfile()
    {
        // Y-32/A-80: ClubCategoryNames'in Club üzerinde kaynağı yok (bağ tablosu join'i gerekir,
        // AutoMapper'da DB çağrısı yasak). Açıkça Ignore ediliyor; adları ClubManager dolduruyor.
        CreateMap<Club, ClubListItemDto>()
            .ForMember(d => d.ClubCategoryNames, o => o.Ignore());

        // K-44: SocialLinks ayrı bir tablodan gelir (ClubSocialLink); ClubManager dolduruyor.
        // A-75: MyRelationship/MyCapabilities çağırana özgüdür, Club üzerinde kaynağı yok —
        // ClubManager.ResolveViewerAsync dolduruyor.
        CreateMap<Club, ClubDetailDto>()
            .ForMember(d => d.ClubCategoryNames, o => o.Ignore())
            .ForMember(d => d.ClubCategoryIds, o => o.Ignore())
            .ForMember(d => d.SocialLinks, o => o.Ignore())
            .ForMember(d => d.MyRelationship, o => o.Ignore())
            .ForMember(d => d.MyCapabilities, o => o.Ignore());
    }
}
