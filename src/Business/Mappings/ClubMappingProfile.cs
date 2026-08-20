using AutoMapper;
using Business.DTOs.Clubs;
using Entities;

namespace Business.Mappings;

/// <summary>docs/MIMARI.md · Y-32: yalnızca alan taşır — koşullu mantık/DB çağrısı yok, Entity→Entity yok.</summary>
public sealed class ClubMappingProfile : Profile
{
    public ClubMappingProfile()
    {
        CreateMap<Club, ClubListItemDto>();
        CreateMap<Club, ClubDetailDto>();
    }
}
